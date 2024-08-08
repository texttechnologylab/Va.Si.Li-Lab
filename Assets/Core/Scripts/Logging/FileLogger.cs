using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace VaSiLi.Logging
{
    /// <summary>
    /// Class to aid in logging json messages to the local file system
    /// All messages are enqueued and process asynchronisly
    /// </summary>
    public class FileLogger
    {
        private static string localPath = Application.persistentDataPath;
        private static Dictionary<string, StreamWriter> filestreams = new Dictionary<string, StreamWriter>();
        private static Dictionary<string, Queue<MessageBase>> queuedFiles = new Dictionary<string, Queue<MessageBase>>();
        private static Task messageProcessor;
        public static void QueueForWrite(MessageBase message)
        {
            var key = CreateKey(message);
            if (queuedFiles.ContainsKey(key))
            {
                queuedFiles[key].Enqueue(message);
            }
            else
            {
                var queue = new Queue<MessageBase>();
                queue.Enqueue(message);
                queuedFiles.Add(key, queue);
            }
            //TODO: There is a small chance this can mean that a message doesn't get logged in the case the task has run too far
            if (messageProcessor == null || messageProcessor.Status != TaskStatus.Running)
            {
                messageProcessor = ProcessMessages();
                messageProcessor.ContinueWith((task) => messageProcessor.Dispose());
            }

        }

        private static string CreateKey(MessageBase baseMessage)
        {
            return $"{baseMessage.playerId}_{baseMessage.GetType()}";
        }

        /// <summary>
        /// Writes the message to disk
        /// </summary>
        /// <param name="roomid">The current room id of a player</param>
        /// <param name="message">The message</param>
        /// <typeparam name="T">A valid serializable message type</typeparam>
        /// <returns></returns>
        public static async Task Write<T>(string roomid, T message)
        {
            var key = $"{roomid}_{message.GetType()}";
            // If we've already created a file stream for this key
            if (filestreams.ContainsKey(key))
            {
                try
                {
                    await filestreams[key].WriteAsync(JsonUtility.ToJson(message));
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            }
            // Create a new file stream
            else
            {
                try
                {
                    var currentPath = Path.Combine(localPath, key + ".json");
                    FileStream fileStream = new FileStream(currentPath, FileMode.Append, FileAccess.Write, FileShare.Write);
                    fileStream.Close();
                    StreamWriter streamWriter = new StreamWriter(currentPath, true);
                    filestreams.Add(key, streamWriter);
                    await filestreams[key].WriteAsync(JsonUtility.ToJson(message));
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            }
        }

        private static async Task ProcessMessages()
        {
            var tasks = new List<Task>();
            foreach (string key in queuedFiles.Keys)
            {
                var file = queuedFiles[key];
                if (filestreams.ContainsKey(key))
                {
                    tasks.Add(WriteMessages(filestreams[key], file));
                }
                else
                {
                    var currentPath = Path.Combine(localPath, key + ".json");
                    FileStream fileStream = new FileStream(currentPath, FileMode.Append, FileAccess.Write, FileShare.Write);
                    fileStream.Close();
                    StreamWriter streamWriter = new StreamWriter(currentPath, true);
                    filestreams.Add(key, streamWriter);
                    tasks.Add(WriteMessages(streamWriter, file));
                }
            }
            await Task.WhenAll(tasks.ToArray());
        }

        private static async Task WriteMessages(StreamWriter stream, Queue<MessageBase> messages)
        {
            while (messages.Count > 0)
            {
                Debug.Log("Writing message");
                var message = messages.Dequeue();
                await stream.WriteAsync(JsonUtility.ToJson(message));
            }
        }

        /// <summary>
        /// Make sure to only fully exit the application once we've written all files and disposed of the stream
        /// </summary>
        public static void OnApplicationQuit()
        {
            Debug.Log("App quit");
            while (messageProcessor != null && messageProcessor.Status == TaskStatus.Running)
            {

            }
            foreach (var filestream in filestreams)
            {
                filestream.Value.Dispose();
            }
        }
    }
}