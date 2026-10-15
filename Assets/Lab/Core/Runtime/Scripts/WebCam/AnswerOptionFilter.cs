using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
        /// <summary>Maximum concrete options shown; longer non-scale lists become free-answer.</summary>
        public const int MaxDisplayedOptions = 6;

        /// <summary>A reason a batch was suppressed, for display to the interviewer.</summary>
        public sealed class SuppressionRule
        {
            public readonly string Name;
            public readonly string Label;
            public readonly Regex Test;
            public SuppressionRule(string name, string label, string pattern)
            {
                Name = name;
                Label = label;
                Test = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
        }

        // Question types whose option lists are deliberately NOT shown to the participant —
        // either because the interviewer reads them aloud, or because seeing the brackets
        // would bias the answer (income, employment status).
        //
        // These are ANCHORED regexes, not substring tests. The previous IndexOf() checks
        // fired on ordinary words that merely contained the token: "ja" matched "Jahre" and
        // "Januar", "euro" matched "Europa", "ledig" matched "lediglich", "gerät" matched
        // inside longer words. A single accidental hit suppressed the whole batch, which on
        // the interviewer's side is indistinguishable from a delivery failure — so it made
        // real transport problems harder to diagnose.
        //
        // Kept byte-for-byte equivalent to SUPPRESSION_RULES in the web frontend's
        // answer-options.js. Change both or neither.
        static readonly SuppressionRule[] Rules =
        {
            new SuppressionRule("zp-statement",   "ZP-Angabe",                @"\bzp\b"),
            new SuppressionRule("interviewer",    "Interviewer-Hinweis",      @"\binterviewer"),
            new SuppressionRule("marital-status", "Familienstand",            @"\bledig\b"),
            new SuppressionRule("yes-no",         "Ja/Nein-Frage",            @"\bja\b"),
            new SuppressionRule("hours",          "Stundenangabe",            @"stunden\b"),
            // Prefix match so compounds like "Gerätetyp" still hit (the old substring test
            // caught those). Requiring the umlaut keeps the unrelated verb "geraten" out.
            new SuppressionRule("device",         "Gerätetyp",                @"\bger(?:ä|ae)t"),
            new SuppressionRule("device",         "Gerätetyp",                @"\bsmartphone"),
            new SuppressionRule("education",      "Bildungsabschluss",        @"\bdiplom"),
            new SuppressionRule("employment",     "Erwerbstätigkeit",         @"\berw[eä]rbst[äa]tig"),
            new SuppressionRule("open-bound",     "Offene Kategoriegrenze",   @"\bweniger als\b"),
            new SuppressionRule("not-stated",     "Nicht-genannt-Kategorie",  @"\bnicht genannt\b"),
            // One general rule replaces the old per-bracket literals ("1.500 bis unter
            // 3.000 Euro" and friends), which only ever covered the exact strings they named.
            new SuppressionRule("income",         "Einkommensklasse",         @"\beuros?\b"),
        };

        // Entries matching this are dropped from the list but don't suppress the batch.
        static readonly Regex DroppedEntry =
            new Regex(@"wei(?:ß|ss)\s*nicht|undefined", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// Apply the frontend filter to a batch of entries.
        /// Returns the entries that would actually be DISPLAYED, or <c>null</c> when the
        /// batch resolves to "no concrete options" (a rule matched, or more than
        /// <see cref="MaxDisplayedOptions"/> non-scale options remain) — the free-answer case.
        /// </summary>
        public static List<string> Filter(IEnumerable<string> entries)
        {
            if (entries == null) return null;

            var materialised = new List<string>();
            foreach (var e in entries) materialised.Add(e);

            if (FindSuppressionRule(materialised) != null) return null;

            var filtered = new List<string>();
            foreach (var textEntry in materialised)
            {
                if (string.IsNullOrWhiteSpace(textEntry)) continue;
                if (DroppedEntry.IsMatch(textEntry)) continue;
                filtered.Add(textEntry);
            }

            if (filtered.Count > MaxDisplayedOptions && !IsNumericScale(filtered))
                return null;

            return filtered;
        }

        /// <summary>The first suppression rule any entry matches, or null if none do.</summary>
        public static SuppressionRule FindSuppressionRule(IEnumerable<string> entries)
        {
            if (entries == null) return null;
            foreach (var textEntry in entries)
            {
                if (string.IsNullOrWhiteSpace(textEntry)) continue;
                foreach (var rule in Rules)
                    if (rule.Test.IsMatch(textEntry))
                        return rule;
            }
            return null;
        }

        /// <summary>
        /// Explain, in one short phrase, why a batch resolved to "no concrete options".
        /// Shown in the interviewer's own chat so a deliberately suppressed batch can never
        /// be mistaken for one that failed to arrive.
        /// </summary>
        public static string DescribeSuppression(IEnumerable<string> entries)
        {
            var rule = FindSuppressionRule(entries);
            if (rule != null) return rule.Label;

            var kept = new List<string>();
            if (entries != null)
                foreach (var e in entries)
                    if (!string.IsNullOrWhiteSpace(e) && !DroppedEntry.IsMatch(e))
                        kept.Add(e);

            if (kept.Count > MaxDisplayedOptions && !IsNumericScale(kept))
                return $"zu viele Optionen ({kept.Count})";

            return "keine verwertbaren Einträge";
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
    }
}
