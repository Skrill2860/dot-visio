using System.Linq;

namespace Common;

public static class StringExtensions
{
    public static string CleanupString(this string s)
    {
        var ans = s;

        foreach (var c in SharedConstants.ControlChars)
        {
            ans = ans.Replace($"{c}", "");
        }

        return ans;
    }

    public static string CleanupPos(this string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return "";
        }

        s = s.CleanupString();
        s = s.Replace(@"\", ""); // GraphViz adds "\" when splitting long lines in output, making parser fail
        return s;
    }

    public static string Quote(this string s)
    {
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }

    public static string UnQuote(this string s)
    {
        if (s.StartsWith("\""))
        {
            s = s.Trim('"');
            s = s.Replace("\"\"", "\"");
        }

        return s;
    }

    public static string QuoteIf(this string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return "\"\"";
        }

        if (s.Any(c => SharedConstants.Quotable.Contains(c)))
        {
            return s.Quote();
        }

        return s;
    }
}