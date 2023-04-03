// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using LegoDimensions;

var portal = LegoPortal.GetFirstPortal();
portal.LegoTagEvent += PortalLegoTagEvent;

//TestColor0xC0();
//TestGetColor0xC1();
//TestSetColorAllPads0xC8();
//TestFlashPad0xCC3();
//TestFlashAllPads0xCC7();
//TestFade0xC2();
//TestFadeAll0xC6();
//TestFadeRandom0xC5();

Console.WriteLine("Press any key to exit");
while (!Console.KeyAvailable)
{   
    Thread.Sleep(1000);
    //TestDisplayPresentTag();
    //TestListTags();
}

void PortalLegoTagEvent(object? sender, LegoTagEventArgs e)
{
    Console.WriteLine($"Tag is present: {e.Present} - UID: {BitConverter.ToString(e.CardUid)}");
    if (e.Present)
    {
        Console.WriteLine($"Tag is a {e.LegoTag?.GetType().Name} - Name= {e.LegoTag?.Name}");
    }
}


void TestColor0xC0()
{
    Console.WriteLine("Pad left blue");
    portal.SetColor(Pad.LeftPad, Color.Blue);
    Thread.Sleep(1000);
    Console.WriteLine("Pad center white");
    portal.SetColor(Pad.CenterPad, Color.White);
    Thread.Sleep(1000);
    Console.WriteLine("Pad right purple");
    portal.SetColor(Pad.RightPad, Color.Red);
    Thread.Sleep(1000);
    Console.WriteLine("All yellow");
    portal.SetColor(Pad.AllPads, Color.Yellow);
}

void TestGetColor0xC1()
{
    Console.WriteLine("All yellow");
    portal.SetColor(Pad.AllPads, Color.Yellow);
    Thread.Sleep(200);
    var col = portal.GetColor(Pad.LeftPad);
    Console.WriteLine($"Pad left color: {col.R}-{col.G}-{col.B}");
}

void TestSetColorAllPads0xC8()
{
    Console.WriteLine("Pad left Blue, center white, right red");
    portal.SetColorAllPads(Color.White, Color.Blue, Color.Red);
}

void TestFlashPad0xCC3()
{
    Console.WriteLine("Pad right azure flashing 20 times (10 on and 10 off)");
    portal.Flash(Pad.RightPad, new FlashPad(20, 20, 20, Color.Azure));
    Thread.Sleep(4000);
}

void TestFlashAllPads0xCC7()
{
    Console.WriteLine("Pad center red asymmetric flashing 40 times (20 on and 20 off)");
    Console.WriteLine("Pad left green flashing forever times fast (10 on and 10 off)");
    Console.WriteLine("Pad right azure flashing 20 times (10 on and 10 off)");
    portal.FlashAll(new FlashPad(10, 30, 40, Color.Red), new FlashPad(1, 1, 255, Color.Green), new FlashPad(20, 20, 20, Color.Azure));
}

void TestFade0xC2()
{
    Console.WriteLine("Fade center pad relatively slowly from the displayed color to red, will finish on the new color as odd number of count.");
    portal.Fade(Pad.CenterPad, new FadePad(50, 5, Color.Red));
}

void TestFadeAll0xC6()
{
    Console.WriteLine("Fade center pad relatively slowly from the displayed color to red, will finish on the new color as odd number of count.");
    Console.WriteLine("Fade left pad relatively quit fast from the displayed color to green, will finish on the old color as even number of count.");
    Console.WriteLine("Fade right will not fade as disabled regardless of the values placed in the Fade Pad.");
    portal.FadeAll(new FadePad(50, 5, Color.Red), new FadePad(5, 50, Color.Green), new FadePad(10, 100, Color.Yellow, false));
}

void TestFadeRandom0xC5()
{
    Console.WriteLine("Randome fading on left pad");
    portal.FadeRandom(Pad.LeftPad, 10, 10);
    Thread.Sleep(1000);
    Console.WriteLine("Randome fading on center pad");
    portal.FadeRandom(Pad.CenterPad, 1, 100);
    Thread.Sleep(1000);
    Console.WriteLine("Randome fading on right pad");
    portal.FadeRandom(Pad.RightPad, 5, 15);
    Thread.Sleep(1000);
}

void TestListTags()
{
    var tagList = portal.ListTags();
    foreach (var tag in tagList)
    {
        Console.WriteLine($"Pad: {tag.Pad}, Index: {tag.Index}, Type: {tag.TagType}");
    }
}

void TestDisplayPresentTag()
{
    var tagList = portal.PresentTags;
    foreach (var tag in tagList)
    {
        Console.WriteLine($"Pad: {tag.Pad}, Index: {tag.Index}, Type: {tag.TagType}");
    }
}