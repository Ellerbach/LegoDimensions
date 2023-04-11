// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using LegoDimensions;
using LegoDimensions.Portal;
using LegoDimensionsRunner;
using LegoDimensionsRunner.Actions;
using System.Text.Json;

Console.WriteLine("Hello, Lego Dimensions Runnuer");

if (args.Length == 0)
{
    Console.WriteLine($"You need to specify a file");
    return;
}

if (!File.Exists(args[0]))
{
    Console.WriteLine($"File {args[0]} does not exist");
    return;
}

CancellationTokenSource cs = new CancellationTokenSource();

Console.WriteLine("Press ctrl+c to exit.");
Console.CancelKeyPress += (sender, eArgs) =>
{
    eArgs.Cancel = true;
    cs.Cancel();
};

var content = File.ReadAllText(args[0]).Replace("\r", "").Replace("\n","");

ProcessRunner.CreateAllPortals();

// Just to make sure we'll keep things clean
var portals = ProcessRunner.GetLegoPortals();
LegoPortal[] legoPortals = new LegoPortal[portals.Length];
for (int i = 0; i < portals.Length; i++)
{
    legoPortals[i] = (LegoPortal)portals[i];
    legoPortals[i].WakeUp();
}

var runner = ProcessRunner.Deserialize(content);
ProcessRunner.Build(runner);
ProcessRunner.Run(cs.Token);

for (int i = 0; i < portals.Length; i++)
{
    legoPortals[i].Dispose();
}

Console.WriteLine("Thank you for playoing with us :-)");