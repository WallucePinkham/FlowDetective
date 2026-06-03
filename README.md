# FlowDetective

Lightweight tool to find fixed-unit (px) CSS values that may affect layout. Parses CSS, HTML, JSX and TSX sources and reports layout-affecting declarations while filtering known visual-only properties.

Features
- Parse .css files, <style> blocks and inline style attributes
- Support for .html, .htm, .jsx and .tsx files (including simple JSX style objects: style={{...}})
- Detects px values and unitless numeric values for common layout properties
- Resolves CSS custom properties (var(--name)) when declared in the same input
- Optionally follow local `<link rel="stylesheet" href="...">` references
- Defensive parsing: does not crash on malformed input (returns empty results)
- Centralized property lists for layout vs visual-only props

Quick start

Build

- Clone the repo and run `dotnet build` in the project directory.

Run against a file (CLI)

`dotnet run --project FlowDetective -- path/to/file.html`


CLI options
- -f, --follow-links
  Follow local `<link rel="stylesheet" href="...">` references and include referenced CSS files (skips http/https).
- -h, --help
  Show help.

Examples
- Scan a single HTML file (do not follow linked CSS):
- Scan and follow linked CSS files:

Output
- Prints found entries like:
  Line 12: width: 100px
- If no layout-affecting px properties are found, prints a short message.

What it parses
- .css files (full CSS)
- .html / .htm files: <style> blocks and inline style attributes
- .jsx / .tsx files: same as HTML plus simple JSX inline style objects (style={{ ... }})
- Extracts declarations from multiple style blocks, inline attributes, media queries and simple var(...) fallbacks.
- Strips closed block comments (/* ... */) before parsing. Parsing is defensive and will not crash on malformed input.

Behavior
- CssAnalyzer.Analyze returns declarations containing px or true unitless numbers and marks whether they affect layout.
- HTMLProcessor / CSSProcessor filter analyzer results to:
  - include only layout-affecting declarations
  - exclude visual-only properties (e.g., box-shadow, text-shadow)
- Unitless numbers (e.g., `width: 100`) are reported and, for width/height/min-/max- properties, flagged as layout-affecting.

Testing
- Unit tests use xUnit and live under TestParsing.
- From the repository root run all tests with:

`dotnet test`

Design notes
- CssAnalyzer.Analyze returns all declarations that contain px values or true unitless numeric tokens. It marks whether a property affects layout; processors (HTMLProcessor/CSSProcessor) filter visual-only properties using a centralized PropertyLists.
- When input looks like HTML/JSX/TSX, only explicit style sources are scanned: <style> blocks, inline style attributes and simple JSX inline style objects (style={{...}}). This avoids false positives from JS/template strings.
- The analyzer strips closed block comments (/* ... */) before parsing. Unclosed comments are handled defensively.

Contributing
- Open issues and PRs welcome. Add tests for new cases and keep changes small and focused.

License
- MIT License — see LICENSE file.