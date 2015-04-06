using System;
using System.Text;

namespace Molarity.Hardare.AdafruitFona
{
    public class FonaHttpResponse
    {
        public FonaHttpResponse(int status, string body)
        {
            this.Status = status;
            this.Body = body;
        }
        public int Status { get; private set; }
        public string Body { get; private set; }
    }
}
