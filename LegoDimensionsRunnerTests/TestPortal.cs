using LegoDimensions.Portal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegoDimensionsRunnerTests
{
    public class TestPortal : ILegoPortal
    {
        public bool NfcEnabled { get ; set ; }
        public bool GetTagDetails { get; set ; }

        public CancellationTokenSource ResetEvent { get; set ; }

        public int Id => 0;

        public TestPortal() {
            ResetEvent = new CancellationTokenSource();
            ResetEvent.Cancel();
        }

        public event EventHandler<LegoTagEventArgs>? LegoTagEvent;

        public string GetLastFunction { get; set ; }

        public void RaiseTheEvent(LegoTagEventArgs tagEvent) => LegoTagEvent?.Invoke(this, tagEvent);

        public void Fade(Pad pad, FadePad fadePad)
        {
            GetLastFunction = $"{nameof(Fade)}({pad}, {JsonConvert.SerializeObject(fadePad)})";
        }

        public void FadeAll(FadePad fadePadCenter, FadePad fadePadLeft, FadePad fadePadRight)
        {
            GetLastFunction = $"{nameof(FadeAll)}(This is a lot, so should work)";
        }

        public void FadeRandom(Pad pad, byte tickTime, byte tickCount)
        {
            GetLastFunction = $"{nameof(FadeRandom)}({pad}, {tickTime}, {tickCount})";
        }

        public void Flash(Pad pad, FlashPad flashPad)
        {
            GetLastFunction = $"{nameof(Flash)}({pad}, {JsonConvert.SerializeObject(flashPad)})";
        }

        public void FlashAll(FlashPad flashPadCenter, FlashPad flashPadLeft, FlashPad flashPadRight)
        {
            GetLastFunction = $"{nameof(FadeAll)}({JsonConvert.SerializeObject(flashPadCenter)}, {JsonConvert.SerializeObject(flashPadLeft)}, {JsonConvert.SerializeObject(flashPadRight)})";
        }

        public Color GetColor(Pad pad)
        {
            throw new NotImplementedException();
        }

        public void SetColor(Pad pad, Color color)
        {
            GetLastFunction = $"{nameof(SetColor)}({pad}, {color})";
        }

        public void SetColorAll(Color padCenter, Color padLeft, Color padRight)
        {
            GetLastFunction = $"{nameof(SetColorAll)}({padCenter}, {padLeft}, {padRight})";
        }

        public void SwitchOffAll()
        {
            GetLastFunction = $"{nameof(SwitchOffAll)}()";
        }

        public void WakeUp()
        {
            GetLastFunction = $"{nameof(WakeUp)}()";
        }
    }
}
