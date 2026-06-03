using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FlowDetective
{
    public static class HTMLProcessor
    {
        public record PxProperty(int LineNumber, string Property, string Value, bool AffectsLayout);

        public static List<PxProperty> ProcessFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}", filePath);

            if (!IsHtmlFile(filePath))
                return new List<PxProperty>();

            var text = File.ReadAllText(filePath);
            return ProcessText(text);
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
                || string.Equals(ext, ".htm", StringComparison.OrdinalIgnoreCase);
        }
    }
}