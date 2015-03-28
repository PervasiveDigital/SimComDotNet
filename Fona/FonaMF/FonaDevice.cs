using System;
using System.IO.Ports;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

// FonaDevice implementation for NETMF

namespace Molarity.Hardare.AdafruitFona
{
    public delegate void PowerStateEventHandler(object sender, PowerStateEventArgs args);

    public partial class FonaDevice
    {
        private bool _fSuppressPowerStateDetection = false;

        private OutputPort _onOffKeyPin;

        public OutputPort OnOffKeyPin
        {
            get { return _onOffKeyPin; }
            set { _onOffKeyPin = value; }
        }

        public bool PowerState
        {
            get { return this.PowerStatePin.Read(); }
            set
            {
                if (this.OnOffKeyPin == null)
                    throw new InvalidOperationException("You cannot change the power state if you have not provided a value for the OnOffKeyPin");

                if (value)
                {
                    // Turn the fona on
                    if (this.PowerState)
                        return; // We are already on
                    TogglePowerState();
                }
                else
                {
                    // Turn the Fona off
                    if (!this.PowerState)
                        return; // We are already off
                    TogglePowerState();
                }
            }
        }

        private void TogglePowerState()
        {
            if (this.OnOffKeyPin == null)
                throw new InvalidOperationException("You cannot change the power state if you have not provided a value for the OnOffKeyPin");

            this.OnOffKeyPin.Write(false);
            Thread.Sleep(2000);
            this.OnOffKeyPin.Write(true);
        }

        private OutputPort _resetPin;
        public OutputPort ResetPin
        {
            get { return _resetPin; }
            set
            {
                _resetPin = value;
                _resetPin.Write(true);
            }
        }

        private InterruptPort _ringIndicatorPin;
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
            if (this.PowerStatePin != null)
            {
                if (!this.PowerStatePin.Read())
                {
                    // The phone cannot be ringing if the power is off, and this pin will
                    //   transition low during power-down.
                    return;
                }
            }
            if (this.Ringing != null)
            {
                Ringing(this, new RingingEventArgs(time));
            }
        }

        private bool HardwareRingIndicationEnabled
        {
            get { return this.RingIndicatorPin != null; }
        }

        private InputPort _powerStatePin;
        public InputPort PowerStatePin
        {
            get { return _powerStatePin; }
            set
            {
                if (_powerStatePin != null)
                {
                    _powerStatePin.OnInterrupt -= PowerStatePinOnInterrupt;
                }
                _powerStatePin = value;
                if (_powerStatePin != null)
                {
                    _powerStatePin.OnInterrupt += PowerStatePinOnInterrupt;
                    _powerStatePin.EnableInterrupt();
                }
            }
        }

        public event PowerStateEventHandler PowerStateChanged;

        private void PowerStatePinOnInterrupt(uint data1, uint data2, DateTime time)
        {
            // The power state will change briefly during a hardware reset
            if (_fSuppressPowerStateDetection)
                return;

            if (this.PowerStateChanged!=null)
            {
                PowerStateChanged(this, new PowerStateEventArgs(time, data2 != 0));
            }
        }

        private void DoHardwareReset()
        {
            if (ResetPin != null)
            {
                try
                {
                    _fSuppressPowerStateDetection = true;
                    // Drive reset pin low for 100ms.  
                    // Initial 10ms delay is to ensure that the device is ready to be reset as it may not be, right after startup.
                    Thread.Sleep(10);
                    ResetPin.Write(false);
                    Thread.Sleep(100);
                    ResetPin.Write(true);

                    // Wait 3s for a reboot
                    Thread.Sleep(3000);
                }
                finally
                {
                    _fSuppressPowerStateDetection = false;
                }
            }
        }

    }
}
