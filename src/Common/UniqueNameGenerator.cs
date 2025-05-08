using System.Collections.Generic;

namespace Common;

public static class UniqueNameGenerator
{
    private static readonly Dictionary<(string, string), int> UniqueCounts = new();

    public static string GenerateUniqueName(string prefix = "", string suffix = "")
    {
        (string, string) key = (prefix, suffix);

        if (UniqueCounts.ContainsKey(key))
        {
            UniqueCounts[key] += 1;
        }
        else
        {
            UniqueCounts[key] = 1;
        }

        return prefix + UniqueCounts[key] + suffix;
    }
}