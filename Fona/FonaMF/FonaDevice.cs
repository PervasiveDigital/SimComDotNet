using System;
using System.IO.Ports;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

// FonaDevice implementation for NETMF

namespace Molarity.Hardare.AdafruitFona
{
    public partial class FonaDevice
    {
        private OutputPort _resetPin;
        private InterruptPort _ringIndicatorPin;

        public OutputPort ResetPin
        {
            get { return _resetPin; }
            set { _resetPin = value; }
        }

        public InterruptPort RingIndicatorPin
        {
            get { return _ringIndicatorPin; }
            set 
            {
                if (_ringIndicatorPin != null)
                {
                    _ringIndicatorPin.OnInterrupt -= RingIndicatorPinOnInterrupt;
                }
                _ringIndicatorPin = value;
                if (_ringIndicatorPin != null)
                {
                    _ringIndicatorPin.OnInterrupt += RingIndicatorPinOnInterrupt;
                    _ringIndicatorPin.EnableInterrupt();
                }
            }
        }


        private void RingIndicatorPinOnInterrupt(uint data1, uint data2, DateTime time)
        {
        }

        private void DoHardwareReset()
        {
        }

    }
}
