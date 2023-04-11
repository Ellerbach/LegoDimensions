using LegoDimensions.Portal;
using LegoDimensions.Tag;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegoDimensionsRunnerTests
{
    [Collection("ProcessRunner")]
    public class EventTests
    {
        private const int MaxWait = 5000;
        private static TestPortal _portal;

        static EventTests()
        {
            if (ProcessRunner.GetLegoPortals() == null)
            {
                _portal = new TestPortal();
                ProcessRunner.SetSinglePortal(_portal);
            }
            else
            {
                _portal = ProcessRunner.GetLegoPortals()[0] as TestPortal;
            }
        }

        [Fact]
        public void IdEventTest()
        {
            // Arrange
            while (!_portal.ResetEvent.IsCancellationRequested)
            {
                _portal.ResetEvent.Token.WaitHandle.WaitOne(5000, true);
            }

            Runner runner = new Runner();
            runner.Animations = new List<Animation>();
            Animation animation1 = new Animation();

            animation1.Name = "Test1";
            animation1.Actions = new List<dynamic>();
            var setColor1 = new SetColor() { Pad = Pad.Center, Color = $"#{Color.Yellow.GetHashCode():X2}" };
            var strSetColor1 = setColor1.ToString();
            animation1.Actions.Add(strSetColor1);
            runner.Animations.Add(animation1);

            Animation animation2 = new Animation();
            animation2.Name = "Test2";
            animation2.Actions = new List<dynamic>();
            var setColor2 = new SetColor() { Pad = Pad.Right, Color = $"#{Color.Red.GetHashCode():X2}" };
            var strSetColor2 = setColor2.ToString();
            animation2.Actions.Add(strSetColor2);
            runner.Animations.Add(animation2);

            Event event1 = new Event() { Animation = "Test1", Id = 42 };
            runner.Events = new List<Event>();
            runner.Events.Add(event1);

            // Act

            ProcessRunner.Build(runner);
            _portal.ResetEvent = new CancellationTokenSource(MaxWait);
            _portal.GetLastFunction = null;
            _portal.RaiseTheEvent(new LegoTagEventArgs(Pad.Center, true, null, Character.Characters.FirstOrDefault(m => m.Id == 42), 0));
            var res = _portal.GetLastFunction;
            _portal.ResetEvent.Cancel();

            // Assert
            // This should be more complicated and check with the initial values
            Assert.Equal("SetColor(Center, #FFFFFF00)", res);

        }

        [Fact]
        public void PadEventTest()
        {
            // Arrange
            while (!_portal.ResetEvent.IsCancellationRequested)
            {
                _portal.ResetEvent.Token.WaitHandle.WaitOne(5000, true);
            }

            Runner runner = new Runner();
            runner.Animations = new List<Animation>();
            Animation animation1 = new Animation();

            animation1.Name = "Test1";
            animation1.Actions = new List<dynamic>();
            var setColor1 = new SetColor() { Pad = Pad.Center, Color = $"#{Color.Yellow.GetHashCode():X2}" };
            var strSetColor1 = setColor1.ToString();
            animation1.Actions.Add(strSetColor1);
            runner.Animations.Add(animation1);

            Animation animation2 = new Animation();
            animation2.Name = "Test2";
            animation2.Actions = new List<dynamic>();
            var setColor2 = new SetColor() { Pad = Pad.Right, Color = $"#{Color.Red.GetHashCode():X2}" };
            var strSetColor2 = setColor2.ToString();
            animation2.Actions.Add(strSetColor2);
            runner.Animations.Add(animation2);

            Event event1 = new Event() { Animation = "Test1", Pad = Pad.Right };
            runner.Events = new List<Event>();
            runner.Events.Add(event1);

            // Act
            ProcessRunner.Build(runner);
            _portal.ResetEvent = new CancellationTokenSource(MaxWait);
            _portal.GetLastFunction = null;
            _portal.RaiseTheEvent(new LegoTagEventArgs(Pad.Right, true, null, Character.Characters.FirstOrDefault(m => m.Id == 40), 0));
            var res = _portal.GetLastFunction;
            _portal.ResetEvent.Cancel();

            // Assert
            // This should be more complicated and check with the initial values
            Assert.Equal("SetColor(Center, #FFFFFF00)", res);
        }

        [Fact]
        public void PadAndIdEventTest()
        {
            // Arrange
            while (!_portal.ResetEvent.IsCancellationRequested)
            {
                _portal.ResetEvent.Token.WaitHandle.WaitOne(5000, true);
            }

            Runner runner = new Runner();
            runner.Animations = new List<Animation>();
            Animation animation1 = new Animation();

            animation1.Name = "Test1";
            animation1.Actions = new List<dynamic>();
            var setColor1 = new SetColor() { Pad = Pad.Center, Color = $"#{Color.Yellow.GetHashCode():X2}" };
            var strSetColor1 = setColor1.ToString();
            animation1.Actions.Add(strSetColor1);
            runner.Animations.Add(animation1);

            Animation animation2 = new Animation();
            animation2.Name = "Test2";
            animation2.Actions = new List<dynamic>();
            var setColor2 = new SetColor() { Pad = Pad.Right, Color = $"#{Color.Red.GetHashCode():X2}" };
            var strSetColor2 = setColor2.ToString();
            animation2.Actions.Add(strSetColor2);
            runner.Animations.Add(animation2);

            Event event1 = new Event() { Animation = "Test1", Pad = Pad.Right, Id = 40 };
            runner.Events = new List<Event>();
            runner.Events.Add(event1);

            // Act
            ProcessRunner.Build(runner);
            _portal.ResetEvent = new CancellationTokenSource(MaxWait);
            _portal.GetLastFunction = null;
            _portal.RaiseTheEvent(new LegoTagEventArgs(Pad.Right, true, null, Character.Characters.FirstOrDefault(m => m.Id == 40), 0));
            var res = _portal.GetLastFunction;
            _portal.ResetEvent.Cancel();

            // Assert
            // This should be more complicated and check with the initial values
            Assert.Equal("SetColor(Center, #FFFFFF00)", res);
        }

        [Fact]
        public void NoPadEventTest()
        {
            // Arrange
            while (!_portal.ResetEvent.IsCancellationRequested)
            {
                _portal.ResetEvent.Token.WaitHandle.WaitOne(5000, true);
            }

            Runner runner = new Runner();
            runner.Animations = new List<Animation>();
            Animation animation1 = new Animation();

            animation1.Name = "Test1";
            animation1.Actions = new List<dynamic>();
            var setColor1 = new SetColor() { Pad = Pad.Center, Color = $"#{Color.Yellow.GetHashCode():X2}" };
            var strSetColor1 = setColor1.ToString();
            animation1.Actions.Add(strSetColor1);
            runner.Animations.Add(animation1);

            Animation animation2 = new Animation();
            animation2.Name = "Test2";
            animation2.Actions = new List<dynamic>();
            var setColor2 = new SetColor() { Pad = Pad.Right, Color = $"#{Color.Red.GetHashCode():X2}" };
            var strSetColor2 = setColor2.ToString();
            animation2.Actions.Add(strSetColor2);
            runner.Animations.Add(animation2);

            Event event1 = new Event() { Animation = "Test1", Pad = Pad.Right };
            runner.Events = new List<Event>();
            runner.Events.Add(event1);

            // Act
            ProcessRunner.Build(runner);
            _portal.ResetEvent = new CancellationTokenSource(MaxWait);
            _portal.GetLastFunction = null;
            _portal.RaiseTheEvent(new LegoTagEventArgs(Pad.Center, true, null, Character.Characters.FirstOrDefault(m => m.Id == 40), 0));
            var res = _portal.GetLastFunction;
            _portal.ResetEvent.Cancel();

            // Assert
            // This should be more complicated and check with the initial values
            Assert.Null(res);
        }

        [Fact]
        public void NoIdEventTest()
        {
            // Arrange
            while (!_portal.ResetEvent.IsCancellationRequested)
            {
                _portal.ResetEvent.Token.WaitHandle.WaitOne(5000, true);
            }

            Runner runner = new Runner();
            runner.Animations = new List<Animation>();
            Animation animation1 = new Animation();

            animation1.Name = "Test1";
            animation1.Actions = new List<dynamic>();
            var setColor1 = new SetColor() { Pad = Pad.Center, Color = $"#{Color.Yellow.GetHashCode():X2}" };
            var strSetColor1 = setColor1.ToString();
            animation1.Actions.Add(strSetColor1);
            runner.Animations.Add(animation1);

            Animation animation2 = new Animation();
            animation2.Name = "Test2";
            animation2.Actions = new List<dynamic>();
            var setColor2 = new SetColor() { Pad = Pad.Right, Color = $"#{Color.Red.GetHashCode():X2}" };
            var strSetColor2 = setColor2.ToString();
            animation2.Actions.Add(strSetColor2);
            runner.Animations.Add(animation2);

            Event event1 = new Event() { Animation = "Test1", Id = 42 };
            runner.Events = new List<Event>();
            runner.Events.Add(event1);

            // Act
            ProcessRunner.Build(runner);
            _portal.ResetEvent = new CancellationTokenSource(MaxWait);
            _portal.GetLastFunction = null;
            _portal.RaiseTheEvent(new LegoTagEventArgs(Pad.Center, true, null, Character.Characters.FirstOrDefault(m => m.Id == 40), 0));
            var res = _portal.GetLastFunction;

            // Assert
            // This should be more complicated and check with the initial values
            Assert.Null(res);
        }

        [Fact]
        public void NoEventTest()
        {
            // Arrange
            while (!_portal.ResetEvent.IsCancellationRequested)
            {
                _portal.ResetEvent.Token.WaitHandle.WaitOne(5000, true);
            }

            Runner runner = new Runner();
            runner.Animations = new List<Animation>();
            Animation animation1 = new Animation();

            animation1.Name = "Test1";
            animation1.Actions = new List<dynamic>();
            var setColor1 = new SetColor() { Pad = Pad.Center, Color = $"#{Color.Yellow.GetHashCode():X2}" };
            var strSetColor1 = setColor1.ToString();
            animation1.Actions.Add(strSetColor1);
            runner.Animations.Add(animation1);

            Animation animation2 = new Animation();
            animation2.Name = "Test2";
            animation2.Actions = new List<dynamic>();
            var setColor2 = new SetColor() { Pad = Pad.Right, Color = $"#{Color.Red.GetHashCode():X2}" };
            var strSetColor2 = setColor2.ToString();
            animation2.Actions.Add(strSetColor2);
            runner.Animations.Add(animation2);

            // No event for this test.
            // Event event1 = new Event() { Animation = "Test1", Id = 42 };
            // runner.Events = new List<Event>();
            // runner.Events.Add(event1);            

            // Act
            ProcessRunner.Build(runner);
            _portal.ResetEvent = new CancellationTokenSource(MaxWait);
            _portal.GetLastFunction = null;
            _portal.RaiseTheEvent(new LegoTagEventArgs(Pad.Center, true, null, Character.Characters.FirstOrDefault(m => m.Id == 40), 0));
            var res = _portal.GetLastFunction;

            // Assert
            // This should be more complicated and check with the initial values
            Assert.Null(res);
        }

        [Fact]
        public void CardIdEventTest()
        {
            // Arrange
            while (!_portal.ResetEvent.IsCancellationRequested)
            {
                _portal.ResetEvent.Token.WaitHandle.WaitOne(5000, true);
            }

            Runner runner = new Runner();
            runner.Animations = new List<Animation>();
            Animation animation1 = new Animation();

            animation1.Name = "Test1";
            animation1.Actions = new List<dynamic>();
            var setColor1 = new SetColor() { Pad = Pad.Center, Color = $"#{Color.Yellow.GetHashCode():X2}" };
            var strSetColor1 = setColor1.ToString();
            animation1.Actions.Add(strSetColor1);
            runner.Animations.Add(animation1);

            Animation animation2 = new Animation();
            animation2.Name = "Test2";
            animation2.Actions = new List<dynamic>();
            var setColor2 = new SetColor() { Pad = Pad.Right, Color = $"#{Color.Red.GetHashCode():X2}" };
            var strSetColor2 = setColor2.ToString();
            animation2.Actions.Add(strSetColor2);
            runner.Animations.Add(animation2);

            Event event1 = new Event() { Animation = "Test1", CardId = BitConverter.ToString(new byte[7] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 }) };
            runner.Events = new List<Event>();
            runner.Events.Add(event1);

            // Act
            ProcessRunner.Build(runner);
            _portal.ResetEvent = new CancellationTokenSource(MaxWait);
            _portal.GetLastFunction = null;
            _portal.RaiseTheEvent(new LegoTagEventArgs(Pad.Center, true, new byte[7] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 }, null, 0));
            var res = _portal.GetLastFunction;
            _portal.ResetEvent.Cancel();

            // Assert
            // This should be more complicated and check with the initial values
            Assert.Equal("SetColor(Center, #FFFFFF00)", res);
        }

        [Fact]
        public void NoCardIdEventTest()
        {
            // Arrange
            while (!_portal.ResetEvent.IsCancellationRequested)
            {
                _portal.ResetEvent.Token.WaitHandle.WaitOne(5000, true);
            }

            Runner runner = new Runner();
            runner.Animations = new List<Animation>();
            Animation animation1 = new Animation();

            animation1.Name = "Test1";
            animation1.Actions = new List<dynamic>();
            var setColor1 = new SetColor() { Pad = Pad.Center, Color = $"#{Color.Yellow.GetHashCode():X2}" };
            var strSetColor1 = setColor1.ToString();
            animation1.Actions.Add(strSetColor1);
            runner.Animations.Add(animation1);

            Animation animation2 = new Animation();
            animation2.Name = "Test2";
            animation2.Actions = new List<dynamic>();
            var setColor2 = new SetColor() { Pad = Pad.Right, Color = $"#{Color.Red.GetHashCode():X2}" };
            var strSetColor2 = setColor2.ToString();
            animation2.Actions.Add(strSetColor2);
            runner.Animations.Add(animation2);

            Event event1 = new Event() { Animation = "Test1", CardId = BitConverter.ToString(new byte[7] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 }) };
            runner.Events = new List<Event>();
            runner.Events.Add(event1);

            // Act
            ProcessRunner.Build(runner);
            _portal.ResetEvent = new CancellationTokenSource(MaxWait);
            _portal.GetLastFunction = null;
            _portal.RaiseTheEvent(new LegoTagEventArgs(Pad.Center, true, new byte[7] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x00 }, null, 0));
            var res = _portal.GetLastFunction;
            _portal.ResetEvent.Cancel();

            // Assert
            // This should be more complicated and check with the initial values
            Assert.Null(res);
        }
    }
}
