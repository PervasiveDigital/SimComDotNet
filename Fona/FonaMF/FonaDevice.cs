using System;
using System.IO.Ports;
using System.Threading;
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
            set
            {
                _resetPin = value;
                _resetPin.Write(true);
            }
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
            if (ResetPin != null)
            {
                // Drive reset pin low for 100ms.  
                // Initial 10ms delay is to ensure that the device is ready to be reset as it may not be, right after startup.
                Thread.Sleep(10);
                ResetPin.Write(false);
                Thread.Sleep(100);
                ResetPin.Write(true);

                // Wait 3s for a reboot
                Thread.Sleep(3000);
            }
        }

    }
}
