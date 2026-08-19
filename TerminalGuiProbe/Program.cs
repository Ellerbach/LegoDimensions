using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

var asm = typeof(Application).Assembly;
var lines = new List<string>();
foreach (var t in asm.GetTypes().Where(t =>
    t.FullName != null &&
    (t.Name.Contains("Button") ||
     t.Name.Contains("Label") ||
     t.Name.Contains("TextView") ||
     t.Name.Contains("Window") ||
     t.Name.Contains("Wizard") ||
     t.Name.Contains("Radio") ||
     t.Name.Contains("ListView") ||
     t.Name.Contains("TextField") ||
     t.Name.Contains("Application") ||
     t.Name.Contains("Pos") ||
     t.Name.Contains("Dim") ||
     t.Name.Contains("ColorScheme") ||
     t.Name.Contains("Dialog"))).OrderBy(t => t.FullName))
{
    lines.Add(t.FullName ?? t.Name);
    foreach (var m in t.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly).OrderBy(x => x.Name).Take(60))
    {
        if (m.Name.Contains("Click") || m.Name.Contains("Accept") || m.Name.Contains("Select") || m.Name.Contains("Run") || m.Name.Contains("Init") || m.Name.Contains("Changed") || m.Name.Contains("Added") || m.Name.Contains("Text") || m.Name.Contains("Selected") || m.Name.Contains("Value"))
        {
            lines.Add("  " + m.MemberType + ": " + m.Name);
        }
    }
}
File.WriteAllLines("api.txt", lines);
Console.WriteLine($"Wrote {lines.Count} lines");
