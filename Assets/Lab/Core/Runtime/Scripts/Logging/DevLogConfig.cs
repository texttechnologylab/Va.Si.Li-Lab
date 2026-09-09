using UnityEngine;

namespace VaSiLi.Logging
{
    /// <summary>
    /// Disables Unity's per-log stack-trace capture in development builds.
    ///
    /// Stack-trace capture is expensive on Android — every Debug.Log/LogWarning/LogError walks the
    /// managed stack. When something logs each frame (e.g. the Meta Avatar SDK's per-frame error in
    /// dev builds) this both costs real CPU and inflates Profiler captures. Disabling it gives a
    /// representative "release-like" capture while the Profiler stays attached (which needs a
    /// development build).
    ///
    /// Unity exposes no reliable runtime "is the Profiler connected" flag, so we can't scope this to
    /// profiling automatically. Instead it's gated to development builds and exposed as a toggle.
    /// In release builds this component does nothing — stack-trace behaviour is left untouched.
    ///
    /// Usage: place on a GameObject in your startup scene. Toggle off when you actually need stack
    /// traces while debugging. (If you'd rather drive it from a build flag, gate the body behind a
    /// custom scripting define like PROFILING and set it only for profiling builds.)
    /// </summary>
    public class DevLogConfig : MonoBehaviour
    {
        [Tooltip("Development builds only: when on, disables stack-trace capture for Log/Warning/Error. " +
                 "Exception and Assert keep their traces. Turn off if you need traces while debugging.")]
        [SerializeField] private bool disableLogStackTraces = true;

        void Awake()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (!disableLogStackTraces)
                return;

            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
            // Exception/Assert keep their stack traces — they're rare and the trace is worth keeping.

            Debug.Log("[DevLogConfig] Log/Warning/Error stack traces disabled for this development build.");
#endif
        }
    }
}
