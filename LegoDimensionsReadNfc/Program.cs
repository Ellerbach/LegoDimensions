// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using Iot.Device.Card.Ultralight;
using Iot.Device.Pn532;
using Iot.Device.Pn532.ListPassive;
using LegoDimensions.Tag;
using LegoDimensionsReadNfc;
using NStack;
using System.Diagnostics;
using System.IO.Ports;
using System.Xml.Linq;
using Terminal.Gui;

Application.Init();

// Setup wizar
var wizard = new Wizard($"Setup Wizard");

// Add 1st step
var firstStep = new Wizard.WizardStep("Setup NFC reader");
wizard.AddStep(firstStep);
firstStep.NextButtonText = "Continue!";
firstStep.HelpText = "This wizard will help you to setup the NFC reader.";

// Add 2nd step
var secondStep = new Wizard.WizardStep("Reader setup");
wizard.AddStep(secondStep);
secondStep.HelpText = "Please select the serial port of the PN532 NFC reader.";
var lbl = new Label("Ports:") { AutoSize = true };
secondStep.Add(lbl);
secondStep.NextButtonText = "Continue!";

var names = SerialPort.GetPortNames();
ustring[] comPortsU = new ustring[names.Length];
for (int i = 0; i < names.Length; i++)
{
    comPortsU[i] = names[i];
}

var comPortNames = new RadioGroup(comPortsU) { X = Pos.Right(lbl) + 1, Width = Dim.Fill() - 1 };
secondStep.Add(comPortNames);

// Ask what want to be done
var thirdStep = new Wizard.WizardStep("Action step");
wizard.AddStep(thirdStep);
thirdStep.HelpText = "What do you want to do?";
ustring[] actionChoices = new ustring[] { "Erase tag", "Read tag", "Read all card", "Write tag" };
var lblChoices = new Label("What do you want to execute?") { AutoSize = true };
thirdStep.Add(lbl);
var actionChoice = new RadioGroup(actionChoices) { X = Pos.Right(lblChoices) + 1, Width = Dim.Fill() - 1 };
thirdStep.Add(actionChoice);

wizard.Finished += (args) =>
{
    // MessageBox.Query("Wizard", $"Finished. The selected port is '{names[comPortNames.SelectedItem]}' and action '{actionChoices[actionChoice.SelectedItem]}'", "Ok");
    NfcPn532.OpenComPort(names[comPortNames.SelectedItem]);
    Application.RequestStop();
};

Application.Top.Add(wizard);
Application.Run();
Application.Shutdown();

// That's the write tag
switch (actionChoice.SelectedItem)
{
    case 0:
        Console.WriteLine("Place an empty tag on the reader to erase it.");
        NfcPn532.ErraseTag();
        break;
    case 1:
        Console.WriteLine("Place a tag on the reader to read it. Press any key to stop.");
        NfcPn532.ReadLegoTag(false);
        break;
    case 2:
        Console.WriteLine("Place a tag on the reader to read it. Press any key to stop.");
        NfcPn532.ReadLegoTag(true);
        break;
    case 3:
        Application.Init();
        bool okpressed = false;
        var ok = new Button(3, 14, "Ok");
        var cancel = new Button(10, 14, "Cancel");

        var dialog = new Dialog("Lego tag ID", 60, 18, ok, cancel);
        ok.Clicked += () => { Application.RequestStop(); okpressed = true; };
        cancel.Clicked += () => Application.RequestStop();

        var entry = new TextField()
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(),
            Height = 1
        };

        var label = new Label("All characters and vehicules:")
        {
            X = Pos.Left(entry),
            Y = Pos.Top(entry) + 1,
        };

        var list = new ListView()
        {
            X = Pos.Left(entry),
            Y = Pos.Top(label) + 1,
            Width = Dim.Fill(),
            Height = Dim.Height(dialog) - 7,
        };

        List<string> details = new List<string>();
        foreach (var car in Character.Characters)
        {
            details.Add($"{car.Id}: {car.Name}");
        }

        foreach (var vec in Vehicle.Vehicles)
        {
            details.Add($"{vec.Id}: {vec.Name}");
        }

        list.SetSource(details);
        dialog.Add(entry);
        dialog.Add(label);
        dialog.Add(list);

        Application.Top.Add(dialog);
        Application.Run();
        Application.Shutdown();
        if (okpressed)
        {
            Console.WriteLine("Place an empty tag on the reader.");
            ushort id = 0;
            if (entry.Text.IsEmpty)
            {
                if (list.SelectedItem > 0)
                {
                    id = ushort.Parse(details[list.SelectedItem].Split(":")[0]);
                }
            }
            else
            {
                id = ushort.Parse(entry.Text.ToString());
            }

            NfcPn532.WriteEmptyTag(id, id < 1000);
            Console.WriteLine("Writing tag done.");
        }

        break;
}

