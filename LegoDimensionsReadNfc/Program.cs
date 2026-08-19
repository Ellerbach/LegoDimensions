// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using System.IO.Ports;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

var app = Application.Create();
app.Init();

var portNames = SerialPort.GetPortNames();
var selectedPort = portNames.FirstOrDefault();

var root = new Window("Lego NFC setup")
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var portLabel = new Label("Serial port:")
{
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
portList.SetSource(portNames);

var actionLabel = new Label("Action:")
{
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
actionList.SetSource(new[] { "Erase tag", "Read tag", "Read all card", "Write tag", "Quit" });

var openButton = new Button("Open")
{
    X = Pos.AnchorEnd(11),
    Y = Pos.AnchorEnd(1)
};
openButton.Clicked += () =>
{
    selectedPort = portNames[portList.SelectedItem >= 0 ? portList.SelectedItem : 0];
    switch (actionList.SelectedItem)
    {
        case 0:
            NfcPn532.OpenComPort(selectedPort);
            NfcPn532.ErraseTag();
            break;
        case 1:
            NfcPn532.OpenComPort(selectedPort);
            NfcPn532.ReadLegoTag(false);
            break;
        case 2:
            NfcPn532.OpenComPort(selectedPort);
            NfcPn532.ReadLegoTag(true);
            break;
        case 3:
            NfcPn532.OpenComPort(selectedPort);
            NfcPn532.WriteEmptyTag(0, true);
            break;
        default:
            app.RequestStop();
            return;
    }
};

root.Add(portLabel, portList, actionLabel, actionList, openButton);
app.Run(root);
app.Shutdown();
