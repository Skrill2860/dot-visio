using System;
using System.Diagnostics;
using System.IO;
using Common;
using Domain;
using GUI.Gui;

namespace GUI.Common;

public static class GraphVizRunner
{
    public static string RunGraphViz(string inputFile)
    {
        var pgmpath = GetGraphvizDir();
        var temppath = Path.GetTempPath();
        var ofile = temppath + SharedConstants.OUTPUTFILE;
        var efile = temppath + SharedConstants.ERRORFILE;
        var pgm = SharedGui.CurrentDotSettings["algorithm"] + ".exe";

        if (!File.Exists(pgmpath + pgm))
        {
            throw new DotVisioException("GraphViz executable " + pgm + ".exe not found at " + pgmpath +
                                        ". Please (re)install Graphviz from www.graphviz.org");
        }

        if (File.Exists(ofile))
        {
            File.Delete(ofile);
        }

        var cmd = (pgmpath + pgm).Quote();
        var args = GraphvizOptions() + " -q -o" + ofile.Quote() + " " + inputFile.Quote();

        ProgressHelper.StartProgress("Calculating layout...", -1);
        var startInfo = new ProcessStartInfo(cmd);
        startInfo.Arguments = args;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardError = true;
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        startInfo.CreateNoWindow = true;
        using var dotVisioProcess = Process.Start(startInfo);
        dotVisioProcess?.WaitForExit(60000);
        ProgressHelper.EndProgress();

        int procEc;
        if (dotVisioProcess?.HasExited ?? false)
        {
            procEc = dotVisioProcess.ExitCode;
        }
        else
        {
            throw new DotVisioException(
                $"{SharedGui.CurrentDotSettings["algorithm"]} didn't terminate. commandline: {cmd} {args}");
        }

        if (procEc != 0)
        {
            var errorReader = dotVisioProcess.StandardError;
            var errors = errorReader.ReadToEnd();
            errorReader.Close();
            var pos = errors.IndexOf("Error:", StringComparison.Ordinal);
            if (pos > 0)
            {
                errors = errors.Substring(pos);
            }

            throw new DotVisioException(SharedGui.CurrentDotSettings["algorithm"] + " failed, rc=" + procEc +
                                        "\r\n" + errors + "\r\n" + "Command Line: " + cmd +
                                        " " + args);
        }

        return temppath + SharedConstants.OUTPUTFILE;
    }

    private static string GetGraphvizDir()
    {
        var pfd = new string[3];
        var answer = "";

        try
        {
            pfd[1] = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            pfd[2] = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            for (var i = 1; i <= 2; i++)
            {
                var di = new DirectoryInfo(pfd[i]);
                var dirs = di.GetDirectories("Graphviz*");
                foreach (var dir in dirs)
                {
                    answer = pfd[i] + @"\" + dir.Name + @"\bin\";
                }
            }
        }
        catch (Exception e)
        {
            throw new DotVisioException("Error locating Graphviz in Program Files: " + e.Message);
        }

        if (string.IsNullOrEmpty(answer))
        {
            throw new DotVisioException("No directory called 'Graphviz*' found in Program Files. " +
                                        "Please download and install Graphviz from www.graphviz.org.");
        }

        return answer;
    }

    private static string GraphvizOptions()
    {
        var ans = " -Gcharset=utf-8";
        if (SharedGui.CurrentDotSettings["aspectratio"] != "0")
        {
            ans = ans + " -Gratio=" + SharedGui.CurrentDotSettings["aspectratio"];
        }

        if (!string.IsNullOrEmpty(SharedGui.CurrentDotSettings["overlap"]))
        {
            ans = ans + " -Goverlap=" + SharedGui.CurrentDotSettings["overlap"];
        }

        if (!string.IsNullOrEmpty(SharedGui.CurrentDotSettings["splines"]))
        {
            ans = ans + " -Gsplines=" + SharedGui.CurrentDotSettings["splines"];
        }

        if (!string.IsNullOrEmpty(SharedGui.CurrentDotSettings["rankdir"]) && SharedGui.CurrentDotSettings["algorithm"] == "dot")
        {
            ans = ans + " -Grankdir=" + SharedGui.CurrentDotSettings["rankdir"].ToUpper();
        }

        var seed = SharedGui.CurrentDotSettings["seed"];
        if (seed != "0")
        {
            if (!int.TryParse(seed, out _))
            {
                seed = "1";
            }

            ans = ans + " -Gstart=" + seed;
        }
        
        var start = SharedGui.CurrentDotSettings["start"];
        if (start != "")
        {
            ans = ans + " -Gstart=" + start;
        }

        if (!string.IsNullOrEmpty(SharedGui.CurrentDotSettings["commandoptions"]))
        {
            ans = ans + " " + SharedGui.CurrentDotSettings["commandoptions"];
        }

        return ans;
    }
}