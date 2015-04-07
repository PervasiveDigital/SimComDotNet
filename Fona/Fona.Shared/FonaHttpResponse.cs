using System;
using System.Text;

namespace Molarity.Hardare.AdafruitFona
{
    /// <summary>
    /// Information about a response to an http request
    /// </summary>
    public class FonaHttpResponse
    {
        internal FonaHttpResponse(int status, string body)
        {
            this.Status = status;
            this.Body = body;
        }

        /// <summary>
        /// The returned http status
        /// </summary>
        public int Status { get; private set; }

        /// <summary>
        /// The body content that was returned
        /// </summary>
        public string Body { get; private set; }
    }
}
