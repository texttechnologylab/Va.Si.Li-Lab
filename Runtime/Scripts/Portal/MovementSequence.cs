using UnityEngine;
using System.Collections.Generic;

namespace Oculus.Interaction
{
    /**
     * MovementSequence
     *
     * @author Vujevic
     *
     * This class is used to check if a sequence of IActiveState is active in order.
     * It can be used to create a sequence of actions that need to be performed in order to activate something.
     */
    public class MovementSequence : MonoBehaviour, IActiveState
    {
        [SerializeField, Interface(typeof(IActiveState))]
        /**
         * List of IActiveState that need to be active in order for the sequence to be active.
         */
        private List<UnityEngine.Object> _activeStates;
        [Header("Timer Settings")]
        [SerializeField]
        /**
         * If true, the sequence will reset if the next step is not activated within the step delay time.
         */
        private bool _useTimer = false;

        [SerializeField]
        /**
         * Time in seconds before the sequence resets if the next step is not activated.
         */
        private float _stepDelay = 1f; //seconds

        /**
         * Timer to keep track of the time since the last step was activated.
         */
        private float _timer = 0f;
        
        /**
         * List of IActiveState that need to be active in order for the sequence to be active.
         */
        private List<IActiveState> _states;
        /**
         * Current index of the sequence.
         */
        private int _currentIndex = 0;

        /**
         * Convert the list of active states to IActiveState.
         */
        protected void Awake()
        {
            _states = _activeStates.ConvertAll(o => o as IActiveState);
        }

        /**
         * Check if the sequence is active.
         */
        public bool Active
        {
            get
            {
                if (_states == null || _states.Count == 0)
                    return false;

                if (_useTimer)
                {
                    _timer -= Time.deltaTime;
                    if (_timer < 0f)
                    {
                        ResetSequence();
                        return false;
                    }   
                } 

                
                if (_states[_currentIndex].Active)
                {
                    _currentIndex++;
                    if (_useTimer)
                        _timer = _stepDelay;

                    if (_currentIndex >= _states.Count)
                    {
                        _currentIndex = 0; 
                        return true;
                    }
                }

                return false;
            }
        }

        /**
         * Reset the sequence to the initial state.
         */
        public void ResetSequence()
        {
            _currentIndex = 0;
            _timer = _stepDelay;
        }
    }
}
