using System.IO;
using Domain;

namespace DotCore.DOT;

public static class DotReader
{
    public static Graph ReadGraphFromDot(string dotfile)
    {
        var fi = new FileInfo(dotfile);

        if (!fi.Exists)
        {
            throw new DotVisioException($"File {dotfile} not found");
        }

        var filenameWoLineBreaks = RemoveSlashLineBreaks(dotfile);

        return new DotParser().LoadDot(filenameWoLineBreaks);
    }

    private static string RemoveSlashLineBreaks(string dotfile)
    {
        var sr = new StreamReader(dotfile);
        var cleanFile = Path.Combine(
            //Path.GetDirectoryName(dotfile) ??
            Path.GetTempPath(),
            Path.GetFileNameWithoutExtension(dotfile) + "_stripped.txt");
        var sw = new StreamWriter(cleanFile);
        string? line;
        do
        {
            var fullline = "";
            do
            {
                line = sr.ReadLine();
                if (line is null)
                {
                    break;
                }

                line = line.TrimEnd(' ');
                if (line.EndsWith(@"\"))
                {
                    fullline += line.Substring(0, line.Length - 1);
                }
                else
                {
                    fullline += line;
                }
            } while (line.EndsWith(@"\"));

            sw.WriteLine(fullline);
        } while (line is not null);

        sr.Close();
        sw.Close();
        return cleanFile;
    }
}