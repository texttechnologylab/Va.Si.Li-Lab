using UnityEngine;
using System.Collections;
using UnityEngine.Events;

namespace VaSiLi.Logging
{
    public class LogToScreen : MonoBehaviour
    {
        uint qsize = 15;  // number of messages to keep
        Queue myLogQueue = new Queue();
        public static UnityAction<string, LogType, string> logFileData = delegate { };
        public bool _enabled;

        void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            logFileData.Invoke(logString, type, stackTrace);
            if (!_enabled)
                return;
            myLogQueue.Enqueue("[" + type + "] : " + logString);
            //if (type == LogType.Exception)
            //myLogQueue.Enqueue(stackTrace);
            while (myLogQueue.Count > qsize)
                myLogQueue.Dequeue();
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 400, Screen.height - 200, 400, 200));
            GUILayout.Label("\n" + string.Join("\n", myLogQueue.ToArray()));
            GUILayout.EndArea();
        }
    }
}