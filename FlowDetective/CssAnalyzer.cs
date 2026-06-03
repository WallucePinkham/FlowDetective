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

        // var(...) usage pattern
        static readonly Regex VarPattern = new(@"var\(\s*(?<name>--[\w-]+)\s*(?:,\s*(?<fallback>[^)]+?)\s*)?\)",
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

            // Strip comments first
            cssText = StripBlockComments(cssText);

            var fragments = new List<(string fragment, int index)>();
            if (cssText.IndexOf('<') >= 0)
            {
                foreach (Match m in StyleBlockPattern.Matches(cssText))
                    fragments.Add((m.Groups["css"].Value, m.Index));

                foreach (Match m in InlineStylePattern.Matches(cssText))
                    fragments.Add((m.Groups["css"].Value, m.Index));

                fragments.Add((cssText, 0));
            }
            else
            {
                var bracePattern = new Regex(@"\{(?<body>[^}]*)\}", RegexOptions.Compiled);
                foreach (Match m in bracePattern.Matches(cssText))
                    fragments.Add((m.Groups["body"].Value, m.Index));

                fragments.Add((cssText, 0));
            }

            // Collect all declarations first (including media-feature ones)
            var decls = new List<(string prop, string rawValue, int index)>();

            foreach (Match m in MediaFeaturePattern.Matches(cssText))
            {
                var prop = m.Groups["prop"].Value.Trim().ToLowerInvariant();
                var rawValue = m.Groups["value"].Value.Trim();
                decls.Add((prop, rawValue, m.Index));
            }

            foreach (var (fragment, idx) in fragments)
            {
                if (string.IsNullOrWhiteSpace(fragment)) continue;
                foreach (Match m in DeclSimple.Matches(fragment))
                {
                    var prop = m.Groups["prop"].Value.Trim().ToLowerInvariant();
                    var rawValue = m.Groups["value"].Value.Trim();
                    int absoluteIndex = idx + m.Index;
                    decls.Add((prop, rawValue, absoluteIndex));
                }
            }

            // Build custom property map (--name -> rawValue). Last declared wins.
            var customProps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in decls)
            {
                if (d.prop.StartsWith("--", StringComparison.OrdinalIgnoreCase))
                    customProps[d.prop] = d.rawValue;
            }

            var results = new List<HTMLProcessor.PxProperty>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Process declarations, resolving var(...) where possible
            foreach (var d in decls)
            {
                ProcessDeclaration(cssText, d.index, d.prop, d.rawValue, results, seen, customProps);
            }

            return results;
        }

        static string StripBlockComments(string cssText)
        {
            // Remove /* ... */ block comments (including multiline)
            return Regex.Replace(cssText, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        }

        static void ProcessDeclaration(string cssText, int matchIndex, string prop, string rawValue, List<HTMLProcessor.PxProperty> results, HashSet<string> seen, Dictionary<string, string> customProps)
        {
            // Resolve var(...) occurrences to effective tokens for detection
            string resolvedValue = ResolveVars(rawValue, customProps);

            // Tokenize to distinguish true unitless tokens ("100", "0") from "100%" or "0.5rem"
            var tokens = Regex.Split(resolvedValue, @"[\s,]+")
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

        static string ResolveVars(string rawValue, Dictionary<string, string> customProps)
        {
            if (!rawValue.Contains("var(", StringComparison.OrdinalIgnoreCase))
                return rawValue;

            return VarPattern.Replace(rawValue, m =>
            {
                var name = m.Groups["name"].Value.Trim();
                var fallback = m.Groups["fallback"].Success ? m.Groups["fallback"].Value.Trim() : null;

                if (customProps != null && customProps.TryGetValue(name, out var val))
                {
                    return val;
                }

                if (!string.IsNullOrEmpty(fallback))
                    return fallback;

                // Unknown var reference — return empty string so it doesn't falsely match px/number
                return string.Empty;
            });
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