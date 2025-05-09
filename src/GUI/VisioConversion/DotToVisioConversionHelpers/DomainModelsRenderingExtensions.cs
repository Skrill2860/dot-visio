using Domain;

namespace GUI.VisioConversion.DotToVisioConversionHelpers;

public static class DomainModelsRenderingExtensions
{
    public static void RenderBoundingBoxAttributes(this Graph node)
    {
        ShapeRenderHelper.ApplyClusterAttributes(node);
    }

    public static void RenderAttributes(this Node node)
    {
        node.CoalesceInheritedAttributes();
        ShapeRenderHelper.ApplyAttributesToShape(node);
    }

    public static void RenderAttributes(this Edge edge)
    {
        edge.CoalesceInheritedAttributes();
        ShapeRenderHelper.ApplyAttributesToShape(edge);
    }
}