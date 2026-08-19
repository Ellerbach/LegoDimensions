// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.IO.Ports;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

var app = Application.Create();
app.Init();

var portNames = SerialPort.GetPortNames();
var selectedPort = portNames.FirstOrDefault() ?? string.Empty;

var root = new Window
{
    Title = "Lego NFC setup",
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var portLabel = new Label
{
    Text = "Serial port:",
    X = 1,
    Y = 1,
    Width = 12,
    Height = 1
};

var portList = new ListView
{
    X = 1,
    Y = 2,
    Width = Dim.Fill() - 2,
    Height = 6
};
var portItems = new ObservableCollection<string>(portNames);
portList.SetSource(portItems);
if (portItems.Count > 0)
{
    portList.SelectedItem = 0;
}

var actionLabel = new Label
{
    Text = "Action:",
    X = 1,
    Y = 10,
    Width = 12,
    Height = 1
};

var actionList = new ListView
{
    X = 1,
    Y = 11,
    Width = Dim.Fill() - 2,
    Height = 6
};
var actionItems = new ObservableCollection<string>(new[] { "Erase tag", "Read tag", "Read all card", "Write tag", "Quit" });
actionList.SetSource(actionItems);
actionList.SelectedItem = 0;

var openButton = new Button
{
    Text = "Open",
    X = Pos.AnchorEnd(11),
    Y = Pos.AnchorEnd(1)
};
openButton.Accepting += (_, _) =>
{
    var portIndex = portList.SelectedItem >= 0 ? portList.SelectedItem : 0;
    var portName = portIndex < portItems.Count ? portItems[portIndex] : string.Empty;
    if (string.IsNullOrWhiteSpace(portName))
    {
        return;
    }

    NfcPn532.OpenComPort(portName);

    var actionIndex = actionList.SelectedItem >= 0 ? actionList.SelectedItem : 0;
    var actionName = actionIndex < actionItems.Count ? actionItems[actionIndex] : string.Empty;
    switch (actionName)
    {
        case "Erase tag":
            NfcPn532.ErraseTag();
            break;
        case "Read tag":
            NfcPn532.ReadLegoTag(false);
            break;
        case "Read all card":
            NfcPn532.ReadLegoTag(true);
            break;
        case "Write tag":
            NfcPn532.WriteEmptyTag(0, true);
            break;
        case "Quit":
            app.RequestStop();
            break;
    }
};

root.Add(portLabel, portList, actionLabel, actionList, openButton);
app.Run(root);
app.Dispose();
