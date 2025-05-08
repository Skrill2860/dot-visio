using Domain;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace GUI.VisioConversion.DotToVisioConversionHelpers;

public static class SubstituteDotMarkersHelper
{
    public static string SubstituteDot(Graph graph, string text)
    {
        var ans = text;
        ans = ans.Replace(@"\G", graph.Name);
        ans = ans.Replace(@"\n", Conversions.ToString(Strings.ChrW(8232)));
        return ans;
    }
    public static string SubstituteDot(Graph graph, Node node, string text)
    {
        var ans = text;
        ans = ans.Replace(@"\N", node.Id);
        ans = ans.Replace(@"\G", graph.Name);
        ans = ans.Replace(@"\n", Conversions.ToString(Strings.ChrW(8232)));
        return ans;
    }

    public static string SubstituteDot(Graph graph, Edge edge, string text)
    {
        var ans = text;
        ans = ans.Replace(@"\E", edge.FromNode.Id + "->" + edge.ToNode.Id);
        ans = ans.Replace(@"\G", graph.Name);
        ans = ans.Replace(@"\n", Conversions.ToString(Strings.ChrW(8232)));
        return ans;
    }
}