using Common;
using Domain;
using DotCore.DOT;
using GUI.Common;
using GUI.Properties;
using GUI.VisioConversion;
using Path = System.IO.Path;

namespace GUI.Actions;

public static class LayoutExisting
{
    public static void LayoutCurrentPage()
    {
        ProgressBarRunner.Run(LayoutCore, true);
    }

    private static void LayoutCore()
    {
        var page = SharedGui.MyVisioApp.ActivePage ??
                   throw new DotVisioException(Resources.ResourceManager.GetString("ErrorNoVisioPageIsActive") ?? "No Visio page is active");

        var graph = LoadVisio.LoadGraphFromVisioPage(page);

        if (graph.GlobalNodeNames.Count == 0)
        {
            throw new DotVisioException(Resources.ResourceManager.GetString("ErrorNoShapesOnPage") ??
                                        "There is nothing to layout, there are no connected shapes on the page");
        }

        SharedGui.CurrentDotSettings.ApplyToGraph(graph);

        var tempPath = Path.Combine(Path.GetTempPath(), SharedConstants.INPUTFILE);

        new DotWriter().WriteDot(graph, tempPath);

        var dotfile = GraphVizRunner.RunGraphViz(tempPath);

        var newGraph = DotReader.ReadGraphFromDot(dotfile);

        new GraphRenderer().DrawGraph(page, newGraph, false, false);
    }
}