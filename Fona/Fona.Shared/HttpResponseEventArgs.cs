using System;
#if MF_FRAMEWORK
using Microsoft.SPOT;
#endif

namespace Molarity.Hardare.AdafruitFona
{
    public class HttpResponseEventArgs : EventArgs
    {
        internal HttpResponseEventArgs(FonaHttpResponse response)
        {
            
        }

        public FonaHttpResponse Response { get; private set; }
    }
}
