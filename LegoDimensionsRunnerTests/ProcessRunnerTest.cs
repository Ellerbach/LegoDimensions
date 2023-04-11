// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using LegoDimensions.Portal;
using LegoDimensionsRunner;
using Newtonsoft.Json;
using System.Diagnostics;
using Xunit.Sdk;

namespace LegoDimensionsRunnerTests
{
    [Collection("ProcessRunner")]
    public class ProcessRunnerTest
    {
        private const int MaxWait = 5000;
        private static TestPortal _portal;

        static ProcessRunnerTest()
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
        public void BasicTest()
        {
            // Arrange
            while (!_portal.ResetEvent.IsCancellationRequested)
            {
                _portal.ResetEvent.Token.WaitHandle.WaitOne(5000, true);
            }

            Runner runner = new Runner();
            runner.Animations = new List<Animation>();
            Animation animation = new Animation();
            animation.Name = "Test";
            animation.Actions = new List<dynamic>();
            var setColor = new SetColor() { Pad = Pad.Center, Color = $"#{Color.Yellow.GetHashCode():X2}" };
            var strSetColor = setColor.ToString();
            var setColorAll = new SetColorAll() { Center = Color.Yellow.ToString(), Left = $"#{Color.Red.GetHashCode():X2}", Right = "Tan", Duration = 200 };
            var strSetColorAll = setColorAll.ToString();
            animation.Actions.Add(strSetColor);
            animation.Actions.Add(strSetColorAll);
            runner.Animations.Add(animation);

            // Act
            ProcessRunner.Build(runner);

            // Assert
            Assert.Equal(2, runner.Animations[0].CompiledActions.Count);
            _portal.ResetEvent = new CancellationTokenSource(MaxWait);

            foreach (var anim in runner.Animations)
            {
                foreach (var action in anim.CompiledActions)
                {
                    while(!_portal.ResetEvent.IsCancellationRequested)
                    {
                        _portal.ResetEvent.Token.WaitHandle.WaitOne(5000, true);
                    }

                    
                    _portal.GetLastFunction = null;
                    action.Invoke();
                    // This should be more complicated and check with the initial values
                    var res = _portal.GetLastFunction;                   
                    Assert.NotNull(res);                    
                }
            }

            _portal.ResetEvent.Cancel();
        }

        [Fact]
        public void SetColorTest()
        {
            // Arrange
            while (!_portal.ResetEvent.IsCancellationRequested)
            {
                _portal.ResetEvent.Token.WaitHandle.WaitOne(5000, true);
            }

            Runner runner = new Runner();
            runner.Animations = new List<Animation>();
            Animation animation = new Animation();
            animation.Name = "Test";
            animation.Actions = new List<dynamic>();
            var setColor = new SetColor() { Pad = Pad.Center, Color = $"#{Color.Yellow.GetHashCode():X2}" };
            var strSetColor = setColor.ToString();
            animation.Actions.Add(strSetColor);
            runner.Animations.Add(animation);

            // Act
            ProcessRunner.Build(runner);
           
            _portal.ResetEvent = new CancellationTokenSource(MaxWait);
            _portal.GetLastFunction = null;
            runner.Animations[0].CompiledActions[0].Invoke();
            var res = _portal.GetLastFunction;
            _portal.ResetEvent.Cancel();

            // Assert
            Assert.Equal("SetColor(Center, #FFFFFF00)", res);
        }

        [Fact]
        public void SetColorWithPortalIdTest()
        {
            // Arrange
            while (!_portal.ResetEvent.IsCancellationRequested)
            {
                _portal.ResetEvent.Token.WaitHandle.WaitOne(5000, true);
            }

            Runner runner = new Runner();
            runner.Animations = new List<Animation>();
            Animation animation = new Animation();
            animation.Name = "Test";
            animation.PortalId = 0;
            animation.Actions = new List<dynamic>();
            var setColor = new SetColor() { Pad = Pad.Center, Color = $"#{Color.Yellow.GetHashCode():X2}" };
            var strSetColor = setColor.ToString();
            animation.Actions.Add(strSetColor);
            runner.Animations.Add(animation);

            // Act
            ProcessRunner.Build(runner);
            
            _portal.ResetEvent = new CancellationTokenSource(MaxWait);
            _portal.GetLastFunction = null;
            runner.Animations[0].CompiledActions[0].Invoke();
            var res = _portal.GetLastFunction;
            _portal.ResetEvent.Cancel();

            // Assert
            Assert.Equal("SetColor(Center, #FFFFFF00)", res);
        }

        [Theory]
        [InlineData("{\"Configuration\":null,\"Animations\":[{\"Name\":\"Test\",\"Duration\":null,\"Actions\":[\"Name=SetColor,Pad=Center,Color=#FFFFFF00,Duration=\",\"Name=SetColorAll,Center=#FFFFFF00,Left=#FFFF0000,Right=#FFD2B48C,Duration=1\"],\"CompiledActions\":null}],\"Event\":null}")]
        [InlineData("{\"Animations\":[{\"Name\":\"Test\",\"Actions\":[\"Name=SetColor,Pad=1,Color=#FFFFFF00\",\"Name=SetColorAll,Center=yellow,Left=#FFFF0000,Right=#FFD2B48C,Duration=200\"]}]}")]
        [InlineData("{\"anImaTions\":[{\"Name\":\"Test\",\"Actions\":[\"Name=SetColor,   PAD =     1, color = #FFFFFF00    \",\"Name=SetColorAll,Center=yellow,Left=#FFFF0000,Right=#FFD2B48C,Duration=200\"]}]}")]
        [InlineData("{\"Configuration\":null,\"Animations\":[{\"Name\":\"Test\",\"Duration\":null,\"Actions\":[{\"Duration\":null,\"Name\":\"SetColor\",\"Pad\":1,\"Color\":\"#FFFFFF00\"},{\"Name\":\"SetColorAll\",\"Duration\":1,\"Center\":\"#FFFFFF00\",\"Left\":\"#FFFF0000\",\"Right\":\"#FFD2B48C\"}],\"CompiledActions\":null}],\"Event\":null}")]
        [InlineData("{\"Animations\":[{\"Name\":\"Test\",\"Actions\":[{\"Name\":\"setcolor\",\"PAD\":1,\"coLor\":\"yellow\"},{\"Name\":\"SetColorAll\",\"Duration\":200,\"Center\":\"#FFFFFF00\",\"Left\":\"#FFFF0000\",\"Right\":\"#FFD2B48C\"}],\"CompiledActions\":null}],\"Event\":null}")]
        public void SetColorFromJson(string json)
        {
            // Arrange
            while (!_portal.ResetEvent.IsCancellationRequested)
            {
                _portal.ResetEvent.Token.WaitHandle.WaitOne(5000, true);
            }

            Runner runner = ProcessRunner.Deserialize(json);

            // Act
            ProcessRunner.Build(runner);            

            _portal.ResetEvent = new CancellationTokenSource(MaxWait);
            _portal.GetLastFunction = null;
            runner.Animations[0].CompiledActions[0].Invoke();
            var res = _portal.GetLastFunction;
            _portal.ResetEvent.Cancel();

            // Assert
            Assert.Equal("SetColor(Center, #FFFFFF00)", res);
        }

        [Fact]
        public void SetColorAllTest()
        {
            // Arrange
            while (!_portal.ResetEvent.IsCancellationRequested)
            {
                _portal.ResetEvent.Token.WaitHandle.WaitOne(5000, true);
            }

            Runner runner = new Runner();
            runner.Animations = new List<Animation>();
            Animation animation = new Animation();
            animation.Name = "Test";
            animation.Actions = new List<dynamic>();
            var setColor = new SetColorAll() { Center = Color.Yellow.ToString(), Left = $"#{Color.Red.GetHashCode():X2}", Right = "Tan", Duration = 2000 };
            var strSetColor = setColor.ToString();
            animation.Actions.Add(strSetColor);
            runner.Animations.Add(animation);

            // Act
            ProcessRunner.Build(runner);
            
            _portal.ResetEvent = new CancellationTokenSource(MaxWait);
            _portal.GetLastFunction = null;

            // Check the time of execution as well, must be more than the duration
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            runner.Animations[0].CompiledActions[0].Invoke();
            stopwatch.Stop();
            var res = _portal.GetLastFunction;
            _portal.ResetEvent.Cancel();

            // Assert
            Assert.Equal("SetColorAll(#FFFFFF00, #FFFF0000, #FFD2B48C)", res);
            Assert.True(stopwatch.ElapsedMilliseconds >= 2000);
        }

        [Fact]
        public void FadeTest()
        {
            // Arrange
            while (!_portal.ResetEvent.IsCancellationRequested)
            {
                _portal.ResetEvent.Token.WaitHandle.WaitOne(5000, true);
            }

            Runner runner = new Runner();
            runner.Animations = new List<Animation>();
            Animation animation = new Animation();
            animation.Name = "Test";
            animation.Actions = new List<dynamic>();
            var fade = new Fade() { Color = "yeLLOw", Pad = Pad.Right, TickCount = 42, TickTime = 20, Duration = 100 };
            var strFade = fade.ToString();
            animation.Actions.Add(strFade);
            runner.Animations.Add(animation);

            // Act
            ProcessRunner.Build(runner);
            
            _portal.ResetEvent = new CancellationTokenSource(MaxWait);
            _portal.GetLastFunction = null;
            runner.Animations[0].CompiledActions[0].Invoke();
            var res = _portal.GetLastFunction;
            _portal.ResetEvent.Cancel();

            // Assert           
            Assert.Equal("Fade(Right, {\"Enabled\":true,\"TickTime\":20,\"TickCount\":42,\"Color\":{\"A\":255,\"B\":0,\"R\":255,\"G\":255}})", res);
        }

        [Fact]
        public void FadeAllTest()
        {
            // Arrange
            while (!_portal.ResetEvent.IsCancellationRequested)
            {
                _portal.ResetEvent.Token.WaitHandle.WaitOne(5000, true);
            }

            Runner runner = new Runner();
            runner.Animations = new List<Animation>();
            Animation animation = new Animation();
            animation.Name = "Test";
            animation.Actions = new List<dynamic>();
            var fade = new FadeAll() { CenterColor = "yeLLOw", CenterTickCount = 42, CenterTickTime = 20, LeftColor = "RED", LeftTickCount = 24, LeftTickTime = 2, LeftEnabled = false, Duration = 100 };
            var strFade = fade.ToString();
            animation.Actions.Add(strFade);
            runner.Animations.Add(animation);

            // Act
            ProcessRunner.Build(runner);
            
            _portal.ResetEvent = new CancellationTokenSource(MaxWait);
            _portal.GetLastFunction = null;
            runner.Animations[0].CompiledActions[0].Invoke();
            var res = _portal.GetLastFunction;
            _portal.ResetEvent.Cancel();

            // Assert
            Assert.Equal("FadeAll(This is a lot, so should work)", res);
        }

        [Fact]
        public void FadeRandomTest()
        {
            // Arrange
            while (!_portal.ResetEvent.IsCancellationRequested)
            {
                _portal.ResetEvent.Token.WaitHandle.WaitOne(5000, true);
            }

            Runner runner = new Runner();
            runner.Animations = new List<Animation>();
            Animation animation = new Animation();
            animation.Name = "Test";
            animation.Actions = new List<dynamic>();
            var fade = new FadeRandom() { Pad = Pad.Right, TickCount = 42, TickTime = 20, Duration = 100 };
            var strFade = fade.ToString();
            animation.Actions.Add(strFade);
            runner.Animations.Add(animation);

            // Act
            ProcessRunner.Build(runner);
                        
            _portal.ResetEvent = new CancellationTokenSource(MaxWait);
            _portal.GetLastFunction = null;
            runner.Animations[0].CompiledActions[0].Invoke();
            var res = _portal.GetLastFunction;
            _portal.ResetEvent.Cancel();

            // Assert
            Assert.Equal("FadeRandom(Right, 20, 42)", res);
        }

        [Fact]
        public void FlashTest()
        {
            // Arrange
            while (!_portal.ResetEvent.IsCancellationRequested)
            {
                _portal.ResetEvent.Token.WaitHandle.WaitOne(5000, true);
            }

            Runner runner = new Runner();
            runner.Animations = new List<Animation>();
            Animation animation = new Animation();
            animation.Name = "Test";
            animation.Actions = new List<dynamic>();
            var flash = new Flash() { Color = "yeLLOw", Pad = Pad.Right, TickCount = 42, TickOn = 20, TickOff = 12, Duration = 100 };
            var strFlash = flash.ToString();
            animation.Actions.Add(strFlash);
            runner.Animations.Add(animation);

            // Act
            ProcessRunner.Build(runner);

            _portal.ResetEvent = new CancellationTokenSource(MaxWait);
            _portal.GetLastFunction = null;
            runner.Animations[0].CompiledActions[0].Invoke();
            var res = _portal.GetLastFunction;
            _portal.ResetEvent.Cancel();

            // Assert
            Assert.Equal("Flash(Right, {\"FlashForever\":false,\"TickOn\":20,\"TickOff\":12,\"TickCount\":42,\"Color\":{\"A\":255,\"B\":0,\"R\":255,\"G\":255},\"Enabled\":true})", res);
        }

        [Fact]
        public void FlashAllTest()
        {
            // Arrange
            while (!_portal.ResetEvent.IsCancellationRequested)
            {
                _portal.ResetEvent.Token.WaitHandle.WaitOne(5000, true);
            }

            Runner runner = new Runner();
            runner.Animations = new List<Animation>();
            Animation animation = new Animation();
            animation.Name = "Test";
            animation.Actions = new List<dynamic>();
            var flash = new FlashAll() { CenterColor = "yeLLOw", CenterTickCount = 42, CenterTickOn = 20, CenterTickOff = 12, LeftColor = "dlack", LeftTickCount = 123, RightColor = "#12345678" };
            var strFlash = flash.ToString();
            animation.Actions.Add(strFlash);
            runner.Animations.Add(animation);

            // Act
            ProcessRunner.Build(runner);

            _portal.ResetEvent = new CancellationTokenSource(MaxWait);
            _portal.GetLastFunction = null;
            runner.Animations[0].CompiledActions[0].Invoke();
            var res = _portal.GetLastFunction;
            _portal.ResetEvent.Cancel();

            // Assert
            Assert.Equal("FadeAll({\"FlashForever\":false,\"TickOn\":20,\"TickOff\":20,\"TickCount\":20,\"Color\":{\"A\":255,\"B\":0,\"R\":255,\"G\":255},\"Enabled\":true}, {\"FlashForever\":false,\"TickOn\":0,\"TickOff\":0,\"TickCount\":123,\"Color\":{\"A\":255,\"B\":0,\"R\":0,\"G\":0},\"Enabled\":true}, {\"FlashForever\":false,\"TickOn\":0,\"TickOff\":0,\"TickCount\":0,\"Color\":{\"A\":18,\"B\":120,\"R\":52,\"G\":86},\"Enabled\":true})", res);
        }
    }
}
