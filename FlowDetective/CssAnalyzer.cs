using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FlowDetective
{
    public static class CssAnalyzer
    {
        // Simple declaration pattern used on extracted fragments
        static readonly Regex DeclSimple = new(@"(?<prop>[\w-]+)\s*:\s*(?<value>[^;{}]+?)\s*(?:;|$)",
                                               RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Media-feature pattern, e.g. (max-width:600px)
        static readonly Regex MediaFeaturePattern = new(@"\(\s*(?<prop>[\w-]+)\s*:\s*(?<value>[^)]+?)\s*\)",
                                                        RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // <style> block extractor
        static readonly Regex StyleBlockPattern = new(@"<style\b[^>]*>(?<css>.*?)<\/style>",
                                                       RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Inline style attribute extractor
        static readonly Regex InlineStylePattern = new(@"style\s*=\s*[""'](?<css>[^""']+)[""']",
                                                        RegexOptions.Compiled | RegexOptions.IgnoreCase);

        static readonly Regex NumericOnly = new(@"^-?\d+(\.\d+)?$", RegexOptions.Compiled);

        public static List<HTMLProcessor.PxProperty> Analyze(string cssText)
        {
            if (cssText is null) return new List<HTMLProcessor.PxProperty>();

            var fragments = new List<(string fragment, int index)>();
            // If input contains HTML-ish content, extract style blocks and inline styles
            if (cssText.IndexOf('<') >= 0)
            {
                foreach (Match m in StyleBlockPattern.Matches(cssText))
                {
                    var css = m.Groups["css"].Value;
                    fragments.Add((css, m.Index));
                }

                foreach (Match m in InlineStylePattern.Matches(cssText))
                {
                    var css = m.Groups["css"].Value;
                    fragments.Add((css, m.Index));
                }

                // Also include any remaining text as fallback
                fragments.Add((cssText, 0));
            }
            else
            {
                // For pure CSS, extract rule bodies and the whole text as fallback
                // Rule bodies inside braces
                var bracePattern = new Regex(@"\{(?<body>[^}]*)\}", RegexOptions.Compiled);
                foreach (Match m in bracePattern.Matches(cssText))
                {
                    fragments.Add((m.Groups["body"].Value, m.Index));
                }
                // Add the whole text too (handles top-level declarations / minified)
                fragments.Add((cssText, 0));
            }

            var results = new List<HTMLProcessor.PxProperty>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1) Extract media-feature declarations from entire text (so we catch (max-width:600px))
            foreach (Match m in MediaFeaturePattern.Matches(cssText))
            {
                var prop = m.Groups["prop"].Value.Trim().ToLowerInvariant();
                var rawValue = m.Groups["value"].Value.Trim();
                ProcessDeclaration(cssText, m.Index, prop, rawValue, results, seen);
            }

            // 2) Process each extracted fragment with declaration regex
            foreach (var (fragment, idx) in fragments)
            {
                if (string.IsNullOrWhiteSpace(fragment)) continue;

                foreach (Match m in DeclSimple.Matches(fragment))
                {
                    var prop = m.Groups["prop"].Value.Trim().ToLowerInvariant();
                    var rawValue = m.Groups["value"].Value.Trim();
                    // When fragment came from a style block/inline, the match index is relative — adjust using fragment index
                    int absoluteIndex = idx + m.Index;
                    ProcessDeclaration(cssText, absoluteIndex, prop, rawValue, results, seen);
                }
            }

            return results;
        }

        static void ProcessDeclaration(string cssText, int matchIndex, string prop, string rawValue, List<HTMLProcessor.PxProperty> results, HashSet<string> seen)
        {
            // Tokenize to distinguish true unitless tokens ("100", "0") from "100%" or "0.5rem"
            var tokens = Regex.Split(rawValue, @"[\s,]+")
                              .Select(t => t.Trim().TrimEnd(';', ')', '}'))
                              .Where(t => t.Length > 0)
                              .ToArray();

            bool hasPx = tokens.Any(t => t.EndsWith("px", StringComparison.OrdinalIgnoreCase));
            bool hasUnitlessNumber = tokens.Any(t => NumericOnly.IsMatch(t));

            // Only include declarations that contain px or a true unitless numeric token
            if (!hasPx && !hasUnitlessNumber)
                return;

            // Avoid duplicate (prop+value) pairs
            string key = prop + "|" + rawValue;
            if (!seen.Add(key))
                return;

            int line = cssText.Take(matchIndex).Count(c => c == '\n') + 1;
            bool affects = IsLayoutImpactingProperty(prop, rawValue, hasPx, hasUnitlessNumber);
            results.Add(new HTMLProcessor.PxProperty(line, prop, rawValue, affects));
        }

        static bool IsLayoutImpactingProperty(string prop, string value, bool hasPx, bool hasUnitlessNumber)
        {
            if (PropertyLists.LayoutAffectingProps.Contains(prop)) return true;
            if (prop.StartsWith("margin", StringComparison.OrdinalIgnoreCase)) return true;
            if (prop.StartsWith("padding", StringComparison.OrdinalIgnoreCase)) return true;
            if (prop is "top" or "left" or "right" or "bottom") return true;
            if (prop.StartsWith("border", StringComparison.OrdinalIgnoreCase) && (hasPx || hasUnitlessNumber)) return true;
            if (prop.Contains("grid") || prop.Contains("column")) return true;

            if ((prop == "width" || prop == "height" || prop.StartsWith("min-") || prop.StartsWith("max-")) && (hasPx || hasUnitlessNumber)) return true;

            return false;
        }
    }
}