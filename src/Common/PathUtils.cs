using System;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Common;

public static class PathUtils
{
    public static string ApplicationPath()
    {
        var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);

        if (path.Substring(0, 5) == "file:")
        {
            path = path.Substring(5);
        }

        while (path.StartsWith(@"\"))
        {
            path = path.Substring(1);
        }

        if (!path.EndsWith(@"\"))
        {
            path += @"\";
        }

        return path;
    }

    public static string LocalDataDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Assembly.GetExecutingAssembly().GetName().Name);
    }
}