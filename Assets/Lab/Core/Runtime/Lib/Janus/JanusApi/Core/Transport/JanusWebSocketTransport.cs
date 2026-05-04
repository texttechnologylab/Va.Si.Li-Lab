using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VaSiLi.Janus
{
    public class JanusWebSocketTransport : IJanusTransport
    {
        readonly Uri _uri;
        readonly List<string> _subprotocols;
        readonly Dictionary<string, string> _headers;

        VaSiLi.WebSocket.WebSocket _socket;
        TaskCompletionSource<bool> _openTcs;
        readonly object _stateLock = new object();
        JanusTransportState _state = JanusTransportState.Closed;

        public JanusTransportState State
        {
            get { lock (_stateLock) return _state; }
        }

        public event EventHandler Opened;
        public event EventHandler<string> Message;
        public event EventHandler<string> Error;
        public event EventHandler<string> Closed;

        public JanusWebSocketTransport(Uri uri, string subprotocol = "janus-protocol", Dictionary<string, string> headers = null)
        {
            _uri = uri;
            _subprotocols = new List<string>();
            if (!string.IsNullOrEmpty(subprotocol))
                _subprotocols.Add(subprotocol);
            _headers = headers ?? new Dictionary<string, string>();
        }

        public Task OpenAsync(CancellationToken cancellation = default)
        {
            lock (_stateLock)
            {
                if (_state == JanusTransportState.Open || _state == JanusTransportState.Connecting)
                    throw new InvalidOperationException("Transport is already opening or open.");
                _state = JanusTransportState.Connecting;
            }

            _openTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _socket = new VaSiLi.WebSocket.WebSocket(_uri.ToString(), _subprotocols, _headers);

            _socket.OnOpen += HandleOpen;
            _socket.OnMessage += HandleMessage;
            _socket.OnError += HandleError;
            _socket.OnClose += HandleClose;

            _ = Task.Run(async () =>
            {
                try
                {
                    await _socket.Connect();
                }
                catch (Exception ex)
                {
                    HandleError(ex.Message);
                }
            }, cancellation);

            return _openTcs.Task;
        }

        public async Task SendAsync(string text, CancellationToken cancellation = default)
        {
            if (State != JanusTransportState.Open)
                throw new JanusTransportException("Transport is not open.");
            await _socket.SendText(text);
        }

        public async Task CloseAsync(CancellationToken cancellation = default)
        {
            lock (_stateLock)
            {
                if (_state == JanusTransportState.Closed) return;
                _state = JanusTransportState.Closing;
            }
            if (_socket != null)
                await _socket.Close();
        }

        void HandleOpen()
        {
            lock (_stateLock) _state = JanusTransportState.Open;
            _openTcs?.TrySetResult(true);
            Opened?.Invoke(this, EventArgs.Empty);
        }

        void HandleMessage(byte[] data, int offset, int length)
        {
            try
            {
                var text = Encoding.UTF8.GetString(data, offset, length);
                Message?.Invoke(this, text);
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, "Failed to decode message: " + ex.Message);
            }
        }

        void HandleError(string message)
        {
            _openTcs?.TrySetException(new JanusTransportException(message));
            Error?.Invoke(this, message);
        }

        void HandleClose(Meta.Net.NativeWebSocket.WebSocketCloseCode code)
        {
            lock (_stateLock) _state = JanusTransportState.Closed;
            _openTcs?.TrySetException(new JanusTransportException("Closed before open: " + code));
            Closed?.Invoke(this, code.ToString());
        }
    }
}
