namespace Molarity.Hardare.AdafruitFona
{
    public static class SerialPortExtensions
    {
        public static void DiscardBufferedInput(this System.IO.Ports.SerialPort port)
        {
            port.DiscardInBuffer();
        }
    }
}
