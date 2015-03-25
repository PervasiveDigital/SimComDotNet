using System;
using System.IO.Ports;
using Microsoft.SPOT;

namespace Molarity.Hardware
{
    public class SerialPort
    {
        private readonly System.IO.Ports.SerialPort _port;
        private string _newLine = "\n";

        public SerialPort(System.IO.Ports.SerialPort port)
        {
            _port = port;
        }

        public void Open()
        {
            _port.Open();
        }

        public void WriteLine(string text)
        {
            throw new NotImplementedException();
        }

        public string ReadLine()
        {
            throw new NotImplementedException();
        }

        public string NewLine
        {
            get { return _newLine; }
            set { _newLine = value; }
        }

        public void DiscardInBuffer()
        {
            _port.DiscardInBuffer();
        }
    }
}
