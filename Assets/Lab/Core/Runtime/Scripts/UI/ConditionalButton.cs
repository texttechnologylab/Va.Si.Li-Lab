using UnityEngine;
using UnityEngine.Events;

namespace VaSiLi.UI
{
    /// <summary>
    /// Wraps a validation step around a set of actions.
    /// Call <see cref="Press"/> (e.g. from a Button onClick). If the registered
    /// <see cref="Condition"/> returns true (or no condition is set), <see cref="OnSuccess"/>
    /// fires. If validation fails, <see cref="OnFailure"/> fires instead.
    ///
    /// Wire <see cref="OnSuccess"/> in the Inspector exactly as you would a normal Button's
    /// onClick — it can call as many methods as needed (SwitchPanel, Join, etc.).
    /// </summary>
    public class ConditionalButton : MonoBehaviour
    {
        [Tooltip("Fires when Press() is called and the condition passes.")]
        public UnityEvent OnSuccess;

        [Tooltip("Fires when Press() is called and the condition fails.")]
        public UnityEvent OnFailure;

        // Programmatically assigned validator — returns true to allow, false to block.
        // Leave null to always pass.
        public System.Func<bool> Condition { get; set; }

        /// <summary>
        /// Evaluate the condition and fire OnSuccess or OnFailure accordingly.
        /// Wire this to a Button's onClick in the Inspector.
        /// </summary>
        public void Press()
        {
            if (Condition == null || Condition.Invoke())
                OnSuccess?.Invoke();
            else
                OnFailure?.Invoke();
        }
    }
}
