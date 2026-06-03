using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FlowDetective
{
    public static class CssAnalyzer
    {
        // Simple declaration pattern used on extracted fragments
        // Accept `;`, `}` or end-of-string as declaration terminators so declarations
        // immediately before a closing brace are matched.
        static readonly Regex DeclSimple = new(@"(?<prop>[\w-]+)\s*:\s*(?<value>[^;{}]+?)\s*(?:;|}|$)",
                                               RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Inline style without semicolons: find prop:value pairs separated by whitespace or end
        static readonly Regex InlineNoSemicolonPattern = new(@"(?<prop>[\w-]+)\s*:\s*(?<value>[^;]+?)(?=(?:\s+[\w-]+\s*:)|$)",
                                                               RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // JS(X) inline style object: style={{ width: '100px', height: 50 }}
        static readonly Regex InlineJsxStylePattern = new(@"style\s*=\s*\{\s*\{(?<css>.*?)\}\s*\}",
                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // HTML inline style attribute extractor (style="...")
        static readonly Regex InlineStylePattern = new(@"style\s*=\s*[""'](?<css>[^""']+)[""']",
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

        static readonly Regex NumericOnly = new(@"^-?\d+(\.\d+)?$", RegexOptions.Compiled);

        public static List<HTMLProcessor.PxProperty> Analyze(string cssText)
        {
            if (cssText is null) return new List<HTMLProcessor.PxProperty>();

            // Strip comments first
            cssText = StripBlockComments(cssText);

            var fragments = new List<(string fragment, int index, bool isInline)>();
            bool looksLikeHtml = cssText.IndexOf('<') >= 0;

            if (looksLikeHtml)
            {
                // Extract <style> blocks
                foreach (Match m in StyleBlockPattern.Matches(cssText))
                    fragments.Add((m.Groups["css"].Value, m.Index, false));

                // HTML inline style attributes
                foreach (Match m in InlineStylePattern.Matches(cssText))
                    fragments.Add((m.Groups["css"].Value, m.Index, true));

                // JSX/TSX inline style objects (style={{ ... }})
                foreach (Match m in InlineJsxStylePattern.Matches(cssText))
                {
                    var jsxBody = m.Groups["css"].Value;
                    var converted = ConvertJsxStyleObjectToCss(jsxBody);
                    if (!string.IsNullOrWhiteSpace(converted))
                        fragments.Add((converted, m.Index, true));
                }
            }
            else
            {
                // For pure CSS, extract rule bodies and the whole text as fallback
                var bracePattern = new Regex(@"\{(?<body>[^}]*)\}", RegexOptions.Compiled);
                foreach (Match m in bracePattern.Matches(cssText))
                    fragments.Add((m.Groups["body"].Value, m.Index, false));

                // Add the whole text too (handles top-level declarations / minified)
                fragments.Add((cssText, 0, false));
            }

            // Collect all declarations first (including media-feature ones) from fragments only
            var decls = new List<(string prop, string rawValue, int index)>();

            foreach (var (fragment, fragIdx, isInline) in fragments)
            {
                if (string.IsNullOrWhiteSpace(fragment)) continue;

                // Extract media-feature declarations from fragment as well (catches @media inside style blocks)
                foreach (Match m in MediaFeaturePattern.Matches(fragment))
                {
                    var prop = m.Groups["prop"].Value.Trim().ToLowerInvariant();
                    var rawValue = m.Groups["value"].Value.Trim();
                    decls.Add((prop, rawValue, fragIdx + m.Index));
                }

                if (isInline)
                {
                    // Inline style: handle both semicolon-delimited and space-delimited declarations
                    if (fragment.Contains(';'))
                    {
                        foreach (var part in fragment.Split(';'))
                        {
                            var trimmed = part.Trim();
                            if (string.IsNullOrEmpty(trimmed)) continue;
                            var m = DeclSimple.Match(trimmed + ";"); // ensure pattern sees terminator
                            if (m.Success)
                            {
                                var prop = m.Groups["prop"].Value.Trim().ToLowerInvariant();
                                var rawValue = m.Groups["value"].Value.Trim();
                                int absoluteIndex = fragIdx + fragment.IndexOf(part, StringComparison.Ordinal);
                                decls.Add((prop, rawValue, absoluteIndex));
                            }
                            else
                            {
                                // fallback: attempt prop:value extraction
                                var m2 = Regex.Match(trimmed, @"(?<prop>[\w-]+)\s*:\s*(?<value>.+)", RegexOptions.IgnoreCase);
                                if (m2.Success)
                                {
                                    var prop = m2.Groups["prop"].Value.Trim().ToLowerInvariant();
                                    var rawValue = m2.Groups["value"].Value.Trim();
                                    int absoluteIndex = fragIdx + fragment.IndexOf(part, StringComparison.Ordinal);
                                    decls.Add((prop, rawValue, absoluteIndex));
                                }
                            }
                        }
                    }
                    else
                    {
                        // No semicolons — use inline pattern to find successive prop:value pairs
                        foreach (Match m in InlineNoSemicolonPattern.Matches(fragment))
                        {
                            var prop = m.Groups["prop"].Value.Trim().ToLowerInvariant();
                            var rawValue = m.Groups["value"].Value.Trim();
                            int absoluteIndex = fragIdx + m.Index;
                            decls.Add((prop, rawValue, absoluteIndex));
                        }
                    }
                }
                else
                {
                    foreach (Match m in DeclSimple.Matches(fragment))
                    {
                        var prop = m.Groups["prop"].Value.Trim().ToLowerInvariant();
                        var rawValue = m.Groups["value"].Value.Trim();
                        int absoluteIndex = fragIdx + m.Index;
                        decls.Add((prop, rawValue, absoluteIndex));
                    }
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

        static string ConvertJsxStyleObjectToCss(string jsxBody)
        {
            if (string.IsNullOrWhiteSpace(jsxBody)) return string.Empty;

            // Match key: value pairs inside the JS object. Values may be quoted strings or bare numbers.
            var pairPattern = new Regex(@"(?<key>[\w$]+)\s*:\s*(?<val>('([^'\\]|\\.)*'|""([^""\\]|\\.)*""|[^,}]+))", RegexOptions.Compiled | RegexOptions.Singleline);
            var parts = new List<string>();

            foreach (Match m in pairPattern.Matches(jsxBody))
            {
                var key = m.Groups["key"].Value.Trim();
                var val = m.Groups["val"].Value.Trim();

                // Normalize key from camelCase to kebab-case
                var kebab = Regex.Replace(key, @"([a-z0-9])([A-Z])", "$1-$2").ToLowerInvariant();

                // Trim quotes if present
                if ((val.StartsWith("'") && val.EndsWith("'")) || (val.StartsWith("\"") && val.EndsWith("\"")))
                {
                    val = val.Substring(1, val.Length - 2);
                }

                // Remove trailing commas/spaces
                val = val.Trim().TrimEnd(',');

                parts.Add($"{kebab}:{val}");
            }

            // Join using semicolons so downstream parser sees normal CSS declarations
            return string.Join(";", parts);
        }
    }
}