using Common;
using Microsoft.Office.Interop.Visio;

namespace GUI.VisioConversion;

public static class ShapeExtensions
{
    public static string GetCustomProperty(this Shape shape, string name)
    {
        var value = "";
        try
        {
            if (shape.CellExistsU["Prop." + name + ".Value", 1] != 0)
            {
                value = shape.Cells["Prop." + name + ".Value"].ResultStr[VisUnitCodes.visUnitsString];
            }
        }
        catch
        {
            // ignored
        }

        return value;
    }

    public static void AddCustomProperty(this Shape shape, string name, string value)
    {
        int intRowIndex;
        if (shape.SectionExists[(short)VisSectionIndices.visSectionProp, 1] == 0)
        {
            shape.AddSection((short)VisSectionIndices.visSectionProp);
        }

        if (shape.CellExistsU["Prop." + name + ".label", 1] != 0)
        {
            intRowIndex = shape.CellsRowIndexU["Prop." + name + ".label"];
        }
        else
        {
            intRowIndex = shape.AddNamedRow((short)VisSectionIndices.visSectionProp, name, (short)VisRowIndices.visRowProp);
            shape.CellsSRC[(short)VisSectionIndices.visSectionProp, (short)((int)VisRowIndices.visRowProp + intRowIndex),
                (short)VisCellIndices.visCustPropsPrompt].FormulaForceU = name.Quote();
            shape.CellsSRC[(short)VisSectionIndices.visSectionProp, (short)((int)VisRowIndices.visRowProp + intRowIndex),
                (short)VisCellIndices.visCustPropsPrompt].RowNameU = name;
            shape.CellsSRC[(short)VisSectionIndices.visSectionProp, (short)((int)VisRowIndices.visRowProp + intRowIndex),
                (short)VisCellIndices.visCustPropsLabel].FormulaForceU = name.Quote();
            shape.CellsSRC[(short)VisSectionIndices.visSectionProp, (short)((int)VisRowIndices.visRowProp + intRowIndex),
                (short)VisCellIndices.visCustPropsType].FormulaForceU = ((int)VisCellVals.visPropTypeString).ToString();
        }

        shape.CellsSRC[(short)VisSectionIndices.visSectionProp, (short)((int)VisRowIndices.visRowProp + intRowIndex),
            (short)VisCellIndices.visCustPropsValue].FormulaForceU = value.Quote();
    }

    public static void DeleteCustomProperty(this Shape shape, string name)
    {
        if (shape.SectionExists[(short)VisSectionIndices.visSectionProp, 1] == 0) // 1 - existsLocally = true
        {
            return;
        }

        if (shape.CellExistsU["Prop." + name + ".label", 1] != 0)
        {
            var intRowIndex = shape.CellsRowIndexU["Prop." + name + ".label"];
            shape.DeleteRow((short)VisSectionIndices.visSectionProp, intRowIndex);
        }
    }

    public static void EnsureSection(this Shape shape, short section)
    {
        if (shape.SectionExists[section, 1] == 0) // 1 - existsLocally = true
        {
            shape.AddSection(section);
        }
    }

    public static bool Is2D(this Shape shape)
    {
        return shape.CellExistsU["BeginX", 1] == 0; // 1 - existsLocally = true
    }

    public static bool IsConnector(this Shape conn)
    {
        return conn.CellExistsU["BeginX", 1] != 0; // 1 - existsLocally = true
    }

    public static bool IsVisible(this Shape shape)
    {
        if (shape.LayerCount == 0)
        {
            return true;
        }

        for (short i = 1; i <= shape.LayerCount; i++)
        {
            if (shape.Layer[i].CellsC[(short)VisCellIndices.visLayerVisible].FormulaU != "0")
            {
                return true;
            }
        }

        return false;
    }
}