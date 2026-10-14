
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VaSiLi.Logging
{
    [Serializable]
    public class MovementMessage
    {
        public int timstamp;
        public PositionMessage player;
        public PositionMessage playerCamera;
        public PositionMessage leftHand;
        public PositionMessage rightHand;
    }
    public class MovementLogCollector : MonoBehaviour, IContinousLogCollector<MovementMessage>
    {
        public float PollingRate { get => _pollingRate; }
        [SerializeField]
        private float _pollingRate;
        public string Endpoint { get => _endpoint; }
        [SerializeField]
        private string _endpoint;

        [SerializeField]
        private Transform player;
        [SerializeField]
        private Transform playerCamera;
        [SerializeField]
        private Transform leftHand;
        [SerializeField]
        private Transform rightHand;

        private List<MovementMessage> messages = new List<MovementMessage>();
        public void PollData(float timestamp)
        {
            /*messages.Add(new MovementMessage()
            {
                timstamp = (int)timestamp,
                player = new PositionMessage(player.position),
                playerCamera = new PositionMessage(playerCamera.position),
                leftHand = new PositionMessage(leftHand.position),
                rightHand = new PositionMessage(rightHand.position)
            }); */
        }

        public MovementMessage GetMessage()
        {
            throw new System.NotImplementedException();
        }
    }

}