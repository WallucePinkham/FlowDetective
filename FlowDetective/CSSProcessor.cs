using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FlowDetective
{
    public static class CSSProcessor
    {
        public static List<HTMLProcessor.PxProperty> ProcessFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}", filePath);

            if (!IsCssFile(filePath))
                return new List<HTMLProcessor.PxProperty>();

            var text = File.ReadAllText(filePath);
            return ProcessText(text);
        }

        // Accept CSS text directly
        public static List<HTMLProcessor.PxProperty> ProcessText(string cssText)
        {
            var all = CssAnalyzer.Analyze(cssText);
            // Return only layout-affecting entries and defensively filter known visual-only properties.
            return all
                .Where(p => p.AffectsLayout && !PropertyLists.VisualOnlyProps.Contains(p.Property))
                .ToList();
        }

        static bool IsCssFile(string filePath)
        {
            var ext = Path.GetExtension(filePath);
            return string.Equals(ext, ".css", StringComparison.OrdinalIgnoreCase);
        }
    }
}