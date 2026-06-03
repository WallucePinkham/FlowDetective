
using System;
using System.Collections.Generic;

namespace FlowDetective
{
    public static class PropertyLists
    {
        public static readonly HashSet<string> VisualOnlyProps = new(StringComparer.OrdinalIgnoreCase)
        {
            "box-shadow",
            "text-shadow",
            "filter",
            "outline",
            "outline-width",
            "outline-offset",
            "transform",
            "background-position",
            "background-size",
            "clip-path",
            "opacity",
            "z-index"
        };

        public static readonly HashSet<string> LayoutAffectingProps = new(StringComparer.OrdinalIgnoreCase)
        {
            "width","height",
            "min-width","max-width","min-height","max-height",
            "font-size","line-height",
            "border","border-width","border-left-width","border-right-width","border-top-width","border-bottom-width",
            "column-width",
            "grid-template-columns","grid-template-rows",
            "gap","column-gap","row-gap"
        };
    }
}