using System.Collections.Generic;

namespace Common;

public static class SharedConstants
{
    public const string GRAMMARFILE = "DotVisio.egt";
    public const string INPUTFILE = "DotVisioIn.txt";
    public const string OUTPUTFILE = "DotVisioOut.txt";
    public const string ERRORFILE = "DotVisioErr.txt";
    public const string STENCILNAME = "DotVisio.vssx";
    public const string SETTINGSDIRECTORY = "DotSettings";

    // transaction constants
    public const bool COMMIT = true;
    public const bool ROLLBACK = false;

    public const string
        ROOTGRAPHNAME =
            "DotVisio_RootGraph"; // DotVisio_ prefix, in case user has a layer called "RootGraph"

    public static readonly HashSet<char> ControlChars = [];
    public static readonly HashSet<char> Quotable = [];

    public static readonly HashSet<string> GraphvizGraphOptions =
    [
        "arrowhead", "arrowsize", "arrowtail", "bb", "bgcolor", "center", "charset", "clusterrank", "color", "colorscheme", "comment",
        "compound", "concentrate", "constraint", "Damping", "decorate", "defaultdist", "dim", "dir", "diredgeconstraints", "distortion",
        "dpi", "edgehref", "edgetarget", "edgetooltip", "edgeURL", "epsilon", "esep", "fillcolor", "fixedsize", "fontcolor", "fontname",
        "fontnames", "fontpath", "fontsize", "group", "headclip", "headhref", "headlabel", "headport", "headtarget", "headtooltip",
        "headURL", "height", "href", "image", "imagescale", "label", "labelangle", "labeldistance", "labelfloat", "labelfontcolor",
        "labelfontname", "labelfontsize", "labelhref", "labeljust", "labelloc", "labeltarget", "labeltooltip", "labelURL", "landscape",
        "layer", "layers", "layersep", "len", "levelsgap", "lhead", "lp", "ltail", "margin", "maxiter", "mclimit", "mindist", "minlen",
        "mode", "model", "mosek", "nodesep", "nojustify", "normalize", "nslimit", "ordering", "orientation", "outputorder", "overlap",
        "pack", "packmode", "pad", "page", "pagedir", "pencolor", "peripheries", "pin", "pos", "quantum", "rank", "rankdir", "ranksep",
        "ratio", "rects", "regular", "remincross", "resolution", "root", "rotate", "samehead", "sametail", "samplepoints", "searchsize",
        "sep", "shape", "shapefile", "showboxes", "sides", "size", "skew", "splines", "start", "style", "stylesheet", "tailclip",
        "tailhref", "taillabel", "tailport", "tailtarget", "tailtooltip", "tailURL", "target", "tooltip", "truecolor", "URL", "vertices",
        "viewport", "voro_margin", "weight", "width", "z"
    ];

    static SharedConstants()
    {
        // Note characters which require to be quoted in strings
        const string nice = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_$";
        for (var ch = 1; ch <= 255; ch++)
        {
            if (!nice.Contains($"{(char)ch}"))
            {
                Quotable.Add((char)ch);
            }
        }

        for (var ch = 0; ch <= 31; ch++)
        {
            ControlChars.Add((char)ch);
        }

        for (var ch = 127; ch <= 159; ch++)
        {
            ControlChars.Add((char)ch);
        }
    }
}