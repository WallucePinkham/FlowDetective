using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FlowDetective
{
    public static class HTMLProcessor
    {
        public record PxProperty(int LineNumber, string Property, string Value, bool AffectsLayout);

        // followLinks: when true, resolve <link rel="stylesheet" href="..."> references
        // relative to the HTML file and include their CSS declarations.
        public static List<PxProperty> ProcessFile(string filePath, bool followLinks = false)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}", filePath);

            if (!IsHtmlFile(filePath))
                return new List<PxProperty>();

            var text = File.ReadAllText(filePath);

            var combined = new List<HTMLProcessor.PxProperty>();

            // Analyze HTML/JSX/TSX text (style blocks, inline styles, JSX inline style objects)
            // Be defensive: if the analyzer throws for malformed input, return an empty list rather than crash.
            try
            {
                combined.AddRange(CssAnalyzer.Analyze(text));
            }
            catch
            {
                // Analyzer failed on this input; per requirement, do not crash when .jsx/.tsx passed.
                return new List<PxProperty>();
            }

            if (followLinks)
            {
                try
                {
                    // Find <link rel="stylesheet" href="..."> occurrences and include referenced CSS files.
                    // Basic extraction to support relative and absolute file paths. Ignore remote URLs.
                    var linkPattern = new System.Text.RegularExpressions.Regex(
                        @"<link\b[^>]*rel\s*=\s*[""']?stylesheet[""']?[^>]*href\s*=\s*[""'](?<href>[^""']+)[""']",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

                    var dir = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? "";

                    foreach (System.Text.RegularExpressions.Match m in linkPattern.Matches(text))
                    {
                        var href = m.Groups["href"].Value.Trim();
                        if (string.IsNullOrEmpty(href)) continue;

                        // Skip remote URLs (http/https)
                        if (Uri.TryCreate(href, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps))
                            continue;

                        string candidate;
                        if (Path.IsPathRooted(href))
                            candidate = href;
                        else
                            candidate = Path.GetFullPath(Path.Combine(dir, href));

                        if (File.Exists(candidate))
                        {
                            try
                            {
                                var cssText = File.ReadAllText(candidate);
                                combined.AddRange(CssAnalyzer.Analyze(cssText));
                            }
                            catch
                            {
                                // ignore individual link read/parse failures
                            }
                        }
                    }
                }
                catch
                {
                    // Defensive: if link extraction/parsing fails, don't crash; continue with what we have.
                }
            }

            // Filter same as ProcessText: only layout-affecting and not visual-only
            return combined
                .Where(e => e.AffectsLayout && !PropertyLists.VisualOnlyProps.Contains(e.Property))
                .ToList();
        }

        public static List<PxProperty> ProcessText(string htmlText)
        {
            var all = CssAnalyzer.Analyze(htmlText);
            // Ensure processors filter visual-only props in addition to requiring layout-affecting.
            return all
                .Where(e => e.AffectsLayout && !PropertyLists.VisualOnlyProps.Contains(e.Property))
                .ToList();
        }

        static bool IsHtmlFile(string filePath)
        {
            var ext = Path.GetExtension(filePath);
            return string.Equals(ext, ".html", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".htm", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".jsx", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".tsx", StringComparison.OrdinalIgnoreCase);
        }
    }
}