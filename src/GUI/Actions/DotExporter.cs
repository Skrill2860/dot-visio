using System.Windows.Forms;
using DotCore.DOT;
using GUI.Common;
using GUI.Error_Handling;
using GUI.Gui;
using GUI.Properties;
using GUI.VisioConversion;
using Microsoft.Office.Interop.Visio;

namespace GUI.Actions;

public static class DotExporter
{
    private static string _filename = "";
    private static Page _page = null!;

    public static void ExportActivePageAsDot()
    {
        if (!ProgressBarRunner.CanRun())
        {
            WarningDialogHelper.ShowWarning(Resources.ResourceManager.GetString("AnotherProcessAlreadyRunning") ??
                                            "Another process already running");

            return;
        }

        _page = SharedGui.MyVisioApp.ActivePage;
        if (_page is null)
        {
            _page = SharedGui.MyVisioApp.Documents.Add("").Pages[0];
            WarningDialogHelper.ShowWarning(Resources.ResourceManager.GetString("ErrorNoVisioPageIsActive") ?? "No Visio page is active");
        }

        using var saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = @"GraphViz DOT Format (*.gv;*.dot)|*.gv;*.dot";
        saveFileDialog.OverwritePrompt = true;
        saveFileDialog.AddExtension = true;
        saveFileDialog.DefaultExt = ".gv";
        saveFileDialog.CheckFileExists = false;
        saveFileDialog.ShowDialog();
        _filename = saveFileDialog.FileName;

        if (!string.IsNullOrEmpty(_filename))
        {
            ProgressBarRunner.Run(WriteFullDot, true);
        }
        else
        {
            WarningDialogHelper.ShowWarning(Resources.ResourceManager.GetString("ExportFileNotSelected") ?? "No file selected");
        }
    }

    private static void WriteFullDot()
    {
        ProgressHelper.StartProgress("Analysing...", 0);
        var graph = LoadVisio.LoadGraphFromVisioPage(_page);
        ProgressHelper.EndProgress();

        graph.RenameNodes();

        ProgressHelper.StartProgress("Storing layout...", graph.ItemCount());
        var dotWriter = new DotWriter();
        dotWriter.WriteProgressIncreaseEvent += ProgressHelper.IncreaseProgress;
        dotWriter.WriteDot(graph, _filename);
        ProgressHelper.EndProgress();
    }
}