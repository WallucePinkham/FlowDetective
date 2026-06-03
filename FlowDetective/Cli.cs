using System;
using System.IO;
using System.Linq;

namespace FlowDetective
{
    public static class Cli
    {
        public static void Run(string[] args, TextWriter output)
        {
            if (output is null) throw new ArgumentNullException(nameof(output));
            if (args is null || args.Length == 0)
            {
                output.WriteLine("Usage: FlowDetective [options] <files>");
                output.WriteLine("Options:");
                output.WriteLine("  -f, --follow-links    Follow <link rel=\"stylesheet\" href=\"...\"> and include referenced CSS files");
                output.WriteLine("  -h, --help            Show this help");
                return;
            }

            bool followLinks = false;
            var files = args.Where(a => a is not null).ToList();

            // simple option parsing
            for (int i = files.Count - 1; i >= 0; i--)
            {
                var a = files[i];
                if (string.Equals(a, "-f", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "--follow-links", StringComparison.OrdinalIgnoreCase))
                {
                    followLinks = true;
                    files.RemoveAt(i);
                }
                else if (string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "/?"))
                {
                    output.WriteLine("Usage: FlowDetective [options] <files>");
                    output.WriteLine("Options:");
                    output.WriteLine("  -f, --follow-links    Follow <link rel=\"stylesheet\" href=\"...\"> and include referenced CSS files");
                    output.WriteLine("  -h, --help            Show this help");
                    return;
                }
            }

            if (files.Count == 0)
            {
                output.WriteLine("No files specified. Use --help for usage.");
                return;
            }

            foreach (var file in files)
            {
                if (!File.Exists(file))
                {
                    output.WriteLine($"File not found: {file}");
                    continue;
                }

                try
                {
                    var ext = Path.GetExtension(file);
                    if (IsHtmlExtension(ext))
                    {
                        var entries = HTMLProcessor.ProcessFile(file, followLinks);
                        output.WriteLine($"Scanning HTML/JSX/TSX: {file}");
                        WriteEntries(entries, output);
                    }
                    else if (string.Equals(ext, ".css", StringComparison.OrdinalIgnoreCase))
                    {
                        var entries = CSSProcessor.ProcessFile(file);
                        output.WriteLine($"Scanning CSS: {file}");
                        WriteEntries(entries, output);
                    }
                    else
                    {
                        // ignore unknown extension
                        output.WriteLine($"Skipping unsupported file: {file}");
                    }
                }
                catch (Exception ex)
                {
                    output.WriteLine($"Processing failed for '{file}': {ex.Message}");
                }
            }
        }

        static bool IsHtmlExtension(string ext) =>
            string.Equals(ext, ".html", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ext, ".htm", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ext, ".jsx", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ext, ".tsx", StringComparison.OrdinalIgnoreCase);

        static void WriteEntries(System.Collections.Generic.IEnumerable<HTMLProcessor.PxProperty> entries, TextWriter output)
        {
            if (!entries.Any())
            {
                output.WriteLine("  No fixed-unit (px) properties found.");
                return;
            }

            foreach (var e in entries)
            {
                output.WriteLine($"  Line {e.LineNumber}: {e.Property}: {e.Value}");
            }
        }
    }
}