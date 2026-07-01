using System;
using System.Collections.Generic;

namespace VaSiLi.WebCam
{
    /// <summary>
    /// Single source of truth for how answer-option batches are filtered before display.
    /// Ports the web frontend's <c>filterAnswerOptions</c> / <c>normalizeAnswerData</c>
    /// (faces-website/janus-html/js/answer-options.js) 1:1 so the Quest interviewee
    /// renders exactly what the website would, and the interviewer can run the SAME
    /// check to decide whether a batch actually shows concrete options on the other end.
    ///
    /// Kept as a plain static class (no MonoBehaviour) so both <see cref="WebViewReceiver"/>
    /// (interviewee display) and <see cref="WebCamClient"/> (interviewer displayability
    /// check + chat echo) share one implementation and can never drift apart.
    /// </summary>
    public static class AnswerOptionFilter
    {
        // Substrings that mark a batch as "not concrete answer options" — if ANY entry
        // contains one of these, the whole batch is suppressed (free-answer case).
        // Matches the website's blocklist verbatim (case-insensitive, like its toLowerCase()).
        static readonly string[] BlockTerms =
        {
            "zp gibt",
            "interviewer",
            "ledig",
            "ja",
            "stunden",
            "gerät",
            "smartphone",
            "zp",
            "diplom",
            "erwärbstätig",
            "weniger als",
            "nicht genannt",
            "1.500 bis unter 3.000 Euro",
            "500 bis unter 1.000 Euro",
            "4.000 bis unter 5.000 Euro ",
        };

        // Entries containing one of these are silently dropped (but don't suppress the batch).
        static readonly string[] SkipTerms =
        {
            "weiß nicht",
            "undefined",
        };

        /// <summary>
        /// Apply the frontend filter to a batch of entries.
        /// Returns the entries that would actually be DISPLAYED, or <c>null</c> when the
        /// batch resolves to "no concrete options" (a block term matched, or more than
        /// six non-scale options remain) — the free-answer case.
        /// </summary>
        public static List<string> Filter(IEnumerable<string> entries)
        {
            if (entries == null) return null;

            var filtered = new List<string>();
            foreach (var textEntry in entries)
            {
                if (string.IsNullOrWhiteSpace(textEntry)) continue;

                if (ContainsAny(textEntry, BlockTerms))
                    return null;

                if (ContainsAny(textEntry, SkipTerms))
                    continue;

                filtered.Add(textEntry);
            }

            // The website caps concrete option lists at six; longer non-scale lists are
            // treated as free-answer (e.g. long pick-lists) and suppressed.
            if (filtered.Count > 6 && !IsNumericScale(filtered))
                return null;

            return filtered;
        }

        /// <summary>True when the batch is an 11-point 0–10 scale (each entry starts with its index).</summary>
        public static bool IsNumericScale(IReadOnlyList<string> entries)
        {
            if (entries == null || entries.Count != 11) return false;
            for (int i = 0; i < entries.Count; i++)
                if (!entries[i].TrimStart().StartsWith(i.ToString(), StringComparison.Ordinal))
                    return false;
            return true;
        }

        /// <summary>Strip everything after the first newline (the descriptive second line), like the rendered rows.</summary>
        public static string FilterText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            int idx = text.IndexOf('\n');
            return idx >= 0 ? text.Substring(0, idx) : text;
        }

        static bool ContainsAny(string text, string[] terms)
        {
            foreach (var term in terms)
                if (text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }
    }
}
