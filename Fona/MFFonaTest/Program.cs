using System;
using System.IO.Ports;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Molarity.Hardare.AdafruitFona;

namespace MFFonaTest
{
    public class Program
    {
        private static FonaDevice _fona;
        private readonly static OutputPort _onboardLed = new OutputPort(Cpu.Pin.GPIO_Pin14, false);
        private readonly static OutputPort _resetPin = new OutputPort(Cpu.Pin.GPIO_Pin4, true);
        private readonly static OutputPort _keyPin = new OutputPort((Cpu.Pin)19, true);
        private readonly static InterruptPort _powerStatePin = new InterruptPort((Cpu.Pin)16, true, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeBoth);
        private readonly static InterruptPort _ringIndicatorPin = new InterruptPort(Cpu.Pin.GPIO_Pin15, true, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeLow);

        public static void Main()
        {
            // Bluetooth command interface on 'O Molecule' device. You may need to choose a different serial port for your device.
            var fonaPort = new SerialPort("COM2", 9600, Parity.None, 8, StopBits.One);
            _fona = new FonaDevice(fonaPort);
            _fona.ResetPin = _resetPin;
            _fona.RingIndicatorPin = _ringIndicatorPin;
            _fona.PowerStatePin = _powerStatePin;
            _fona.OnOffKeyPin = _keyPin;

            // Make sure we are powered on
            if (!_fona.PowerState)
                _fona.PowerState = true;

            // Make sure we get the local tower time, if available
            if (!_fona.RtcEnabled)
                _fona.RtcEnabled = true;

            // Reset the device to a known state
            _fona.Reset();

            // Watch for ringing phones and manual changes to the power state
            _fona.Ringing += FonaOnRinging;
            _fona.PowerStateChanged += FonaOnPowerStateChanged;

            _fona.EnableCallerId = true;
            _fona.CallerIdReceived += FonaOnCallerIdReceived;
            _fona.EnableSmsNotification = true;
            _fona.SmsMessageReceived += FonaOnSmsMessageReceived;

            // Output some identifying information
            Debug.Print("IMEI = " + _fona.GetImei());
            Debug.Print("SIM CCID = " + _fona.GetSimCcid());

            var status = _fona.GetNetworkStatus();
            Debug.Print("We are currently connected to : " + status.RegistrationStatus.ToString());
            if (status.LocationAreaCode!=null)
                Debug.Print("   Location area code : " + status.LocationAreaCode);
            if (status.CellId!=null)
                Debug.Print("   Cell ID : " + status.CellId);

            Debug.Print("Current RSSI : " + _fona.GetRssi());

            var count = _fona.GetSmsMessageCount();
            Debug.Print("There are " + count + " received sms messages");

            if (count > 0)
            {
                var sms = _fona.GetSmsMessage(count);
                Debug.Print("Latest text message:");
                Debug.Print("   Phone number : " + sms.Number);
                Debug.Print("   Address Type : " + sms.AddressType);
                Debug.Print("   Status : " + sms.Status);
                Debug.Print("   Timestamp : " + sms.Timestamp);
                Debug.Print("   Body : " + sms.Body);
            }

            Debug.Print("Battery Voltage : " + _fona.GetBatteryVoltage());
            Debug.Print("Battery charge state : " + _fona.GetBatteryChargeState());
            Debug.Print("Battery charge percentage : " + _fona.GetBatteryChargePercentage() + "% of fully charged.");
            Debug.Print("ADC Voltage : " + _fona.GetAdcVoltage());

            bool state = true;
            int iCount = 0;
            while (true)
            {
                _onboardLed.Write(state);
                state = !state;
                Thread.Sleep(500);
                if (++iCount == 20)
                {
                    Debug.Print("Current time = " + _fona.GetCurrentTime().ToString());
                    iCount = 0;
                }
            }
        }

        private static void FonaOnSmsMessageReceived(object sender, SmsMessageReceivedEventArgs args)
        {
            Debug.Print("A new sms message has been received. Storage=" + args.Storage + ", index=" + args.MessageIndex);
        }

        private static void FonaOnCallerIdReceived(object sender, CallerIdEventArgs args)
        {
            Debug.Print("Caller id information received:");
            Debug.Print("   Number : " + args.Number);
            Debug.Print("   Address type : " + args.AddressType);
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
