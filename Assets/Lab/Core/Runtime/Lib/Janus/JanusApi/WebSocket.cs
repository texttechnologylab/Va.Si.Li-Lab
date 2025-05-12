using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Meta.Net.NativeWebSocket;
using Org.BouncyCastle.Asn1.Esf;
using UnityEngine;

namespace VaSiLi.WebSocket
{
    public class WebSocket : IWebSocket
    {
        private readonly object IncomingMessageLock = new object();

        private readonly object OutgoingMessageLock = new object();

        private readonly Dictionary<string, string> headers;

        private bool isSending;

        private CancellationToken m_CancellationToken;

        private readonly List<byte[]> m_MessageList = new List<byte[]>();

        private ClientWebSocket m_Socket = new ClientWebSocket();

        private CancellationTokenSource m_TokenSource;

        private readonly List<ArraySegment<byte>> sendBytesQueue = new List<ArraySegment<byte>>();

        private readonly List<ArraySegment<byte>> sendTextQueue = new List<ArraySegment<byte>>();

        private readonly List<string> subprotocols;

        private readonly Uri uri;

        public Meta.Net.NativeWebSocket.WebSocketState State
        {
            get
            {
                switch (m_Socket.State)
                {
                    case System.Net.WebSockets.WebSocketState.Connecting:
                        return Meta.Net.NativeWebSocket.WebSocketState.Connecting;
                    case System.Net.WebSockets.WebSocketState.Open:
                        return Meta.Net.NativeWebSocket.WebSocketState.Open;
                    case System.Net.WebSockets.WebSocketState.CloseSent:
                    case System.Net.WebSockets.WebSocketState.CloseReceived:
                        return Meta.Net.NativeWebSocket.WebSocketState.Closing;
                    case System.Net.WebSockets.WebSocketState.Closed:
                        return Meta.Net.NativeWebSocket.WebSocketState.Closed;
                    default:
                        return Meta.Net.NativeWebSocket.WebSocketState.Closed;
                }
            }
        }

        public event WebSocketOpenEventHandler OnOpen;

        public event WebSocketMessageEventHandler OnMessage;

        public event WebSocketErrorEventHandler OnError;

        public event WebSocketCloseEventHandler OnClose;

        public WebSocket(string url, Dictionary<string, string> headers = null)
        {
            uri = new Uri(url);
            if (headers == null)
            {
                this.headers = new Dictionary<string, string>();
            }
            else
            {
                this.headers = headers;
            }

            subprotocols = new List<string>();
            string scheme = uri.Scheme;
            if (!scheme.Equals("ws") && !scheme.Equals("wss"))
            {
                throw new ArgumentException("Unsupported protocol: " + scheme);
            }
        }

        public WebSocket(string url, string subprotocol, Dictionary<string, string> headers = null)
        {
            uri = new Uri(url);
            if (headers == null)
            {
                this.headers = new Dictionary<string, string>();
            }
            else
            {
                this.headers = headers;
            }

            subprotocols = new List<string> { subprotocol };
            string scheme = uri.Scheme;
            if (!scheme.Equals("ws") && !scheme.Equals("wss"))
            {
                throw new ArgumentException("Unsupported protocol: " + scheme);
            }
        }

        public WebSocket(string url, List<string> subprotocols, Dictionary<string, string> headers = null)
        {
            uri = new Uri(url);
            if (headers == null)
            {
                this.headers = new Dictionary<string, string>();
            }
            else
            {
                this.headers = headers;
            }

            this.subprotocols = subprotocols;
            string scheme = uri.Scheme;
            if (!scheme.Equals("ws") && !scheme.Equals("wss"))
            {
                throw new ArgumentException("Unsupported protocol: " + scheme);
            }
        }

        public void CancelConnection()
        {
            m_TokenSource?.Cancel();
        }

        public async Task Connect()
        {
            try
            {
                m_TokenSource = new CancellationTokenSource();
                m_CancellationToken = m_TokenSource.Token;
                m_Socket = new ClientWebSocket();
                foreach (KeyValuePair<string, string> header in headers)
                {
                    m_Socket.Options.SetRequestHeader(header.Key, header.Value);
                }

                foreach (string subprotocol in subprotocols)
                {
                    m_Socket.Options.AddSubProtocol(subprotocol);
                }
                m_Socket.Options.SetBuffer(8192 * 2, 8192 * 2);
                await m_Socket.ConnectAsync(uri, m_CancellationToken);
                this.OnOpen?.Invoke();
                await Receive();
            }
            catch (Exception ex2)
            {
                Exception ex = ex2;
                this.OnError?.Invoke(ex.Message);
                this.OnClose?.Invoke(WebSocketCloseCode.Abnormal);
            }
            finally
            {
                if (m_Socket != null)
                {
                    m_TokenSource.Cancel();
                    m_Socket.Dispose();
                }
            }
        }

        public Task Send(byte[] bytes)
        {
            return SendMessage(sendBytesQueue, WebSocketMessageType.Binary, new ArraySegment<byte>(bytes));
        }

        public Task SendText(string message)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            return SendMessage(sendTextQueue, WebSocketMessageType.Text, new ArraySegment<byte>(bytes, 0, bytes.Length));
        }

        private async Task SendMessage(List<ArraySegment<byte>> queue, WebSocketMessageType messageType, ArraySegment<byte> buffer)
        {
            if (buffer.Count == 0)
            {
                return;
            }

            bool sending;
            lock (OutgoingMessageLock)
            {
                sending = isSending;
                if (!isSending)
                {
                    isSending = true;
                }
            }

            if (!sending)
            {
                if (!Monitor.TryEnter(m_Socket, 1000))
                {
                    await m_Socket.CloseAsync(WebSocketCloseStatus.InternalServerError, string.Empty, m_CancellationToken);
                    return;
                }

                try
                {
                    Task t = m_Socket.SendAsync(buffer, messageType, endOfMessage: true, m_CancellationToken);
                    t.Wait(m_CancellationToken);
                }
                finally
                {
                    Monitor.Exit(m_Socket);
                }

                lock (OutgoingMessageLock)
                {
                    isSending = false;
                }

                await HandleQueue(queue, messageType);
            }
            else
            {
                lock (OutgoingMessageLock)
                {
                    queue.Add(buffer);
                }
            }
        }

        private async Task HandleQueue(List<ArraySegment<byte>> queue, WebSocketMessageType messageType)
        {
            ArraySegment<byte> buffer = default(ArraySegment<byte>);
            lock (OutgoingMessageLock)
            {
                if (queue.Count > 0)
                {
                    buffer = queue[0];
                    queue.RemoveAt(0);
                }
            }

            if (buffer.Count > 0)
            {
                await SendMessage(queue, messageType, buffer);
            }
        }

        public async Task Receive()
        {
            WebSocketCloseCode closeCode = WebSocketCloseCode.Abnormal;
            await new WaitForBackgroundThread();
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[8192]);
            try
            {
                while (m_Socket.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    WebSocketReceiveResult result;
                    buffer = new ArraySegment<byte>(new byte[8192]);
                    int writePosition = 0;
                    do
                    {
                        ArraySegment<byte> partBuffer = new ArraySegment<byte>(new byte[2048]);
                        result = await m_Socket.ReceiveAsync(partBuffer, m_CancellationToken);

                        if (result.Count > 0)
                        {
                            int bytesToCopy = Math.Min(result.Count, buffer.Count - writePosition);
                            partBuffer.CopyTo(buffer.Array, writePosition);
                            writePosition += bytesToCopy;
                        }
                    }
                    while (!result.EndOfMessage);
                    this.OnMessage?.Invoke(buffer.Array, buffer.Offset, writePosition + result.Count);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await Close();
                        closeCode = WebSocketHelpers.ParseCloseCodeEnum((int)result.CloseStatus.Value);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(ex.Message);
                UnityEngine.Debug.LogError(ex.StackTrace);
                m_TokenSource.Cancel();
            }
            finally
            {
                await new WaitForUpdate();
                this.OnClose?.Invoke(closeCode);
            }
        }

        public async Task Close()
        {
            if (State == Meta.Net.NativeWebSocket.WebSocketState.Open)
            {
                await m_Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, m_CancellationToken);
            }
        }
    }
}