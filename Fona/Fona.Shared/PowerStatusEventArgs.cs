using System;
#if MF_FRAMEWORK
using Microsoft.SPOT;
#endif

namespace Molarity.Hardare.AdafruitFona
{
    /// <summary>
    /// Event arguments used to describe a change in the power state of the Fona device
    /// </summary>
    public class PowerStateEventArgs : EventArgs
    {
        /// <summary>
        /// Initialize a PowerStateEventArgs class
        /// </summary>
        /// <param name="eventTime">The time when the power state change was noticed</param>
        /// <param name="powerIsOn">True if the power has come on. False if the Fona device has powered down.</param>
        public PowerStateEventArgs(DateTime eventTime, bool powerIsOn)
        {
            this.EventTime = eventTime;
            this.PowerIsOn = powerIsOn;
        }

        /// <summary>
        /// The time that the power state change was noticed
        /// </summary>
        public DateTime EventTime { get; private set; }

        /// <summary>
        /// The current power state - true if the Fona device is powered on, false if it is now powered off
        /// </summary>
        public bool PowerIsOn { get; private set; }
    }
}
