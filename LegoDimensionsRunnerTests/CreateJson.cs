using LegoDimensions.Portal;
using Newtonsoft.Json;
using System.Diagnostics;

namespace LegoDimensionsRunnerTests
{
    public class CreateJson
    {
        [Fact]
        public void TestBasic()
        {
            Runner runner = new Runner();
            runner.Animations = new List<Animation>();
            Animation animation = new Animation();
            animation.Name = "Test";
            animation.Actions = new List<dynamic>();
            var setColor = new SetColor() { Pad = Pad.Center, Color = $"#{Color.Yellow.GetHashCode():X2}" };
            var strSetColor = setColor.ToString();
            var setColorAll = new SetColorAll() { Center = $"#{Color.Yellow.GetHashCode():X2}", Left = $"#{Color.Red.GetHashCode():X2}", Right = $"#{Color.Tan.GetHashCode():X2}", Duration = 200 };
            var strSetColorAll = setColorAll.ToString();
            animation.Actions.Add(strSetColor);
            animation.Actions.Add(strSetColorAll);
            runner.Animations.Add(animation);

            string ser = JsonConvert.SerializeObject(runner);

            var runnerDeser = JsonConvert.DeserializeObject<Runner>(ser);
            Assert.Equal(1, runnerDeser.Animations.Count);
            Assert.Equal("Test", runnerDeser.Animations[0].Name);
            Assert.Equal(strSetColor, runnerDeser.Animations[0].Actions[0]);
        }
        
        [Fact]
        public void TestBasicJson()
        {
            Runner runner = new Runner();
            runner.Animations = new List<Animation>();
            Animation animation = new Animation();
            animation.Name = "Test";
            animation.Actions = new List<dynamic>();
            var setColor = new SetColor() { Pad = Pad.Center, Color = $"#{Color.Yellow.GetHashCode():X2}" };
            var setColorAll = new SetColorAll() { Center = $"#{Color.Yellow.GetHashCode():X2}", Left = $"#{Color.Red.GetHashCode():X2}", Right = $"#{Color.Tan.GetHashCode():X2}", Duration = 200 };
            animation.Actions.Add(setColor);
            animation.Actions.Add(setColorAll);
            runner.Animations.Add(animation);

            string ser = JsonConvert.SerializeObject(runner);

            var runnerDeser = JsonConvert.DeserializeObject<Runner>(ser);
            Assert.Equal(1, runnerDeser.Animations.Count);
            Assert.Equal("Test", runnerDeser.Animations[0].Name);
            Assert.Equal(setColor.Name, (string)runnerDeser.Animations[0].Actions[0]["Name"]);
        }
    }
}