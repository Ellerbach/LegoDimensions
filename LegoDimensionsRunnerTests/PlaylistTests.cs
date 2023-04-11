using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegoDimensionsRunnerTests
{
    [Collection("ProcessRunner")]
    public class PlaylistTests
    {
        private const int MaxWait = 5000;
        private static TestPortal _portal;

        static PlaylistTests()
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
        public void SimplePlaylistTest()
        {
            // Arrange
            while (!_portal.ResetEvent.IsCancellationRequested)
            {
                _portal.ResetEvent.Token.WaitHandle.WaitOne(5000, true);
            }

            Runner runner = ProcessRunner.Deserialize("{\"anImaTions\":[{\"Name\":\"Test\",\"Actions\":[\"Name=SetColor,   PAD =     1, color = #FFFFFF00    \",\"Name=SetColorAll,Center=yellow,Left=#FFFF0000,Right=#FFD2B48C,Duration=200\"]},{\"Name\":\"Test2\",\"Actions\":[\"Name=SetColor,   PAD =     2, color = #FFFF0000    \",\"Name=SetColorAll,Center=yellow,Left=#FFFFFF00,Right=#FFD2B48C,Duration=200\"]}],\"Playlist\":[\"Test\",\"Test2\"]}");

            // Act
            ProcessRunner.Build(runner);

            _portal.ResetEvent = new CancellationTokenSource(MaxWait);
            _portal.GetLastFunction = null;
            CancellationTokenSource cs = new CancellationTokenSource(5000);
            ProcessRunner.Run(cs.Token);
            var res = _portal.GetLastFunction;
            _portal.ResetEvent.Cancel();

            // Assert
            // Should be more complex but that's enought for now
            Assert.NotNull(res);
        }
    }
}
