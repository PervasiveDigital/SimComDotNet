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
        private readonly static OutputPort _onboardLed = new OutputPort(Cpu.Pin.GPIO_Pin14, false);
        private readonly static OutputPort _resetPin = new OutputPort(Cpu.Pin.GPIO_Pin4, true);
        private readonly static InterruptPort _powerStatePin = new InterruptPort((Cpu.Pin)16, true, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeBoth);
        private readonly static InterruptPort _ringIndicatorPin = new InterruptPort(Cpu.Pin.GPIO_Pin15, true, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeLow);

        public static void Main()
        {
            // Bluetooth command interface on 'O Molecule' device. You may need to choose a different serial port for your device.
            var fonaPort = new AugmentedSerialPort("COM2", 9600, Parity.None, 8, StopBits.One);
            _fona = new FonaDevice(fonaPort);
            _fona.ResetPin = _resetPin;
            _fona.RingIndicatorPin = _ringIndicatorPin;
            _fona.PowerStatePin = _powerStatePin;

            _fona.Ringing += FonaOnRinging;
            _fona.PowerStateChanged += FonaOnPowerStateChanged;

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

        private static void FonaOnPowerStateChanged(object sender, PowerStateEventArgs args)
        {
            Debug.Print("The Fona device was turned " + (args.PowerIsOn ? "on" : "off"));
        }

        private static void FonaOnRinging(object sender, RingingEventArgs args)
        {
            Debug.Print("The phone is ringing (or maybe we got a text).");
        }
    }
}
