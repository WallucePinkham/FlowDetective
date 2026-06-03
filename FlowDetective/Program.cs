using System;
using System.IO;
using System.Linq;

namespace FlowDetective
{
    internal class Program
    {   
        static void Main(string[] args)
        {
            foreach (string file in args)
            {
                ProcessPath(file);
            }
        }

        static void ProcessPath(string file)
        {
            if (!File.Exists(file))
            {
                Console.WriteLine($"File not found: {file}");
                return;
            }

            string ext = Path.GetExtension(file);
            string text;
            try
            {
                text = File.ReadAllText(file);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read '{file}': {ex.Message}");
                return;
            }

            try
            {
                if (IsHtmlExtension(ext))
                {
                    var entries = HTMLProcessor.ProcessText(text);
                    Console.WriteLine($"Scanning HTML/JSX/TSX: {file}");
                    WriteEntries(entries);
                }
                else if (string.Equals(ext, ".css", StringComparison.OrdinalIgnoreCase))
                {
                    var entries = CSSProcessor.ProcessText(text);
                    Console.WriteLine($"Scanning CSS: {file}");
                    WriteEntries(entries);
                }
                else
                {
                    // Unknown extension — ignore
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Processing failed for '{file}': {ex.Message}");
            }
        }

        static bool IsHtmlExtension(string ext) =>
            string.Equals(ext, ".html", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ext, ".htm", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ext, ".jsx", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ext, ".tsx", StringComparison.OrdinalIgnoreCase);

        static void WriteEntries(System.Collections.Generic.IEnumerable<HTMLProcessor.PxProperty> entries)
        {
            if (!entries.Any())
            {
                Console.WriteLine("  No fixed-unit (px) properties found.");
                return;
            }

            foreach (var e in entries)
            {
                Console.WriteLine($"  Line {e.LineNumber}: {e.Property}: {e.Value}");
            }
        }
    }
}
