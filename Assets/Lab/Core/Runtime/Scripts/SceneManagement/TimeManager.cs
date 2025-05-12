using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using System;
using Ubiq.Samples;
using Ubiq.Messaging;
using Ubiq.Rooms;

namespace VaSiLi.SceneManagement
{
    /// <summary>
    /// Manages a timestate with the maximum derived from the ApiInfo object 
    /// Provides methods to start/reset the timer as well as appropriate actions
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        public static int maxSceneDurationMinutes = 30;
        private int currentSceneDurationMinutes;
        private bool timerRunning = false;
        private NetworkContext context;
        public RoomClient roomClient;
        public static UnityAction<int> timerUpdated = delegate { };
        public static UnityAction timerStarted = delegate { };
        public static UnityAction timerReset = delegate { };
        public static UnityAction timerStopped = delegate { };

        private struct SynchMessage
        {
            public int minutes;
        }

        void Start()
        {
            SceneManager.infosUpdated += OnInfosUpdated;
            LevelManager.levelStatus += OnLevelStatusChanged;
            SceneManager.UpdateInfos().ContinueWith((info) => { });
            context = NetworkScene.Register(this);
            // Register the synch function
            roomClient.OnPeerAdded.AddListener(SynchronizeData);
        }

        /// <summary>
        /// If a peer joins synchronize the current running time with them
        /// </summary>
        /// <param name="_peer"></param>
        private void SynchronizeData(IPeer _peer)
        {
            context.SendJson(new SynchMessage()
            {
                minutes = currentSceneDurationMinutes
            });
        }

        private void OnInfosUpdated(ApiInfos[] infos)
        {
            var time = infos.FirstOrDefault(info => info.mode == "timeEnd").description;
            try
            {
                maxSceneDurationMinutes = Int32.Parse(time);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        private void OnLevelStatusChanged(int level, LevelManager.Status status)
        {
            if (level <= 0 && status == LevelManager.Status.RUNNING)
            {
                StartTimer();
                ResetTimer();
            }
            else if (status == LevelManager.Status.RUNNING)
            {
                StartTimer();
            }
        }

        /// <summary>
        /// Resets the timer to 0 minutes
        /// </summary>
        private void ResetTimer()
        {
            Debug.Log("Reset Timer");
            timerReset.Invoke();
            currentSceneDurationMinutes = 0;
        }

        /// <summary>
        /// Starts the timer if it's not already running
        /// </summary>
        private void StartTimer()
        {
            if (!timerRunning)
            {
                Debug.Log("Start Timer");
                timerRunning = true;
                StartCoroutine(TimerTask());
                timerStarted.Invoke();
            }
        }

        /// <summary>
        /// The actual timer
        /// </summary>
        /// <returns>void</returns>
        private IEnumerator TimerTask()
        {
            while (timerRunning)
            {
                yield return new WaitForSeconds(60);
                currentSceneDurationMinutes++;
                timerUpdated.Invoke(currentSceneDurationMinutes);
                if (currentSceneDurationMinutes >= maxSceneDurationMinutes)
                {
                    timerRunning = false;
                    timerStopped.Invoke();
                }
            }
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var m = message.FromJson<SynchMessage>();
            currentSceneDurationMinutes = m.minutes;
        }

    }
}