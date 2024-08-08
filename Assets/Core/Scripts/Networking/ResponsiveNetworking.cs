using Ubiq.Messaging;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Ubiq.Rooms;
using System;
using System.Security.Cryptography;
using System.Text;

namespace VaSiLi.Networking
{
    /// <summary>
    /// Class <c>ResponsiveNetworking</c> is a Utility class to allow for networking requests
    /// that trigger a callback function once it has been completed. This callback function contains
    /// information on whether all <c>Peers</c> could process the request or not.
    /// </summary>
    public class ResponsiveNetworking : MonoBehaviour
    {
        public delegate void JsonCallBack(CallbackResult result);
        public NetworkScene networkScene;
        private static NetworkContext context;
        private static Dictionary<string, MessageData> messageData = new Dictionary<string, MessageData>();
        public static RoomClient roomClient;
        // Incrementing counter to aid making sure each request is uniquely identifiable
        private static int tally;
        private static int Tally
        {
            get { return tally; }
            set
            {
                if (value >= 1000)
                {
                    tally = 0;
                }
                else
                {
                    tally = value;
                }
            }
        }

        private enum MessageType
        {
            Request,
            Response
        }

        public enum Status
        {
            Success,
            ProcessNotFound,
            GenericError
        }

        /// <summary>
        /// The structure of the result that gets sent back once all requests have been processed
        /// </summary>
        public struct CallbackResult
        {
            public bool success;
            public object message;
            public Dictionary<string, Status> responses { get; }
            public object context;
            public CallbackResult(bool success, object message, Dictionary<string, Status> results, object payload)
            {
                this.success = success;
                this.message = message;
                this.responses = results;
                this.context = payload;
            }

            public CallbackResult(object message, object payload)
            {
                this.success = false;
                this.message = message;
                this.responses = new Dictionary<string, Status>();
                this.context = payload;
            }
        }
        /// <summary>
        /// Stores data for a request that has been made to keep track of things
        /// </summary>
        private struct MessageData
        {
            public List<string> messageQueue;
            public JsonCallBack callBack;
            public CallbackResult result;
            public MessageData(List<string> messageQueue, JsonCallBack callBack, CallbackResult results)
            {
                this.messageQueue = messageQueue;
                this.callBack = callBack;
                this.result = results;
            }
        }
        /// <summary>
        /// A wrapper around a generic message.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private struct MessageWrapper<T>
        {
            public MessageType type;
            public string uniqueIdentifier;
            public string peer;
            public string message;
            public T Message
            {
                get { return JsonUtility.FromJson<T>(message); }
                set { message = JsonUtility.ToJson(value); }
            }

            public MessageWrapper(MessageType type, string uniqueIdentifier, string peer, T message)
            {
                this.type = type;
                this.uniqueIdentifier = uniqueIdentifier;
                this.peer = peer;
                this.message = JsonUtility.ToJson(message);
            }
        }

        private struct Request
        {
            public NetworkId targetProcess;
            public string message;

            public Request(NetworkId targetProcess, object message)
            {
                this.targetProcess = targetProcess;
                this.message = JsonUtility.ToJson(message);
            }
        }

        private struct Response
        {
            public Status status;
            public string err;

            public Response(Status rejected, string err)
            {
                this.status = rejected;
                this.err = err;
            }
        }

        protected void Awake()
        {
            if (context.Scene == null)
                PrepareScene();
        }

        public void PrepareScene()
        {
            context.Scene = networkScene;
            using (var sha1 = new SHA1Managed())
            {
                context.Id = new NetworkId(sha1.ComputeHash(Encoding.UTF8.GetBytes("Responsive Networking")), 0);
            }
            context.Component = this;
            context.Scene.AddProcessor(context.Id, ProcessMessage);
            roomClient = networkScene.GetComponent<RoomClient>();
        }

        /// <summary>
        /// Sends a network request to all connected clients 
        /// </summary>
        /// <typeparam name="T">The type of the message</typeparam>
        /// <param name="processTarget">The networkid linking to the message processsor</param>
        /// <param name="message">The message that should be sent</param>
        /// <param name="clb">The callback function which get's called once all requests have been received</param>
        /// <param name="payload">Additional data that will persist locally</param>
        public static void SendJson<T>(NetworkId processTarget, T message, JsonCallBack clb, object payload = null)
        {
            if (!context.Scene)
            {
                throw new Exception("No valid network scene could be found. Make sure the script is attached to a component");
            }

            // Generate a unique identifier
            string unique = processTarget.ToString() + Tally;
            Tally += 1;
            // Prepare the request
            Request request = new Request(processTarget, message);
            MessageWrapper<Request> msg = new MessageWrapper<Request>(MessageType.Request, unique, roomClient.Me.uuid, request);
            // Add an entry to the queue to track which peer has received the message
            MessageData data = new MessageData(roomClient.Peers.Select(peer => peer.uuid).ToList(), clb, new CallbackResult(message, payload));
            messageData.Add(unique, data);
            // Send the message
            context.SendJson(msg);
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<MessageWrapper<object>>();
            if (msg.type == MessageType.Request)
            {
                // Convert the message to it's correct type
                Request requestMsg = message.FromJson<MessageWrapper<Request>>().Message;
                var processors = context.Scene.GetProcessors();
                // Attempt to obtain the target processor
                var processor = processors.FirstOrDefault(item => item.Value == requestMsg.targetProcess).Key;
                if (processor != null)
                {
                    // If we could find the processor
                    try
                    {
                        // Prepare response message
                        Response response = new Response(Status.Success, "");
                        MessageWrapper<Response> responseMessage = new MessageWrapper<Response>(MessageType.Response, msg.uniqueIdentifier, roomClient.Me.uuid, response);

                        // Feed the attached message to the local processor
                        var currMessage = ReferenceCountedSceneGraphMessage.Rent(requestMsg.message);
                        currMessage.objectid = requestMsg.targetProcess;
                        processor(currMessage);

                        // Respond
                        context.SendJson(responseMessage);
                    }
                    catch (Exception ex)
                    {
                        Response response = new Response(Status.GenericError, ex.ToString());
                        MessageWrapper<Response> responseMessage = new MessageWrapper<Response>(MessageType.Response, msg.uniqueIdentifier, roomClient.Me.uuid, response);
                        context.SendJson(responseMessage);
                    }
                }
                else
                {
                    // If we couldn't find the processor
                    Response response = new Response(Status.ProcessNotFound, "");
                    MessageWrapper<Response> responseMessage = new MessageWrapper<Response>(MessageType.Response, msg.uniqueIdentifier, roomClient.Me.uuid, response);
                    context.SendJson(responseMessage);
                }
            }
            else if (msg.type == MessageType.Response)
            {
                // Convert the message to it's correct type
                Response messageResponse = message.FromJson<MessageWrapper<Response>>().Message;
                MessageData data = messageData.FirstOrDefault(item => item.Key == msg.uniqueIdentifier).Value;
                // If we just joined a scene we might not have added the message to the list
                if (data.Equals(default(MessageData)))
                    return;
                data.messageQueue.Remove(msg.peer);
                data.result.responses.Add(msg.peer, messageResponse.status);

                // If all the messages have been received
                if (data.messageQueue.Count == 0)
                {
                    var clb = data.callBack;
                    if (data.result.responses.Values.All(status => status == Status.Success))
                        data.result.success = true;
                    clb(data.result);
                    // Remove entry from list
                    messageData.Remove(msg.uniqueIdentifier);
                }
            }
        }
    }
}