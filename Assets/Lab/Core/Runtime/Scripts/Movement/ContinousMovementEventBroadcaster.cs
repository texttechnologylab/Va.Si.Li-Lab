/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Oculus.Interaction;
using Oculus.Interaction.Input;
using Oculus.Interaction.Locomotion;
using System;
using UnityEngine;

namespace VaSiLi.Movement
{
    /// <summary>
    /// This component transforms an Axis value into Locomotion turning events.
    /// The moment at which the events are emitted depends on an Interactor State.
    /// When using Snap turning mode the event is sent once during Select while
    /// on Smooth turning mode it is processed continuously during select.
    /// </summary>
    public class ContinousMovementEventBroadcaster : MonoBehaviour,
        ILocomotionEventBroadcaster
    {

        /// <summary>
        /// Interactor that the state is read from so that when it selects, the events are fired.
        /// </summary>
        [SerializeField, Interface(typeof(IInteractor))]
        [Tooltip("The interactor defines when the Locomotion events are sent based on its Select state.")]
        private UnityEngine.Object _interactor;
        private IInteractor Interactor { get; set; }

        /// <summary>
        /// 1D axis (from -1 to 1) to read in order to produce the direction and strength of the turn.
        /// </summary>
        [SerializeField, Interface(typeof(IAxis2D))]
        [Tooltip("Axis from -1 to 1 indicating the turning direction and velocity.")]
        private UnityEngine.Object _axis;
        private IAxis2D Axis { get; set; }

        [SerializeField]
        [Tooltip("Degrees to continuously rotate during selection when in Smooth turn mode, it is remapped from the Axis value")]
        private AnimationCurve _smoothTurnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 100f);
        /// <summary>
        /// Degrees to continuously rotate during selection when in Smooth turn mode, it is remapped from the Axis value
        /// </summary>
        public AnimationCurve SmoothTurnCurve
        {
            get
            {
                return _smoothTurnCurve;
            }
            set
            {
                _smoothTurnCurve = value;
            }
        }

        [SerializeField]
        [Tooltip("When enabled, snap turn happens on unselect. If false it happens on select")]
        private bool _fireSnapOnUnselect = true;
        /// <summary>
        /// When enabled, snap turn happens on unselect. If false it happens on select
        /// </summary>
        public bool FireSnapOnUnselect
        {
            get
            {
                return _fireSnapOnUnselect;
            }
            set
            {
                _fireSnapOnUnselect = value;
            }
        }

        private UniqueIdentifier _identifier;
        public int Identifier => _identifier.ID;

        protected bool _started;

        protected virtual void Awake()
        {
            _identifier = UniqueIdentifier.Generate();

            Interactor = _interactor as IInteractor;
            Axis = _axis as IAxis2D;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Interactor, nameof(Interactor));
            this.AssertField(Axis, nameof(Axis));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Interactor.WhenPostprocessed += HandlePostprocessed;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Interactor.WhenPostprocessed -= HandlePostprocessed;
            }
        }

        private Action<LocomotionEvent> _whenLocomotionEventRaised = delegate { };
        public event Action<LocomotionEvent> WhenLocomotionPerformed
        {
            add
            {
                _whenLocomotionEventRaised += value;
            }
            remove
            {
                _whenLocomotionEventRaised -= value;
            }
        }

        private void HandlePostprocessed()
        {
            if (Interactor.State == InteractorState.Select)
            {
                ProcessSmoothTurn(Axis.Value());
            }

        }

        private void ProcessSmoothTurn(Vector2 pointerOffset)
        {
            Vector3 velocity = new Vector3(_smoothTurnCurve.Evaluate(pointerOffset.x), 0f, _smoothTurnCurve.Evaluate(pointerOffset.y));
            LocomotionEvent locomotionEvent = new LocomotionEvent(
                Identifier, velocity, LocomotionEvent.TranslationType.Velocity);
            _whenLocomotionEventRaised.Invoke(locomotionEvent);
        }

        #region Inject
        public void InjectAllTurnerEventBroadcaster(IInteractor interactor,
            IAxis2D axis)
        {
            InjectInteractor(interactor);
            InjectAxis(axis);
        }

        public void InjectInteractor(IInteractor interactor)
        {
            _interactor = interactor as UnityEngine.Object;
            Interactor = interactor;
        }

        public void InjectAxis(IAxis2D axis)
        {
            _axis = axis as UnityEngine.Object;
            Axis = axis;
        }
        #endregion
    }
}
