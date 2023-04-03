// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using LegoDimensions;

var portal = LegoPortal.GetFirstPortal();
portal.LegoTagEvent += PortalLegoTagEvent;

Console.WriteLine("Press any key to exit");
while(!Console.KeyAvailable)
{
    Console.WriteLine("Pad left Blue, center white");
    portal.SetColorAllPads(Color.White, Color.Blue, Color.Red);
    Thread.Sleep(3000);
    Console.WriteLine("Pad left purple");
    portal.SetColor(Pad.LeftPad, Color.Purple);
    Thread.Sleep(2000);
    Console.WriteLine("Pad right azure flashing 20 times");
    portal.Flash(Pad.RightPad, 20, 20, 20, Color.Azure);
    Thread.Sleep(4000);
    Console.WriteLine("Pad center fading, stoppping and returning to original in darkred");
    portal.Fade(Pad.CenterPad, 50, ColorPulse.StopsReturningToOriginal, Color.DarkRed);
    Thread.Sleep(4000);
}

void PortalLegoTagEvent(object? sender, LegoTagEventArgs e)
{
    Console.WriteLine($"Tag is present: {e.Present} - UID: {BitConverter.ToString(e.CardUid)}");
    if (e.Present)
    {
        Console.WriteLine($"Tag is a {e.LegoTag?.GetType().Name} - Name= {e.LegoTag?.Name}");
    }
}
