// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using LegoDimensions;
using LegoDimensions.Portal;
using LegoDimensions.Tag;

List<LegoTagEventArgs> tags = new List<LegoTagEventArgs>();

var portal = LegoPortal.GetFirstPortal();
// If you don't want to get the tag details on the event, you can disable it
//portal.GetTagDetails = false;
portal.LegoTagEvent += PortalLegoTagEvent;

//TestColor0xC0();
//TestGetColor0xC1();
//TestSetColorAllPads0xC8();
//TestFlashPad0xCC3();
//TestFlashAllPads0xCC7();
//TestFade0xC2();
//TestFadeAll0xC6();
//TestFadeRandom0xC5();
//TestReadTag();
//TestPasswordAndRead();
//TestExistingCommùands();
//TestGetChallenge();
//TestWrite();

Console.WriteLine("Press any key to exit");
while (!Console.KeyAvailable)
{
    Thread.Sleep(1000);
    //TestDisplayPresentTag();
    //TestListTags();
}

portal.Dispose();
Console.WriteLine("End of test");

void PortalLegoTagEvent(object? sender, LegoTagEventArgs e)
{
    Console.WriteLine($"Tag is present: {e.Present} - UID: {BitConverter.ToString(e.CardUid)}");
    if (e.Present)
    {
        tags.Add(e);
        Console.WriteLine($"Tag is a {e.LegoTag?.GetType().Name} - Name= {e.LegoTag?.Name}");
    }
    else
    {
        var tag = tags.Where(m => m.CardUid.SequenceEqual(e.CardUid)).FirstOrDefault();
        if (tag != null)
        {
            tags.Remove(tag);
        }
    }
}


void TestColor0xC0()
{
    Console.WriteLine("Pad left blue");
    portal.SetColor(Pad.Left, Color.Blue);
    Thread.Sleep(1000);
    Console.WriteLine("Pad center white");
    portal.SetColor(Pad.Center, Color.White);
    Thread.Sleep(1000);
    Console.WriteLine("Pad right purple");
    portal.SetColor(Pad.Right, Color.Red);
    Thread.Sleep(1000);
    Console.WriteLine("All yellow");
    portal.SetColor(Pad.All, Color.Yellow);
}

void TestGetColor0xC1()
{
    Console.WriteLine("All yellow");
    portal.SetColor(Pad.All, Color.Yellow);
    Thread.Sleep(200);
    var col = portal.GetColor(Pad.Left);
    Console.WriteLine($"Pad left color: {col.R}-{col.G}-{col.B}");
}

void TestSetColorAllPads0xC8()
{
    Console.WriteLine("Pad left Blue, center white, right red");
    portal.SetColorAll(Color.White, Color.Blue, Color.Red);
}

void TestFlashPad0xCC3()
{
    Console.WriteLine("Pad right azure flashing 20 times (10 on and 10 off)");
    portal.Flash(Pad.Right, new FlashPad(20, 20, 20, Color.Azure));
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
    portal.Fade(Pad.Center, new FadePad(50, 5, Color.Red));
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
    portal.FadeRandom(Pad.Left, 10, 10);
    Thread.Sleep(1000);
    Console.WriteLine("Randome fading on center pad");
    portal.FadeRandom(Pad.Center, 1, 100);
    Thread.Sleep(1000);
    Console.WriteLine("Randome fading on right pad");
    portal.FadeRandom(Pad.Right, 5, 15);
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

void TestReadTag()
{
    Console.WriteLine("Place at least 1 tag on the portal, this will dump the full tag.");
    while (!portal.PresentTags.Any())
    {
        Thread.Sleep(1000);
    }

    var idx = portal.PresentTags.First().Index;
    for (byte i = 0; i < 0x2c; i += 4)
    {
        var tag = portal.ReadTag(idx, i);
        if (tag.Length == 0)
        {
            Console.WriteLine($"Error reading card page 0x{i:X2}");
        }
        else
        {
            Console.WriteLine($"Tag: {BitConverter.ToString(tag)}");
        }
    }
}

void TestPasswordAndRead()
{
    Console.WriteLine("Disabling the automatic password, place at least 1 tag on the portal, this will dump the full tag.");
    while (!portal.PresentTags.Any())
    {
        Thread.Sleep(1000);
    }

    var idx = portal.PresentTags.First().Index;
    portal.SetTagPassword(PortalPassword.Disable, idx);
    Thread.Sleep(100);
    TestReadTag();

    Console.WriteLine("Automatic password again");
    portal.SetTagPassword(PortalPassword.Automatic, idx);
    Thread.Sleep(100);
    TestReadTag();
}

void TestExistingCommùands()
{
    for (int i = 0; i < 255; i++)
    {
        Message message = new Message((MessageCommand)i);
        portal.SendMessage(message);
        Thread.Sleep(500);
    }
}

void TestGetChallenge()
{
    var challenge = portal.GetChallenge();
    Console.WriteLine($"Challenge: {BitConverter.ToString(challenge)}");
    Thread.Sleep(1000);
    challenge = portal.GetChallenge();
    Console.WriteLine($"Challenge: {BitConverter.ToString(challenge)}");
}

void TestWrite()
{
    Console.WriteLine("Use with care! This will write on card! Place an empty NTAG21x on the pad. It will create the character E.T (61)");
    while (!tags.Any())
    {
        Thread.Sleep(1000);
    }

    var tag = tags.First();
    var car = LegoTag.EncrypCharactertId(tag.CardUid, 61);
    bool ret = portal.WriteTag(tag.Index, 0x24, car.AsSpan().Slice(0, 4).ToArray());
    if (!ret)
    {
        Console.WriteLine("Write failed writing 0x24");
        return;
    }

    ret = portal.WriteTag(tag.Index, 0x25, car.AsSpan().Slice(4, 4).ToArray());
    if (!ret)
    {
        Console.WriteLine("Write failed writing 0x25");
        return;
    }
}
