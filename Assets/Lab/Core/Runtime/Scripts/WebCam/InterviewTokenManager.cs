using UnityEngine;

namespace VaSiLi.WebCam
{
    /// <summary>
    /// Persists the interview token across scene loads and derives the Janus room ID,
    /// Ubiq room name, and browser URL from it.
    ///
    /// Token-to-int conversion matches the website's roomIdFromString() in state.js:
    ///   - Purely numeric string → parse as int
    ///   - Otherwise → DJB2 hash (seed 5381, multiplier 33), abs value, minimum 1
    /// </summary>
    public class InterviewTokenManager : MonoBehaviour
    {
        public static InterviewTokenManager Instance { get; private set; }

        [Tooltip("Base URL of the questionnaire page. Token is appended as ?token={token}.")]
        public string BrowserBaseUrl = "https://faces.befragung.neps-studie.de/1259000";

        public string Token { get; private set; } = "";

        /// <summary>Fires whenever <see cref="SetToken"/> stores a new value. Subscribers
        /// (e.g. the browser URL loader) can react late-binding without having to poll.</summary>
        public event System.Action<string> TokenChanged;

        /// <summary>Numeric Janus room ID derived from Token.</summary>
        public long RoomId { get; private set; }

        /// <summary>Ubiq room name: "interview-{token}".</summary>
        public string UbiqRoomName => string.IsNullOrEmpty(Token) ? null : "interview-" + Token;

        /// <summary>Full browser URL with token appended.</summary>
        /// <remarks>
        /// The website reads its Janus room from ?ID= or ?room= (state.js), so we pass the token
        /// as both ?token= (for the questionnaire) and ?room= (so the page joins the correct Janus
        /// room and textroom, matching the numeric room ID Unity uses).
        /// </remarks>
        public string BrowserUrl => string.IsNullOrEmpty(Token)
            ? BrowserBaseUrl
            : BrowserBaseUrl
              + "?token=" + System.Uri.EscapeDataString(Token)
              + "&room=" + System.Uri.EscapeDataString(Token);

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetToken(string token)
        {
            Token = (token ?? "").Trim();
            RoomId = RoomIdFromString(Token);
            Debug.Log($"[InterviewToken] token='{Token}' roomId={RoomId} ubiqRoom='{UbiqRoomName}'");
            TokenChanged?.Invoke(Token);
        }

        public static long RoomIdFromString(string s)
        {
            var str = (s ?? "").Trim();
            if (str.Length == 0) return 0;

            // Purely numeric → parse directly
            if (System.Text.RegularExpressions.Regex.IsMatch(str, @"^\d+$"))
            {
                if (long.TryParse(str, out var n)) return n;
            }

            // DJB2 hash (matches JavaScript's 32-bit signed arithmetic)
            int h = 5381;
            foreach (char c in str)
                h = unchecked((h * 33) ^ c);
            return System.Math.Abs(h) == 0 ? 1 : System.Math.Abs(h);
        }
    }
}
