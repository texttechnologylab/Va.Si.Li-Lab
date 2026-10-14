using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace VaSiLi.Interaction
{
    public class Door : MonoBehaviour
    {
        [FormerlySerializedAs("IsOpen")]
        public bool isOpen;

        private bool _isRotatingDoor = true;
        private float _speed = 1f;
        private float _rotationAmount = 90f;
        private float _forwardDirection = 0f;

        private Vector3 _startRotation;
        private Vector3 _forward;
        private Coroutine _animationCoroutine;

        private void Awake()
        {
            _startRotation = transform.rotation.eulerAngles;
            _forward = transform.right;
        }

        public void Open(Vector3 userPosition)
        {
            if (!isOpen)
            {
                if (_animationCoroutine != null)
                {
                    StopCoroutine(_animationCoroutine);
                }
            }

            if (_isRotatingDoor)
            {
                float dot = Vector3.Dot(_forward, (userPosition - transform.position).normalized);
                _animationCoroutine = StartCoroutine(DoRotationOpen(dot));
            }
        }

        public void Close()
        {
            if (isOpen)
            {
                if (_animationCoroutine != null)
                {
                    StopCoroutine(_animationCoroutine);
                }
            }

            if (_isRotatingDoor)
            {
                _animationCoroutine = StartCoroutine(DoRotationClose());
            }
        }

        private IEnumerator DoRotationOpen(float forwardAmount)
        {
            Quaternion startRotation = transform.rotation;
            Quaternion endRotation;

            if (forwardAmount >= _forwardDirection)
            {
                endRotation = Quaternion.Euler(new Vector3(0, _startRotation.y - _rotationAmount, 0));
            }
            else
            {
                endRotation = Quaternion.Euler(new Vector3(0, _startRotation.y + _rotationAmount, 0));
            }

            isOpen = true;
            float time = 0;
            while (time < 1)
            {
                transform.rotation = Quaternion.Slerp(startRotation, endRotation, time);
                yield return null;
                time += Time.deltaTime * _speed;
            }
        }

        private IEnumerator DoRotationClose()
        {
            Quaternion startRotation = transform.rotation;
            Quaternion endRotation = Quaternion.Euler(_startRotation);

            isOpen = false;
            float time = 0;
            while (time < 1)
            {
                transform.rotation = Quaternion.Slerp(startRotation, endRotation, time);
                yield return null;
                time += Time.deltaTime * _speed;
            }
        }
    }
}
