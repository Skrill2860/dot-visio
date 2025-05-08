using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Common;
using Domain;
using DotCore.DOT;
using GUI.Common;
using GUI.Error_Handling;
using GUI.Gui;
using GUI.Properties;
using GUI.VisioConversion;
using Path = System.IO.Path;

namespace GUI.Actions;

public static class DotImporter
{
    public static void ImportDot(bool useGraphViz)
    {
        if (!ProgressBarRunner.CanRun())
        {
            WarningDialogHelper.ShowWarning(Resources.ResourceManager.GetString("AnotherProcessAlreadyRunning") ??
                                            "Another process already running");

            return;
        }

        ImportDotCore(useGraphViz);
    }

    private static void ImportDotCore(bool useGraphViz)
    {
        using var openFileDialog = new OpenFileDialog();
        openFileDialog.InitialDirectory = Settings.Default.DotDirectory;
        openFileDialog.CheckFileExists = true;
        openFileDialog.Filter = "Graphviz files (.gv;.dot)|*.gv;*.dot|All files|*.*";

        var result = openFileDialog.ShowDialog();

        if (result is DialogResult.Abort or DialogResult.Cancel or DialogResult.Ignore or DialogResult.No)
        {
            return;
        }

        var filename = openFileDialog.FileName;

        if (string.IsNullOrEmpty(filename))
        {
            return;
        }

        var fileInfo = new FileInfo(filename);

        if (Settings.Default.LimitFileSize)
        {
            const int maxFileSize = 300 * 1024; // 300 KB
            const int maxLineCount = 1000;

            if (fileInfo.Length > maxFileSize ||
                File.ReadLines(fileInfo.FullName).Count() > maxLineCount)
            {
                throw new DotVisioException(
                    "Dot file is too big to be imported. Limit can be turned off in settings. Add-in does not guarantee stability if files are larger than the file size limit.");
            }
        }

        Settings.Default.DotDirectory = fileInfo.DirectoryName;

        var fileNameWoExt = Path.GetFileNameWithoutExtension(fileInfo.Name);

        var newPageName = CreateNewPage(fileNameWoExt);

        string layedoutFileName;
        if (useGraphViz)
        {
            layedoutFileName = GraphVizRunner.RunGraphViz(filename);
        }
        else
        {
            layedoutFileName = filename;
        }

        var useAutoLayout = false;
        if (!useGraphViz)
        {
            var useAutoLayoutResult = MessageBox.Show(Resources.ResourceManager.GetString("DotImporter_ImportDot_Use_Visio_layouting"),
                Resources.ResourceManager.GetString("DotImporter_ImportDot_Layouting"), MessageBoxButtons.YesNo);

            if (useAutoLayoutResult is not DialogResult.Yes and not DialogResult.No)
            {
                return;
            }

            useAutoLayout = useAutoLayoutResult == DialogResult.Yes;
        }

        ProgressBarRunner.Run(() => ImportDotAndDraw(layedoutFileName, newPageName, useAutoLayout), true);
    }

    private static string CreateNewPage(string fileNameWoExt)
    {
        if (SharedGui.MyVisioApp.ActiveDocument == null)
        {
            SharedGui.MyVisioApp.Documents.Add("");
        }

        SharedGui.MyVisioApp.ActiveDocument!.Pages.GetNames(out var pageNames);
        List<string> pageNamesArray = [];
        foreach (var pageName in pageNames)
        {
            pageNamesArray.Add(pageName.ToString());
        }

        var newPageName = fileNameWoExt;
        if (pageNamesArray.Any(p => string.Equals(p, newPageName, StringComparison.CurrentCultureIgnoreCase)))
        {
            var dialogResult = MessageBox.Show(
                "Страница с названием как у открываемого файла уже существует, импортировать на новую страницу? (Да - создать на новой странице, Нет - очистить текущую страницу и импортировать)",
                "Страница уже существует",
                MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                while (pageNamesArray.Any(p => p == newPageName))
                {
                    newPageName = UniqueNameGenerator.GenerateUniqueName(fileNameWoExt);
                }
            }
            else
            {
                SharedGui.MyVisioApp.ActiveDocument.Pages[fileNameWoExt].Delete(0);
            }
        }

        return newPageName;
    }

    private static void ImportDotAndDraw(string filename, string visioNewPageName, bool useVisioAutoLayout)
    {
        ProgressHelper.StartProgress("Importing...", 0);
        if (!string.IsNullOrEmpty(filename))
        {
            ProgressHelper.StartProgress("Interpreting layout..", -1);

            var graph = DotReader.ReadGraphFromDot(filename);
            if (graph is null)
            {
                throw new DotVisioException("DotVisio couldn't process " + filename + ". Check file " + filename + " for syntax errors");
            }

            ProgressHelper.EndProgress();

            var newPage = SharedGui.MyVisioApp.ActiveDocument.Pages.Add();
            newPage.Name = visioNewPageName;

            new GraphRenderer().DrawGraph(newPage, graph, true, useVisioAutoLayout);
        }

        ProgressHelper.EndProgress();
    }
}