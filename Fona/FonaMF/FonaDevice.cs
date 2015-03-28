using System;
using System.IO.Ports;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

// FonaDevice implementation for NETMF

namespace Molarity.Hardare.AdafruitFona
{
    /// <summary>
    /// A delegate for handling notifications of a change in the power state of the Fona board.
    /// </summary>
    /// <param name="sender">The FonaDevice class that is sending this notification</param>
    /// <param name="args">The arguments describing the new power state.</param>
    public delegate void PowerStateEventHandler(object sender, PowerStateEventArgs args);

    public partial class FonaDevice
    {
        private bool _fSuppressPowerStateDetection = false;

        private OutputPort _onOffKeyPin;

        /// <summary>
        /// An output port that is connected to the Key pin on the Fona board.
        /// </summary>
        public OutputPort OnOffKeyPin
        {
            get { return _onOffKeyPin; }
            set { _onOffKeyPin = value; }
        }

        /// <summary>
        /// The current power state of the Fona device. True if it is powered on. False otherwise.
        /// You must provide a value for PowerStatePin to detect the power state. You must provide 
        /// a value for both PowerStatePin and OnOffKeyPin to change the power state of the Fona device.
        /// </summary>
        public bool PowerState
        {
            get
            {
                if (PowerStatePin==null)
                    throw new InvalidOperationException("You cannot detect the power state if you have not provided a value for PowerStatePin");

                return this.PowerStatePin.Read();
            }
            set
            {
                if (PowerStatePin == null)
                    throw new InvalidOperationException("You cannot change the power state if you have not provided a value for PowerStatePin");
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
        /// <summary>
        /// An output port that can be used to force a hardware reset of the Fona device.
        /// This port should be connected to the RST pin on the Fona board.
        /// You do not have to provide a value for the reset pin, although the Reset member
        /// of this class will be less able to restore the board to a working state without
        /// a value for the reset pin.
        /// </summary>
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
        /// <summary>
        /// This pin will be used for hardware-level detection of incoming calls and texts.
        /// The FonaDevice class can still detect incoming calls without this pin, but it
        /// does so by detecting the string "RING" sent by the board, which is less reliable
        /// and may result in multiple notifications for a single incoming call.
        /// Using the hardware pin is more accurate than depending on detection of the RING string.
        /// This port should be connected to the RI port on the Fona board.
        /// </summary>
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
        /// <summary>
        /// This pin is used to detect the current power state of the Fona board.
        /// You do not have to provide a value for this pin, but you will not be
        /// able to detect or control the power state of the Fona device without it.
        /// This pin works together with the OnOffKeyPin to control the power state
        /// of the Fona board. The PoweStatePin detects the power state and the OnOffKeyPin
        /// is used to change the power state.
        /// </summary>
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

        /// <summary>
        /// This event is raised when the current power state of the Fona board changes.
        /// If the board is turned on or off either via the 'Key' button on the Fona board
        /// or via the OnOffKeyPin, this event will be raised to indicate the change of
        /// power state.
        /// </summary>
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
