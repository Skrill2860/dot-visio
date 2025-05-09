using Domain;
using GUI.Common;
using GUI.Properties;
using GUI.VisioConversion;

namespace GUI.Actions;

public static class RedrawBoundingBoxes
{
    public static void RedrawOnCurrentPage()
    {
        ProgressBarRunner.Run(RedrawBoundingBoxesCore, true);
    }

    private static void RedrawBoundingBoxesCore()
    {
        var page = SharedGui.MyVisioApp.ActivePage ??
                   throw new DotVisioException(Resources.ResourceManager.GetString("ErrorNoVisioPageIsActive") ??
                                               "No Visio page is active");

        var graph = LoadVisio.LoadGraphFromVisioPage(page);

        if (graph.GlobalNodeNames.Count == 0)
        {
            throw new DotVisioException(Resources.ResourceManager.GetString("ErrorNoShapesOnPage") ??
                                        "There is nothing to layout, there are no connected shapes on the page");
        }

        SharedGui.CurrentDotSettings.ApplyToGraph(graph);

        new GraphRenderer().RedrawBoundingBoxes(graph);
    }
}