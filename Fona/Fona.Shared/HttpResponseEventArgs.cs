using System;
#if MF_FRAMEWORK
using Microsoft.SPOT;
#endif

namespace Molarity.Hardare.AdafruitFona
{
    /// <summary>
    /// Event arguments used when raising the event that signals the completion of an http request
    /// </summary>
    public class HttpResponseEventArgs : EventArgs
    {
        internal HttpResponseEventArgs(FonaHttpResponse response)
        {
            this.Response = response;
        }

        /// <summary>
        /// The http response information
        /// </summary>
        public FonaHttpResponse Response { get; private set; }
    }
}
