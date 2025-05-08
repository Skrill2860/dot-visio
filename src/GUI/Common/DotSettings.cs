using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Common;
using Domain;
using GUI.Gui;
using GUI.VisioConversion;
using Microsoft.Office.Interop.Visio;
using Microsoft.VisualBasic.CompilerServices;
using Path = System.IO.Path;

namespace GUI.Common;

public class DotSettings
{
    public readonly int Id;
    public string Name = "";
    private Dictionary<string, string> _values = new();

    public DotSettings(int newid)
    {
        Id = newid;
        _values.Add("algorithm", "dot");
        _values.Add("connectorstyle", "rightangle");
        _values.Add("connectto", "glue");
        _values.Add("drawboundingboxes", "true");
        _values.Add("aspectratio", "auto");
        _values.Add("overlap", "prism");
        _values.Add("rankdir", "TB");
        _values.Add("strict", "none");
        _values.Add("seed", "0");
        _values.Add("exportpositions", "false");
        SharedGui.PageTable.Add(Id, this);
    }

    public string this[string key]
    {
        get => _values.TryGetValue(key, out var item) ? item : "";
        set => _values[key] = value;
    }

    public void Save(string filename)
    {
        var fullFileName = Path.Combine(PathUtils.LocalDataDirectory(), SharedConstants.SETTINGSDIRECTORY, filename + ".txt");
        var sw = new StreamWriter(fullFileName, false);
        foreach (var key in _values.Keys)
        {
            sw.WriteLine(key + "=" + _values[key]);
        }

        sw.Close();
    }

    public void Load(string filename)
    {
        var fullFileName = Path.Combine(PathUtils.LocalDataDirectory(), SharedConstants.SETTINGSDIRECTORY, filename + ".txt");
        var sr = new StreamReader(fullFileName);
        _values = new Dictionary<string, string>();
        
        var line = sr.ReadLine();
        while (!sr.EndOfStream)
        {
            var parse = line.Split(['='], 2, StringSplitOptions.RemoveEmptyEntries);
            if (parse.GetUpperBound(0) > 0)
            {
                _values[parse[0]] = parse[1];
            }

            line = sr.ReadLine();
        }

        sr.Close();
    }

    public void InitFromActiveDocument()
    {
        if (SharedGui.MyVisioApp.ActivePage is null)
        {
            return;
        }

        if (SharedGui.MyVisioApp.ActivePage.PageSheet is null)
        {
            return;
        }

        var pageSheet = SharedGui.MyVisioApp.ActivePage.PageSheet;
        var setts = pageSheet.GetCustomProperty("settings");
        var sett = setts.Split(';');
        foreach (var ss in sett)
        {
            var asgn = ss.Split('=');
            if (!string.IsNullOrEmpty(asgn[0]))
            {
                _values[asgn[0]] = asgn[1];
            }
        }
    }

    public void SaveToActiveDocument()
    {
        if (SharedGui.MyVisioApp.ActivePage is null)
        {
            return;
        }

        if (SharedGui.MyVisioApp.ActivePage.PageSheet is null)
        {
            return;
        }

        var ans = _values.Aggregate("", (current, kvp) => current + kvp.Key + "=" + kvp.Value + ";");

        SharedGui.MyVisioApp.ActivePage.PageSheet.AddCustomProperty("settings", ans);
    }

    public void FromGraph(Graph graph)
    {
        _values["strict"] = graph.Strict.ToString().ToLower();
        foreach (var kvp in graph.Attributes)
        {
            _values[kvp.Key] = kvp.Value;
        }
    }

    public void ApplyToGraph(Graph graph)
    {
        if (this["strict"] != "none")
        {
            graph.Strict = this["strict"].ToLower() == "true";
        }
    }
}