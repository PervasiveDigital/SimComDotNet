using System;
#if MF_FRAMEWORK
using Microsoft.SPOT;
#endif

namespace Molarity.Hardare.AdafruitFona
{
    public class RingingEventArgs : EventArgs
    {
        public RingingEventArgs(DateTime eventTime)
        {
            this.EventTime = eventTime;
        }

        public DateTime EventTime { get; private set; }
    }
}
