using System;
using System.IO.Ports;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Molarity.Hardare.AdafruitFona;
using Molarity.Hardware;

namespace MFFonaTest
{
    public class Program
    {
        private static FonaDevice _fona;
        private static OutputPort _onboardLed = new OutputPort(Cpu.Pin.GPIO_Pin14, false);
        private static OutputPort _resetPin = new OutputPort(Cpu.Pin.GPIO_Pin4, true);

        public static void Main()
        {
            // Bluetooth command interface on 'O Molecule' device. You may need to choose a different serial port for your device.
            var fonaPort = new AugmentedSerialPort("COM2", 9600, Parity.None, 8, StopBits.One);
            _fona = new FonaDevice(fonaPort);
            _fona.ResetPin = _resetPin;

            _fona.Reset();

            Debug.Print("IMEI = " + _fona.GetImei());
            Debug.Print("SIM CCID = " + _fona.GetSimCcid());

            bool state = true;
            while (true)
            {
                _onboardLed.Write(state);
                state = !state;
                Thread.Sleep(500);
            }

        }
    }
}
