using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using DotVisio.Properties;
using GUI.Actions;
using GUI.Common;
using GUI.Error_Handling;
using GUI.Properties;
using Office = Microsoft.Office.Core;

namespace DotVisio.Ribbons;

[ComVisible(true)]
public class DotVisioRibbon : Office.IRibbonExtensibility
{
    private Office.IRibbonUI _ribbon;

    #region IRibbonExtensibility Members

    public string GetCustomUI(string ribbonId)
    {
        return GetResourceText("DotVisio.Ribbons.DotVisioRibbon.xml");
    }

    #endregion

    public void OnImportButtonClick(Office.IRibbonControl control)
    {
        try
        {
            DotImporter.ImportDot(false);
        }
        catch (Exception ex)
        {
            WarningDialogHelper.ShowWarning(ex.Message);
        }
    }

    public void OnImportGraphVizButtonClick(Office.IRibbonControl control)
    {
        try
        {
            DotImporter.ImportDot(true);
        }
        catch (Exception ex)
        {
            WarningDialogHelper.ShowWarning(ex.Message);
        }
    }

    public void OnLayoutWithGraphVizButtonClick(Office.IRibbonControl control)
    {
        try
        {
            LayoutExisting.LayoutCurrentPage();
        }
        catch (Exception ex)
        {
            WarningDialogHelper.ShowWarning(ex.Message);
        }
    }

    public void OnRedrawBoundingBoxesButtonClick(Office.IRibbonControl control)
    {
        try
        {
            RedrawBoundingBoxes.RedrawOnCurrentPage();
        }
        catch (Exception ex)
        {
            WarningDialogHelper.ShowWarning(ex.Message);
        }
    }

    public void OnExportButtonClick(Office.IRibbonControl control)
    {
        try
        {
            DotExporter.ExportActivePageAsDot();
        }
        catch (Exception ex)
        {
            WarningDialogHelper.ShowWarning(ex.Message);
        }
    }

    // Graphviz layout settings
    public void OnAlgorithmChanged(Office.IRibbonControl control, string selectedId, int selectedIndex)
    {
        SharedGui.CurrentDotSettings["algorithm"] = selectedId.Replace("alg_", "");
    }

    public void OnAspectRatioChanged(Office.IRibbonControl control, string text)
    {
        SharedGui.CurrentDotSettings["aspectratio"] = text;
    }

    public void OnOverlapChanged(Office.IRibbonControl control, string selectedId, int selectedIndex)
    {
        SharedGui.CurrentDotSettings["overlap"] = selectedId.Replace("overlap_", "");
    }

    public void OnRankDirChanged(Office.IRibbonControl control, string selectedId, int selectedIndex)
    {
        SharedGui.CurrentDotSettings["rankdir"] = selectedId.Replace("rank_", "");
    }

    // Connector settings
    public void OnConnectorStyleChanged(Office.IRibbonControl control, string selectedId, int selectedIndex)
    {
        SharedGui.CurrentDotSettings["connectorstyle"] = selectedId.Replace("conn_", "");
    }

    public void OnConnectToChanged(Office.IRibbonControl control, string selectedId, int selectedIndex)
    {
        SharedGui.CurrentDotSettings["connectto"] = selectedId.Replace("conn_", "");
    }

    // Advanced settings
    public void OnStrictChanged(Office.IRibbonControl control, string selectedId, int selectedIndex)
    {
        SharedGui.CurrentDotSettings["strict"] = selectedId.Replace("strict_", "").ToLower();
    }

    public void OnBoundingBoxesChanged(Office.IRibbonControl control, bool pressed)
    {
        SharedGui.CurrentDotSettings["drawboundingboxes"] = pressed.ToString().ToLower();
    }

    public void OnCommandOptionsChanged(Office.IRibbonControl control, string text)
    {
        SharedGui.CurrentDotSettings["commandoptions"] = text;
    }

    public void OnSeedChanged(Office.IRibbonControl control, string text)
    {
        SharedGui.CurrentDotSettings["seed"] = text;
    }

    public void OnExportPositionsChanged(Office.IRibbonControl control, string selectedId, int selectedIndex)
    {
        SharedGui.CurrentDotSettings["exportpositions"] = selectedId.Replace("exportpositions_", "").ToLower();
    }

    // App Settings
    public void OnUpdateWhileDrawingChanged(Office.IRibbonControl control, bool pressed)
    {
        Settings.Default.UpdateWhileDrawing = pressed;
        Settings.Default.Save();
    }

    public void OnLimitFileSizeChanged(Office.IRibbonControl control, bool pressed)
    {
        if (pressed == false)
        {
            WarningDialogHelper.ShowWarning(Resources.FileLimitTurnedOffWarningLiabilityWaiver);
        }

        Settings.Default.LimitFileSize = pressed;
        Settings.Default.Save();
    }

    public void OnExtensionLanguageChanged(Office.IRibbonControl control, string selectedId, int selectedIndex)
    {
        Settings.Default.ExtensionLanguage = selectedId.Replace("lang_", "");

        Thread.CurrentThread.CurrentUICulture = Settings.Default.ExtensionLanguage switch
        {
            "en" or "ru" => CultureInfo.GetCultureInfo(Settings.Default.ExtensionLanguage),
            _ => Thread.CurrentThread.CurrentUICulture
        };

        Settings.Default.Save();
        WarningDialogHelper.ShowWarning(Resources.LanguageChangedNeedRebootWarning);
    }

    #region Ribbon Callbacks

    // Create callback methods here. For more information about adding callback methods, visit https://go.microsoft.com/fwlink/?LinkID=271226

    public void Ribbon_Load(Office.IRibbonUI ribbonUi)
    {
        _ribbon = ribbonUi;

        _ribbon.Invalidate();
    }

    #endregion

    #region Helpers

    // Used to update ribbon controls based on current settings
    public string GetSelectedItemId(Office.IRibbonControl control)
    {
        return control.Id switch
        {
            "ddAlgorithm" => "alg_" + SharedGui.CurrentDotSettings["algorithm"],
            "ddRankDir" => "rank_" + SharedGui.CurrentDotSettings["rankdir"],
            "ddOverlap" => "overlap_" + SharedGui.CurrentDotSettings["overlap"],
            "ddConnectorStyle" => "conn_" + SharedGui.CurrentDotSettings["connectorstyle"],
            "ddConnectTo" => "conn_" + SharedGui.CurrentDotSettings["connectto"],
            "ddStrict" => "strict_" + SharedGui.CurrentDotSettings["strict"].ToLower(),
            "ddExtensionLanguage" => "lang_" + Settings.Default.ExtensionLanguage,
            "ddExportPositions" => "exportpositions_" + SharedGui.CurrentDotSettings["exportpositions"],
            _ => ""
        };
    }

    public bool GetPressed(Office.IRibbonControl control)
    {
        return control.Id switch
        {
            "chkShapeIs" => SharedGui.CurrentDotSettings["shapeis"] == "true",
            "chkBoundingBoxes" => SharedGui.CurrentDotSettings["drawboundingboxes"] == "true",
            "chkUpdateWhileDrawing" => Settings.Default.UpdateWhileDrawing,
            "chkLimitFileSize" => Settings.Default.LimitFileSize,
            _ => false
        };
    }

    public string GetText(Office.IRibbonControl control)
    {
        return control.Id switch
        {
            "txtAspectRatio" => SharedGui.CurrentDotSettings["aspectratio"],
            "txtCommandOptions" => SharedGui.CurrentDotSettings["commandoptions"],
            "txtSeed" => SharedGui.CurrentDotSettings["seed"],
            _ => ""
        };
    }

    public string GetLabel(Office.IRibbonControl control)
    {
        Thread.CurrentThread.CurrentUICulture = Settings.Default.ExtensionLanguage switch
        {
            "en" or "ru" => CultureInfo.GetCultureInfo(Settings.Default.ExtensionLanguage),
            _ => Thread.CurrentThread.CurrentUICulture
        };

        return control.Id switch
        {
            "actionsGroup" => Resources.ActionsGroup,
            "btnImportDot" => Resources.BtnImportDot,
            "btnImportDotGraphViz" => Resources.BtnImportDotGraphViz,
            "btnLayoutWithGraphViz" => Resources.BtnLayoutWithGraphViz,
            "btnRedrawBoundingBoxes" => Resources.BtnRedrawBoundingBoxes,
            "btnExportDot" => Resources.BtnExportDot,

            "grpLayout" => Resources.LayoutGroup,
            "ddAlgorithm" => Resources.Algorithm,
            "ddRankDir" => Resources.RankDirection,
            "ddOverlap" => Resources.OverlapHandling,
            "txtAspectRatio" => Resources.AspectRatio,

            "grpConnectorSettings" => Resources.ConnectorGroup,
            "ddConnectorStyle" => Resources.ConnectorStyle,
            "ddConnectTo" => Resources.ConnectionPoints,

            "grpAdvancedSettings" => Resources.AdvancedGroup,
            "ddStrict" => Resources.StrictMode,
            "chkBoundingBoxes" => Resources.DrawBoundingBoxes,
            "txtCommandOptions" => Resources.CommandOptions,
            "txtSeed" => Resources.RandomSeed,
            "ddExportPositions" => Resources.ExportPositions,

            "grpAppSettings" => Resources.AppSettings,
            "chkUpdateWhileDrawing" => Resources.UpdateWhileDrawing,
            "chkLimitFileSize" => Resources.LimitFileSize,
            "ddExtensionLanguage" => Resources.ExtensionLanguage,

            _ => control.Id
        };
    }


    private static string GetResourceText(string resourceName)
    {
        var asm = Assembly.GetExecutingAssembly();
        var resourceNames = asm.GetManifestResourceNames();
        for (var i = 0; i < resourceNames.Length; ++i)
        {
            if (string.Compare(resourceName, resourceNames[i], StringComparison.OrdinalIgnoreCase) == 0)
            {
                using (var resourceReader = new StreamReader(asm.GetManifestResourceStream(resourceNames[i])))
                {
                    if (resourceReader != null)
                    {
                        return resourceReader.ReadToEnd();
                    }
                }
            }
        }

        return null;
    }

    #endregion
}