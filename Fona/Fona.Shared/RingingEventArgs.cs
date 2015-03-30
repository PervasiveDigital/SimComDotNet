using System;
#if MF_FRAMEWORK
using Microsoft.SPOT;
#endif

namespace Molarity.Hardare.AdafruitFona
{
    /// <summary>
    /// Event arguments describing an incoming call
    /// </summary>
    public class RingingEventArgs : EventArgs
    {
        /// <summary>
        /// Initialize the event arguments describing an incoming call
        /// </summary>
        /// <param name="eventTime">The time that the incoming call was detected</param>
        public RingingEventArgs(DateTime eventTime)
        {
            this.EventTime = eventTime;
        }

        /// <summary>
        /// The time that the incoming call was detected
        /// </summary>
        public DateTime EventTime { get; private set; }
    }
}
