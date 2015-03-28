using System;
#if MF_FRAMEWORK
using Microsoft.SPOT;
#endif

namespace Molarity.Hardare.AdafruitFona
{
    public class PowerStateEventArgs : EventArgs
    {
        public PowerStateEventArgs(DateTime eventTime, bool powerIsOn)
        {
            this.EventTime = eventTime;
            this.PowerIsOn = powerIsOn;
        }

        public DateTime EventTime { get; private set; }
        public bool PowerIsOn { get; private set; }
    }
}
