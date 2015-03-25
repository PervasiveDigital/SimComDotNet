using System;
using System.IO.Ports;
using System.Threading;

// Shared FonaDevice implementation

namespace Molarity.Hardare.AdafruitFona
{
    public partial class FonaDevice
    {
        private const string AT = "AT";
        private const string OK = "OK";
        private const string EchoOffCommand = "ATE0";
        private const string UnlockCommand = "AT+CPIN=";
        private const string GetCcidCommand = "AT+CCID";

        private readonly SerialPort _port;
        private object _lock = new object();

        public FonaDevice(SerialPort port)
        {
            _port = port;
            _port.NewLine = "\r\n";
            _port.Open();
        }

        public void Reset()
        {
            DoHardwareReset();

            _port.DiscardInBuffer();

            // Synchronize auto-baud
            for (int i = 0; i < 3; ++i)
            {
                try
                {
                    SendAndExpect(AT, OK);
                    Thread.Sleep(100);
                }
                catch (FonaException)
                {
                    // Ignore exceptions that occur during synchronization
                }
            }

            try
            {
                SendAndExpect(EchoOffCommand, OK);
                Thread.Sleep(100);
            }
            catch (FonaException)
            {
                // Ignore exceptions that occur during synchronization
            }

            // Throw, if we don't get an expected response here
            SendAndExpect(EchoOffCommand, OK);
        }

        public void UnlockSim(string code)
        {
            string command = UnlockCommand + code;
            SendAndExpect(command, OK);
        }

        public string GetSimCcid()
        {
            var response = SendAndReadReply(GetCcidCommand);
            Expect(OK);
            return response;
        }

        private void SendAndExpect(string send, string expect)
        {
            SendAndExpect(send, expect, TimeSpan.MaxValue);
        }

        private void SendAndExpect(string send, string expect, TimeSpan timeout)
        {
            lock (_lock)
            {
                _port.DiscardInBuffer();
                _port.WriteLine(send);
                Expect(new[] { send }, expect, timeout);
            }
        }

        private string SendAndReadReply(string command)
        {
            return SendAndReadReply(command, TimeSpan.MaxValue);
        }

        private string SendAndReadReply(string command, TimeSpan timeout)
        {
            string response;
            lock (_lock)
            {
                _port.DiscardInBuffer();
                _port.WriteLine(command);
                do
                {
                    response = GetReplyWithTimeout(timeout);
                } while (response == null || response == "");
            }
            return response;
        }
        private void Expect(string expect)
        {
            Expect(null, expect, TimeSpan.MaxValue);
        }

        private void Expect(string expect, TimeSpan timeout)
        {
            Expect(null, expect, timeout);
        }

        private void Expect(string[] accept, string expect)
        {
            Expect(accept, expect, TimeSpan.MaxValue);
        }

        private void Expect(string[] accept, string expect, TimeSpan timeout)
        {
            if (accept == null)
                accept = new[] {""};

            bool acceptableInputFound;
            string response;
            do
            {
                acceptableInputFound = false;
                response = GetReplyWithTimeout(timeout);
                response = response.Trim();

                foreach (var s in accept)
                {
#if MF_FRAMEWORK
                    if (response=="" || string.Equals(response.ToLower(), s.ToLower()))
#else
                    if (response=="" || string.Equals(response, s, StringComparison.OrdinalIgnoreCase))
#endif
                    {
                        acceptableInputFound = true;
                        break;
                    }
                }
            } while (acceptableInputFound);
#if MF_FRAMEWORK
            if (!string.Equals(response.ToLower(), expect.ToLower()))
#else
            if (!string.Equals(response, expect, StringComparison.OrdinalIgnoreCase))
#endif
            {
                throw new FonaExpectException(expect, response);
            }
        }

        private string GetReplyWithTimeout(TimeSpan timeout)
        {
            return _port.ReadLine();
        }

    }
}
