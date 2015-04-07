// Define this for very verbose debugging of the serial protocol interactions
#define VERBOSE
// Define this to turn off timeouts, which can get in the way when you are single-stepping
#if DEBUG
#define SUSPEND_TIMEOUT
#endif

using System;
using System.Collections;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Threading;

#if MF_FRAMEWORK
using Microsoft.SPOT;
#endif

// Shared FonaDevice implementation

namespace Molarity.Hardare.AdafruitFona
{
    /// <summary>
    /// Event handler for the FonaDevice.Ringing event. This event handler type is used to process
    /// an incoming call or SMS text.
    /// </summary>
    /// <param name="sender">The FonaDevice that detected the incoming call or text</param>
    /// <param name="args">Arguments describing the incoming call</param>
    public delegate void RingingEventHandler(object sender, RingingEventArgs args);

    /// <summary>
    /// Event handler for caller id information. This event handler is called when incoming caller id information is available
    /// </summary>
    /// <param name="sender">The FonaDevice that detected the incoming call</param>
    /// <param name="args">Detailed caller id information</param>
    public delegate void CallerIdEventHandler(object sender, CallerIdEventArgs args);

    /// <summary>
    /// Event handler for incoming Sms messages.
    /// </summary>
    /// <param name="sender">The FonaDevice that detected the incoming call</param>
    /// <param name="args">Detailed information about the received sms message.</param>
    public delegate void SmsMessageReceivedEventHandler(object sender, SmsMessageReceivedEventArgs args);

    /// <summary>
    /// Event handler for completed http requests
    /// </summary>
    /// <param name="sender">FonaDevice that sent the notification</param>
    /// <param name="args">Detailed information about the completed request</param>
    public delegate void HttpResponseEventHandler(object sender, HttpResponseEventArgs args);

    /// <summary>
    /// This class supports interactions with the Adafruit Fona GSM/GPRS breakout board.
    /// </summary>
    public partial class FonaDevice
    {
#if SUSPEND_TIMEOUT
        // useful in debugging where the timeout gets in the way of single-stepping
        private const int DefaultCommandTimeout = -1;
#else
        private const int DefaultCommandTimeout = 10000;
#endif
        private const int HttpTimeout = 30000;
        private const string AT = "AT";
        private const string OK = "OK";
        private const string FactoryResetCommand = "ATZ";
        private const string DialCommand = "ATD";
        private const string RedialCommand = "ATDL";
        private const string HangUpCommand = "ATH";
        private const string AnswerCommand = "ATA";
        private const string EchoOffCommand = "ATE0";
        private const string UnlockCommand = "AT+CPIN=";
        private const string GetCcidCommand = "AT+CCID";
        private const string GetImeiCommand = "AT+GSN";
        private const string ReadRtcCommand = "AT+CCLK?";
        private const string ReadRtcReply = "+CCLK: ";
        private const string SetLocalTimeStampEnable = "AT+CLTS=";
        private const string ReadLocalTimeStampEnable = "AT+CLTS?";
        private const string ReadLocalTimeStampEnableReply = "+CLTS: ";
        private const string WriteNvram = "AT&W";
        private const string SetEnableSmsRingIndicator = "AT+CFGRI=";
        private const string ReadEnableSmsRingIndicator = "AT+CFGRI?";
        private const string ReadEnableSmsRingIndicatorReply = "+CFGRI: ";
        private const string ReadNetworkStatus = "AT+CREG?";
        private const string ReadNetworkStatusReply = "+CREG: ";
        private const string ReadRssi = "AT+CSQ";
        private const string ReadRssiReply = "+CSQ: ";
        private const string ReadBatteryState = "AT+CBC";
        private const string ReadBatteryStateReply = "+CBC: ";
        private const string ReadAdcVoltage = "AT+CADC?";
        private const string ReadAdcVoltageReply = "+CADC: ";
        private const string SetAudioChannel = "AT+CHFA=";
        private const string ReadAudioChannel = "AT+CHFA?";
        private const string ReadAudioChannelReply = "+CHFA: ";
        private const string SetVolume = "AT+CLVL=";
        private const string ReadVolume = "AT+CLVL?";
        private const string ReadVolumeReply = "+CLVL: ";
        private const string SetEnableCallerId = "AT+CLIP=";
        private const string ReadEnableCallerId = "AT+CLIP?";
        private const string ReadEnableCallerIdReply = "+CLIP: ";
        private const string SetEnableSmsNotification = "AT+CNMI=";
        private const string ReadEnableSmsNotification = "AT+CNMI?";
        private const string ReadEnableSmsNotificationReply = "+CNMI: ";
        private const string ReadSmsMessageCount = "AT+CPMS?";
        private const string ReadSmsMessageCountReply = "+CPMS: ";
        private const string SetSmsMode = "AT+CMGF=";
        private const string SetSmsTextModeOutput = "AT+CSDH=";
        private const string ReadSmsMessage = "AT+CMGR=";
        private const string ReadSmsMessageReply = "+CMGR: ";
        private const string DeleteSmsMessageCommand = "AT+CMGD=";
        private const string ListSmsMessagesCommand = "AT+CMGL=";
        private const string ListSmsMessagesReply = "+CMGL: ";
        private const string HttpActionCommand = "AT+HTTPACTION=";
        private const string HttpActionReply = "+HTTPACTION: ";
        private const string HttpInitializeCommand = "AT+HTTPINIT";
        private const string HttpTerminateCommand = "AT+HTTPTERM";
        private const string SetHttpParameterCommand = "AT+HTTPPARA=";
        private const string SetHttpSslCommand = "AT+HTTPSSL=";
        private const string HttpReadCommand = "AT+HTTPREAD";
        private const string HttpReadReply = "+HTTPREAD: ";
        private const string SetAttachGprs = "AT+CGATT=";
        private const string ReadAttachGprs = "AT+CGATT?";
        private const string ReadAttachGprsReply = "+CGATT: ";
        private const string SetBearerProfile = "AT+SAPBR=";

        private static Thread _eventDispatchThread = null;
        private readonly SerialPort _port;
        private readonly object _lockSendExpect = new object();
        private bool _fIgnoreCLIP = false;

        /// <summary>
        /// Registration status is used to indicate the relationship that the Fona device has with a cell provider
        /// </summary>
        public enum RegistrationStatus
        {
            /// <summary>
            /// Not registered with any cell service
            /// </summary>
            NotRegistered = 0,
            /// <summary>
            /// Registered with Home provider
            /// </summary>
            Home = 1,
            /// <summary>
            /// Searching for a cell service provider
            /// </summary>
            Searching = 2,
            /// <summary>
            /// Registration was denied by cell service provider(s)
            /// </summary>
            RegistrationDenied = 3,
            /// <summary>
            /// Registration status is not known
            /// </summary>
            Unknown = 4,
            /// <summary>
            /// Registered with a provider that is not our home provider or
            /// outside our home region.
            /// </summary>
            Roaming = 5
        }

        /// <summary>
        /// The NetworkStatus structure is used to indicate the current connection status of the Fona device with respect to a network provider
        /// The LocationAreaCode and CellId are only valid for certain values of RegistrationStatus.
        /// </summary>
        public struct NetworkStatus
        {
            /// <summary>
            /// The current registration status. Indicates whether we are registered with a cell service provider
            /// </summary>
            public RegistrationStatus RegistrationStatus;
            /// <summary>
            /// If available, the local area code
            /// </summary>
            public string LocationAreaCode;
            /// <summary>
            /// If available, the local cell identification
            /// </summary>
            public string CellId;
        }


        /// <summary>
        /// This event is raised when an incoming call is detected. If an RI pin is available, the hardware indication is used.
        /// Otherwise, this class will look for the string "RING" from the Fona device. When not using the hardware pin, you may
        /// receive more than one ring indication for a single call because the board sends the RING string multiple times.
        /// </summary>
        public event RingingEventHandler Ringing;

        /// <summary>
        /// This event is raised when caller id information is presented for an incoming call.
        /// Note that you may receive multiple caller id notifications for a single incoming call.
        /// </summary>
        public event CallerIdEventHandler CallerIdReceived;

        /// <summary>
        /// This event is raised when a new SMS message is received, if you have set EnableSmsNotification
        /// to true. You do not need to set EnableRingIndicationForSms because the RI line is not used by
        /// this code to detect new SMS messages.
        /// </summary>
        public event SmsMessageReceivedEventHandler SmsMessageReceived;

        /// <summary>
        /// This event is raised when a response to an Http request is received. The arguments
        /// contain the result of the matching http request.  You should not issue a new http request
        /// until the results of the prior request are returned.
        /// </summary>
        public event HttpResponseEventHandler HttpResponseReceived;

        /// <summary>
        /// Create a FonaDevice class for use in communicating with the Adafruit Fona GSM/GPRS breakout board.
        /// </summary>
        /// <param name="port">This port should be configured for 8 data bits, one stop bit, no parity at pretty much any speed. The Fona device will autobaud when you call .Reset</param>
        public FonaDevice(SerialPort port)
        {
            _port = port;
            _port.DataReceived += PortOnDataReceived;
            _port.Open();

            if (_eventDispatchThread==null)
            {
                _eventDispatchThread = new Thread(EventDispatcher);
                _eventDispatchThread.Start();
            }

            this.HttpUserAgent = "Fona.net";
        }

        /// <summary>
        /// Reset the Fona device. Note that you must call this at least one so that the Fona device can auto-baud and synchronize with the
        /// baud rate you selected on your serial port.
        /// </summary>
        public void Reset()
        {
            DoHardwareReset();

            DiscardBufferedInput();

            // Synchronize auto-baud
            for (int i = 0; i < 3; ++i)
            {
                try
                {
                    SendAndExpect(AT, OK, 500);
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

        /// <summary>
        /// Return the Fona board to its factory settings
        /// </summary>
        public void FactoryReset()
        {
            SendAndExpect(FactoryResetCommand, OK);
        }

        /// <summary>
        /// Pass a four-digit code to unlock the SIM. WARNING: Calling this more than three times with the wrong code may lock your SIM and
        /// render it unusable until you unlock it with a special PUK code from your GSM provider.
        /// </summary>
        /// <param name="code"></param>
        public void UnlockSim(string code)
        {
            string command = UnlockCommand + code;
            SendAndExpect(command, OK);
        }

        /// <summary>
        /// Get the CCID identifier from your SIM. This is a number that uniquely identifies your SIM.
        /// </summary>
        /// <returns>SIM CCID</returns>
        public string GetSimCcid()
        {
            var response = SendCommandAndReadReply(GetCcidCommand);
            Expect(OK);
            return response;
        }

        /// <summary>
        /// Retrieve the IMEI for the Fona device.  This is a number that uniquely identifies your Fona breakout board.
        /// </summary>
        /// <returns>IMEI code from the Fona device</returns>
        public string GetImei()
        {
            var response = SendCommandAndReadReply(GetImeiCommand);
            Expect(OK);
            return response;
        }

        /// <summary>
        /// Read the current time from the real-time clock on the Fona
        /// </summary>
        /// <returns>The current date and time</returns>
        public DateTime GetCurrentTime()
        {
            var reply = SendCommandAndReadReply(ReadRtcCommand, ReadRtcReply, true);
            Expect(OK);

            var tokens = reply.Split(',');
            // We should have two tokens - a date part and a time part
            if (tokens.Length != 2)
                throw new FonaCommandException(ReadRtcReply, ReadRtcReply, tokens[0]);

            return ParseDateString(tokens[0], tokens[1]);
        }

        private DateTime ParseDateString(string dateString, string timeString)
        {
            var dateTokens = dateString.Split('/');
            if (dateTokens.Length != 3)
                throw new FonaException("Bad date format");
            var year = 2000 + int.Parse(dateTokens[0]);
            var month = int.Parse(dateTokens[1]);
            var day = int.Parse(dateTokens[2]);

            var timeTokens = timeString.Split(':');
            if (timeTokens.Length != 3)
                throw new FonaException("Bad time format");
            var temp = timeTokens[2].Split('+');
            timeTokens[2] = temp[0];
            int tz = 0;
            if (temp.Length == 2)
                tz = int.Parse(temp[1]);
            var hour = int.Parse(timeTokens[0]);
            var minute = int.Parse(timeTokens[1]);
            var second = int.Parse(timeTokens[2]);
            var result = new DateTime(year, month, day, hour, minute, second);
            //result.AddHours(tz);
            return new DateTime(result.Ticks, DateTimeKind.Local);
        }

        /// <summary>
        /// Check or set the current state of the local timestamp setting.
        /// When this is set, the Fona will return the network-provided local time.
        /// When not set, the time that is returned is based on a manual setting and not
        /// the network provider's cell-tower time. Note that after changing the setting,
        /// you must reset the device by calling Reset (with a hardware reset pin defined)
        /// or power the Fona device off and then on again.
        /// </summary>
        public bool RtcEnabled
        {
            get
            {
                var reply = SendCommandAndReadReply(ReadLocalTimeStampEnable, ReadLocalTimeStampEnableReply, false);
                Expect(OK);
                return (int.Parse(reply) != 0);
            }
            set
            {
                if (value == this.RtcEnabled)
                    return;
                SendAndExpect(SetLocalTimeStampEnable + (value ? "1" : "0"), OK);
                SendAndExpect(WriteNvram, OK);
            }
        }

        /// <summary>
        /// Return the current network registration status
        /// </summary>
        /// <returns>A NetworkStatus structure indicating the current registration status</returns>
        public NetworkStatus GetNetworkStatus()
        {
            NetworkStatus result = new NetworkStatus();

            var tokens = SendCommandAndParseReply(ReadNetworkStatus, ReadNetworkStatusReply, ',', false);
            Expect(OK);
            if (tokens.Length < 2)
                throw new FonaException("Bad reply to ReadNetworkStatus command");
            var n = int.Parse(tokens[0]);
            result.RegistrationStatus = (RegistrationStatus)int.Parse(tokens[1]);
            if (tokens.Length > 2)
                result.LocationAreaCode = tokens[3];
            if (tokens.Length > 3)
                result.CellId = tokens[4];

            return result;
        }

        /// <summary>
        /// Return an code for the current received signal string indication.
        /// 0 is -115Dbm and 31 is -52Dbm or less.  99 is "not known or not detectable"
        /// </summary>
        /// <returns></returns>
        public int GetRssi()
        {
            var tokens = SendCommandAndParseReply(ReadRssi, ReadRssiReply, ',', false);
            Expect(OK);
            return int.Parse(tokens[0]);
        }

        /// <summary>
        /// Read the battery voltage
        /// </summary>
        /// <returns>Battery voltage in mV</returns>
        public int GetBatteryVoltage()
        {
            var tokens = SendCommandAndParseReply(ReadBatteryState, ReadBatteryStateReply, ',', false);
            Expect(OK);
            return int.Parse(tokens[2]);
        }

        /// <summary>
        /// The BatteryChargeState is used to describe the charging/discharging status of the battery
        /// </summary>
        public enum BatteryChargeState
        {
            /// <summary>
            /// The battery is not charging and the Fona is running on battery power
            /// </summary>
            Discharging = 0,
            /// <summary>
            /// The battery is charging
            /// </summary>
            Charging = 1,
            /// <summary>
            /// The battery is not charging and the Fona is running on USB power
            /// </summary>
            FullyCharged = 2
        }

        /// <summary>
        /// Return the current battery charge state (discharging, charging, etc)
        /// </summary>
        /// <returns>BatteryChargeState enum identifying the current charging state</returns>
        public BatteryChargeState GetBatteryChargeState()
        {
            var tokens = SendCommandAndParseReply(ReadBatteryState, ReadBatteryStateReply, ',', false);
            Expect(OK);
            return (BatteryChargeState)int.Parse(tokens[0]);
        }


        /// <summary>
        /// Get the current battery charge state (as a percentage of fully charged)
        /// </summary>
        /// <returns>The current charge state as a percentage (*100) of fully charged</returns>
        public int GetBatteryChargePercentage()
        {
            var tokens = SendCommandAndParseReply(ReadBatteryState, ReadBatteryStateReply, ',', false);
            Expect(OK);
            return int.Parse(tokens[1]);
        }

        /// <summary>
        /// Read the analog-to-digital-converter voltage
        /// </summary>
        /// <returns>ADC voltage in mV</returns>
        public int GetAdcVoltage()
        {
            var tokens = SendCommandAndParseReply(ReadAdcVoltage, ReadAdcVoltageReply, ',', false);
            Expect(OK);
            return int.Parse(tokens[1]);
        }

        /// <summary>
        /// If false, the Fona will use the headset jack.  If true, the Fona will use the external audio channels.
        /// </summary>
        public bool UseExternalAudio
        {
            get
            {
                var reply = SendCommandAndReadReply(ReadAudioChannel, ReadAudioChannelReply, false);
                Expect(OK);
                return int.Parse(reply) == 1;
            }
            set
            {
                SendAndExpect(SetAudioChannel + (value ? "1" : "0"),OK);
            }
        }

        /// <summary>
        /// Set the speaker volume. Values are between 0 (quiet) and 100 (loud)
        /// </summary>
        public int Volume
        {
            get
            {
                var reply = SendCommandAndReadReply(ReadVolume, ReadVolumeReply, false);
                Expect(OK);
                return int.Parse(reply);
            }
            set
            {
                if (value < 0 || value > 100)
                    throw new ArgumentException("value must be between 0 and 100");
                SendAndExpect(SetVolume + value.ToString(), OK);                
            }
        }

        /// <summary>
        /// Dial a number to initiate a voice call
        /// </summary>
        /// <param name="number">The number to call</param>
        public void CallPhone(string number)
        {
            SendAndExpect(DialCommand + number + ";", OK);
        }

        /// <summary>
        /// Disconnect an active voice call
        /// </summary>
        public void HangUp()
        {
            SendAndExpect(HangUpCommand, OK);
        }

        /// <summary>
        /// Answer an incoming voice call
        /// </summary>
        public void AnswerIncomingCall()
        {
            SendAndExpect(AnswerCommand, OK);
        }

        /// <summary>
        /// Redial the last outbound number
        /// </summary>
        public void Redial()
        {
            SendAndExpect(RedialCommand, OK);
        }

        /// <summary>
        /// Enable caller id for incoming calls
        /// </summary>
        public bool EnableCallerId
        {
            get
            {
                var tokens = SendCommandAndParseReply(ReadEnableCallerId, ReadEnableCallerIdReply, ',', false);
                Expect(OK);
                return int.Parse(tokens[0]) == 1;
            }
            set
            {
                try
                {
                    // This flag is used so that the input parser does not confuse the response to this command with the unsolicited CLIP
                    //   notification used by caller ID
                    _fIgnoreCLIP = true;
                    SendAndExpect(SetEnableCallerId + (value ? "1" : "0"), OK);
                }
                finally
                {
                    _fIgnoreCLIP = false;
                }
            }
        }


        #region SMS Support

        /// <summary>
        /// This value is true if the RI pin will transition when an SMS is received. This is
        /// not required for enabling SMS notification. If all you need is notification of new
        /// incoming messages, use EnableSmsNotification=true and provide a delegate for SmsMessageReceived
        /// </summary>
        public bool EnableRingIndicationForSms
        {
            get
            {
                var reply = SendCommandAndReadReply(ReadEnableSmsRingIndicator, ReadEnableSmsRingIndicatorReply, false);
                Expect(OK);
                return (int.Parse(reply) != 0);
            }
            set
            {
                SendAndExpect(SetEnableSmsRingIndicator + (value ? "1" : "0"), OK);
            }
        }

        /// <summary>
        /// True to enable async notification of incoming SMS messages.
        /// </summary>
        public bool EnableSmsNotification
        {
            get
            {
                var reply = SendCommandAndReadReply(ReadEnableSmsNotification, ReadEnableSmsNotificationReply, false);
                Expect(OK);
                return (int.Parse(reply) != 0);
            }
            set
            {
                // Use a mode that will buffer the notification if we are using the data channel (e.g., PPP connection)
                // The ,1 indicates that we want the index of the message returned too.
                SendAndExpect(SetEnableSmsNotification + (value ? "2,1" : "0"), OK);
            }
        }

        /// <summary>
        /// Return the number of SMS messages in memory
        /// </summary>
        /// <returns>The count of stored messages</returns>
        public int GetSmsMessageCount()
        {
            SendAndExpect(SetSmsMode + "1", OK);
            var tokens = SendCommandAndParseReply(ReadSmsMessageCount, ReadSmsMessageCountReply, ',', false);
            Expect(OK);
            if (tokens.Length!=9)
                throw new FonaException("Bad response - expected 9 tokens and received " + tokens.Length);

            // Return the amount of used storage for reading messages
            return int.Parse(tokens[1]);
        }

        /// <summary>
        /// Delete a single SMS message
        /// </summary>
        /// <param name="index">Index of the message to delete</param>
        public void DeleteSmsMessage(int index)
        {
            SendAndExpect(SetSmsMode + "1", OK);
            SendAndExpect(DeleteSmsMessageCommand + index + ",0", OK);
        }

        /// <summary>
        /// Specify which SMS messages the operation should affect
        /// </summary>
        public enum WhichMessages
        {
            /// <summary>
            /// Only the single specified message
            /// </summary>
            SpecifiedMessageOnly = 0,
            /// <summary>
            /// All messages that have been read (leaving unread and unsent)
            /// </summary>
            AllReadMessages = 1,
            /// <summary>
            /// All messages marked as having been read or sent
            /// </summary>
            ReadOrSentMessages = 2,
            /// <summary>
            /// All messages except for received messages that are unread
            /// </summary>
            AllExceptUnread = 3,
            /// <summary>
            /// All messages regardless of status
            /// </summary>
            AllMessages = 4
        }

        /// <summary>
        /// Delete all SMS messages with a specified status. Note that this command
        /// can take a substantial amount of time to run (25s for 50 or more messages).
        /// </summary>
        /// <param name="which">Indicates which status(es) are to be used to select messages to be deleted.</param>
        public void DeleteSmsMessages(WhichMessages which)
        {
            if (which == WhichMessages.SpecifiedMessageOnly)
                throw new ArgumentException("Use DeleteSmsMessage(index) instead");

            SendAndExpect(SetSmsMode + "1", OK);
            // This command can take a long time to execute
            SendAndExpect(DeleteSmsMessageCommand + "0," + (int)which, OK, 30000);
        }

        /// <summary>
        /// Retrieve a received SMS message
        /// </summary>
        /// <param name="index">The index indicating which message to retrieve</param>
        /// <returns>An object containing the SMS message and related metadata</returns>
        public SmsMessage GetSmsMessage(int index)
        {
            // Text mode
            SendAndExpect(SetSmsMode + "1", OK);

            // Show all parameters
            SendAndExpect(SetSmsTextModeOutput + "1", OK);

            var tokens = SendCommandAndParseReply(ReadSmsMessage + index, ReadSmsMessageReply, ',', false);
            var body = GetReplyWithTimeout(DefaultCommandTimeout);
            Expect(OK);

            // tokens 3 and 4 are a date and time with an embedded quote, which our ParseReply code splits in two - remove the quote remnants
            if (tokens[3][0] == '"')
                tokens[3] = tokens[3].Substring(1);
            if (tokens[4][tokens[4].Length - 1] == '"')
                tokens[4] = tokens[4].Substring(0, tokens[4].Length - 1);

            return new SmsMessage(index, SmsMessage.ParseMessageStatus(Unquote(tokens[0])), Unquote(tokens[1]), (AddressType)int.Parse(Unquote(tokens[5])), ParseDateString(tokens[3], tokens[4]) , body);
        }

        /// <summary>
        /// Get all sms messages by message status
        /// </summary>
        /// <param name="status">The set of messages to retrieve. For instance, read vs unread vs all.</param>
        /// <param name="fClearUnreadFlag"></param>
        /// <returns></returns>
        public SmsMessage[] GetSmsMessages(SmsMessageStatus status, bool fClearUnreadFlag)
        {
            ArrayList result = new ArrayList();
            // Text mode
            SendAndExpect(SetSmsMode + "1", OK);
            // Show all parameters
            SendAndExpect(SetSmsTextModeOutput + "1", OK);

            SendCommand(ListSmsMessagesCommand + '"' + SmsMessage.ConvertToStatString(status) + "\"," + (fClearUnreadFlag ? "1" : "0"));
            var reply = GetReplyWithTimeout(DefaultCommandTimeout).Trim();
            while (reply != OK)
            {
                var info = reply.Substring(ListSmsMessagesReply.Length);
                var tokens = info.Split(',');
                var body = GetReplyWithTimeout(DefaultCommandTimeout);

                var index = int.Parse(tokens[0]);
                var msgStat = SmsMessage.ParseMessageStatus(Unquote(tokens[1]));
                var addrType = (AddressType) int.Parse(Unquote(tokens[6]));

                // tokens 3 and 4 are a date and time with an embedded quote. Remove the quote remnants
                if (tokens[4][0] == '"')
                    tokens[4] = tokens[4].Substring(1);
                if (tokens[5][tokens[5].Length - 1] == '"')
                    tokens[5] = tokens[5].Substring(0, tokens[5].Length - 1);
                var timestamp = ParseDateString(tokens[4], tokens[5]);

                result.Add(new SmsMessage(index, msgStat, Unquote(tokens[2]), addrType, timestamp, body));

                // Keep going until we eat an 'ok'
                reply = GetReplyWithTimeout(DefaultCommandTimeout).Trim();
            }

            return (SmsMessage[])result.ToArray(typeof (SmsMessage));
        }

        #endregion

        #region GPRS

        public string Apn
        {
            get; set;
        }

        public string ApnUsername { get; set; }
        public string ApnPassword { get; set; }

        public void EnableGprs(bool enable)
        {
            
        }

        public bool GprsAttached
        {
            get
            {
                var reply = SendCommandAndReadReply(ReadAttachGprs, ReadAttachGprsReply, false);
                Expect(OK);
                return (int.Parse(reply) != 0);                
            }
            set
            {
                if (value)
                {
                    SendAndExpect(SetAttachGprs + '1', OK);
                    SendAndExpect(SetBearerProfile + "3,1,\"CONTYPE\",\"GPRS\"", OK);

                    if (this.Apn == null || this.Apn.Length == 0)
                        throw new FonaException("APN not set - cannot enable GPRS");

                    SendAndExpect(SetBearerProfile + "3,1,\"APN\",\"" + this.Apn + '"', OK);

                    if (this.ApnUsername != null && this.ApnUsername.Length > 0)
                        SendAndExpect(SetBearerProfile + "3,1,\"USER\",\"" + this.ApnUsername + '"', OK);
                    if (this.ApnPassword!= null && this.ApnPassword.Length > 0)
                        SendAndExpect(SetBearerProfile + "3,1,\"PWD\",\"" + this.ApnPassword + '"', OK);

                    SendAndExpect(SetBearerProfile + "1,1", OK);
                }
                else
                {
                    SendAndExpect(SetBearerProfile + "0,1", OK);
                    SendAndExpect(SetAttachGprs + '0', OK);
                }
            }
        }

        #endregion

        #region HTTP Support

        public string HttpUserAgent { get; set; }

        public void SendHttpRequest(string verb, string url, bool allowHttpRedirect, string body)
        {
            HttpInitialize(url, allowHttpRedirect);

            string reply;
            if (verb.ToUpper()=="GET")
                SendAndExpect(HttpActionCommand + '0', OK);
            else if (verb.ToUpper() == "POST")
                SendAndExpect(HttpActionCommand + '1', OK);
            else if (verb.ToUpper() == "HEAD")
                SendAndExpect(HttpActionCommand + '2', OK);
            else
                throw new ArgumentException("Only GET, POST and HEAD are supported");
        }

        private void HttpInitialize(string url, bool allowHttpRedirect)
        {
            SendCommand(HttpTerminateCommand);
            // We don't really care about the reply to this - in fact, it will fail the first time HttpInitialize is called
            GetReplyWithTimeout(DefaultCommandTimeout);

            SendAndExpect(HttpInitializeCommand, OK);
            SendAndExpect(SetHttpParameterCommand + "\"CID\",1", OK);
            if (this.HttpUserAgent!=null && this.HttpUserAgent.Length>0)
                SendAndExpect(SetHttpParameterCommand + "\"UA\", \"" + this.HttpUserAgent + '"', OK);
            SendAndExpect(SetHttpParameterCommand + "\"URL\",\"" + url + '"', OK);

            SendAndExpect(SetHttpParameterCommand + "\"REDIR\"," + (allowHttpRedirect ? '1' : '0'), OK);
            //SendAndExpect(SetHttpSslCommand + (allowHttpRedirect ? '1' : '0'), OK);
        }

        private void HttpTerminate()
        {
            SendAndExpect(HttpTerminateCommand, OK);
        }

        #endregion

        #region Sending Commands

        private void SendCommand(string send)
        {
            lock (_lockSendExpect)
            {
                DiscardBufferedInput();
                WriteLine(send);
            }
        }

        private void SendAndExpect(string send, string expect)
        {
            SendAndExpect(send, expect, DefaultCommandTimeout);
        }

        private void SendAndExpect(string send, string expect, int timeout)
        {
            lock (_lockSendExpect)
            {
                DiscardBufferedInput();
                WriteLine(send);
                Expect(new[] { send }, expect, timeout);
            }
        }

        private string SendCommandAndReadReply(string command, string replyPrefix, bool replyIsQuoted)
        {
            return SendCommandAndReadReply(command, replyPrefix, replyIsQuoted, DefaultCommandTimeout);
        }

        private string SendCommandAndReadReply(string command, string replyPrefix, bool replyIsQuoted, int timeout)
        {
            var reply = SendCommandAndReadReply(command, DefaultCommandTimeout);
            if (reply.IndexOf(replyPrefix) != 0)
            {
                throw new FonaCommandException(command, replyPrefix, reply);
            }
            reply = reply.Substring(replyPrefix.Length);
            if (replyIsQuoted)
            {
                reply = Unquote(reply);
            }
            return reply;
        }

        private string[] SendCommandAndParseReply(string command, string replyPrefix, char delimiter, bool replyIsQuoted)
        {
            return SendCommandAndParseReply(command, replyPrefix, delimiter, replyIsQuoted, DefaultCommandTimeout);
        }

        private string[] SendCommandAndParseReply(string command, string replyPrefix, char delimiter, bool replyIsQuoted, int timeout)
        {
            var reply = SendCommandAndReadReply(command, replyPrefix, replyIsQuoted, timeout);
            return reply.Split(delimiter);
        }

        private string SendCommandAndReadReply(string command)
        {
            return SendCommandAndReadReply(command, DefaultCommandTimeout);
        }

        private string SendCommandAndReadReply(string command, int timeout)
        {
            string response;
            lock (_lockSendExpect)
            {
                DiscardBufferedInput();
                WriteLine(command);
                do
                {
                    response = GetReplyWithTimeout(timeout);
                } while (response == null || response == "" || response==command);
            }
            return response;
        }

        #endregion

        #region Parse responses

        private void Expect(string expect)
        {
            Expect(null, expect, DefaultCommandTimeout);
        }

        private void Expect(string expect, int timeout)
        {
            Expect(null, expect, timeout);
        }

        private void Expect(string[] accept, string expect)
        {
            Expect(accept, expect, DefaultCommandTimeout);
        }

        private void Expect(string[] accept, string expect, int timeout)
        {
            if (accept == null)
                accept = new[] {""};

            bool acceptableInputFound;
            string response;
            do
            {
                acceptableInputFound = false;
                response = GetReplyWithTimeout(timeout);

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

        private string GetReplyWithTimeout(int timeout)
        {
            string response = null;
            bool haveNewData;
            do
            {
                lock (_responseQueueLock)
                {
                    if (_responseQueue.Count > 0)
                    {
                        response = (string)_responseQueue[0];
                        _responseQueue.RemoveAt(0);
                    }
                    else
                    {
                        _responseReceived.Reset();
                    }
                }

                // If nothing was waiting in the queue, then wait for new data to arrive
                haveNewData = false;
                if (response == null)
                    haveNewData = _responseReceived.WaitOne(timeout, false);

            } while (response==null && haveNewData);

            // We have received no data, and the WaitOne timed out
            if (response == null && !haveNewData)
            {
                throw new FonaCommandTimeout();
            }

            return response;
        }

        #endregion

        #region Serial Helpers

        private static readonly object _eventQueueLock = new object();
        private static readonly ArrayList _eventQueue = new ArrayList();
        private static readonly AutoResetEvent _eventEnqueued = new AutoResetEvent(false);

        private readonly object _responseQueueLock = new object();
        private readonly ArrayList _responseQueue = new ArrayList();
        private readonly AutoResetEvent _responseReceived = new AutoResetEvent(false);
        private string _buffer;
        private StringBuilder _stream = new StringBuilder();
        private int _cbStream = 0;

        private void PortOnDataReceived(object sender, SerialDataReceivedEventArgs serialDataReceivedEventArgs)
        {
            if (serialDataReceivedEventArgs.EventType == SerialData.Chars)
            {
                string newInput = ReadExisting();
#if VERBOSE
                Dbg("ReadExisting : " + newInput);
#endif
                if (newInput != null && newInput.Length > 0)
                {
                    _buffer += newInput;

                    if (_cbStream != 0)
                    {
                        // If we are capturing an input stream, then copy characters from the serial port
                        //   until the count of desired characters == 0
                        while (_cbStream > 0 && _buffer.Length > 0)
                        {
                            var eat = System.Math.Min(_buffer.Length, _cbStream);
                            _stream.Append(_buffer.Substring(0, eat));
                            _buffer = _buffer.Substring(eat);
                            _cbStream -= eat;
                        }
                        // If we have fulfilled the stream request, then add the stream as a whole to the response queue
                        if (_cbStream == 0)
                        {
                            lock (_responseQueueLock)
                            {
                                _responseQueue.Add(_stream.ToString());
                                _stream.Clear();
                                _responseReceived.Set();
                            }
                        }
                    }

                    // process whatever is left in the buffer (after fulfilling any stream requests)
                    var idxNewline = _buffer.IndexOf('\n');
                    while (idxNewline != -1 && _cbStream==0)
                    {
                        var line = _buffer.Substring(0, idxNewline);
                        _buffer = _buffer.Substring(idxNewline + 1);
                        while (line.Length > 0 && line[line.Length - 1] == '\r')
                            line = line.Substring(0, line.Length - 1);
                        if (line.Length > 0)
                        {
#if VERBOSE
                            Dbg("Received Line : " + line);
#endif
                            bool handled;
                            HandleUnsolicitedResponses(line, out handled);
                            if (!handled)
                            {
                                lock (_responseQueueLock)
                                {
#if VERBOSE
                                    Dbg("Enqueue Line : " + line);
#endif
                                    _responseQueue.Add(line);
                                    _responseReceived.Set();
                                }
                            }
                        }

                        // See if we have another line buffered
                        idxNewline = _buffer.IndexOf('\n');
                    }
                }
            }
        }

        private void HandleUnsolicitedResponses(string line, out bool handled)
        {
            handled = false;

            if (line.ToUpper() == "RING")
            {
                // If we received a line with 'RING' on it, and the hardware ring pin
                //   is not enabled, then raise a ring event. In this case, the RING
                //   indication may be raised several times for a single incoming call.
                //   Otherwise, eat the RING string because it will just confuse us
                if (!this.HardwareRingIndicationEnabled)
                {
                    RaiseTextRingEvent();
                    handled = true;
                }
            }
            else if (line.IndexOf("+CLIP: ") == 0)
            {
                if (!_fIgnoreCLIP)
                {
                    // caller id information
                    RaiseCallerIdEvent(line.Substring(7));
                    handled = true;
                }
            }
            else if (line.IndexOf("+CMTI: ") == 0)
            {
                // caller id information
                RaiseSmsReceivedEvent(line.Substring(7));
                handled = true;
            }
            else if (line.IndexOf(ListSmsMessagesReply) == 0)
            {
                var info = line.Substring(ListSmsMessagesReply.Length);
                var tokens = info.Split(',');
                if (tokens.Length > 11)
                {
                    _cbStream = int.Parse(tokens[12]);
                    _stream.Clear();
                }
                // after stripping off the stream count, we still need to return this line
                handled = false;
            }
            else if (line.IndexOf(ReadSmsMessageReply) == 0)
            {
                var info = line.Substring(ReadSmsMessageReply.Length);
                var tokens = info.Split(',');
                if (tokens.Length > 11)
                {
                    _cbStream = int.Parse(tokens[11]);
                    _stream.Clear();
                }
                // after stripping off the stream count, we still need to return this line
                handled = false;
            }
            else if (line.IndexOf(HttpActionReply) == 0)
            {
                var info = line.Substring(HttpActionReply.Length);
                var tokens = info.Split(',');
                if (tokens.Length > 2)
                {
                    var status = int.Parse(tokens[1]);
                    var replyLength = int.Parse(tokens[2]);

                    HandleHttpResponse(status, replyLength);
                }
                handled = true;
            }
            else if (line.IndexOf(HttpReadReply) == 0)
            {
                var info = line.Substring(HttpReadReply.Length);
                _cbStream = int.Parse(info);
                _stream.Clear();

                // after stripping off the stream count, we still need to return this line
                handled = false;
            }
            // we eat all of these so that they don't confuse anything else.  This is not a complete list, but it seems to be complete enough.
            else if (line.IndexOf("+SAPBR:") == 0 || line.IndexOf("+CPIN:") == 0 || line.IndexOf("+PDP:") == 0 || line.IndexOf("+CTZV:") == 0 ||
                line.IndexOf("*PSUTTZ:") == 0 || line.IndexOf("Call Ready") == 0 || line.IndexOf("SMS Ready") == 0 || line.IndexOf("DST:") == 0)
            {
                handled = true;
            }
        }

        private class EventForDispatch
        {
            public object Sender;
            public object EventArgs;
        }

        private void HandleHttpResponse(int status, int replyLength)
        {
            if (status >= 400)
            {
                // Send eventargs with error
                EnqueueEvent(this, new HttpResponseEventArgs(new FonaHttpResponse(status,null)));
                return;
            }
            new Thread(() =>
            {
                // Trigger the return of the body data
                var reply = SendCommandAndReadReply(HttpReadCommand /*+ "0," + replyLength*/);
                EnqueueEvent(this, new HttpResponseEventArgs(new FonaHttpResponse(-1, reply)));

                var replyBody = GetReplyWithTimeout(DefaultCommandTimeout);
                Expect(OK);
                EnqueueEvent(this, new HttpResponseEventArgs(new FonaHttpResponse(status, replyBody)));
            }).Start();
        }

        private void RaiseTextRingEvent()
        {
            EnqueueEvent(this, new RingingEventArgs(DateTime.UtcNow));
        }

        private void RaiseCallerIdEvent(string callerId)
        {
            var tokens = callerId.Split(',');
            if (tokens.Length > 1)
            {
                EnqueueEvent(this, new CallerIdEventArgs(Unquote(tokens[0]), (AddressType) int.Parse(tokens[1])));
            }
        }

        private void RaiseSmsReceivedEvent(string status)
        {
            var tokens = status.Split(',');
            if (tokens.Length > 1)
            {
                var storageCode = tokens[0];
                var index = int.Parse(tokens[1]);

                EnqueueEvent(this, new SmsMessageReceivedEventArgs(storageCode, index));
            }
        }

        private static void EnqueueEvent(object sender, EventArgs args)
        {
            lock (_eventQueueLock)
            {
                _eventQueue.Add(new EventForDispatch() { Sender = sender, EventArgs = args });
                _eventEnqueued.Set();
            }
        }

        private static void EventDispatcher()
        {
            while (true)
            {
                try
                {
                    object item;
                    do
                    {
                        item = null;
                        lock (_eventQueueLock)
                        {
                            if (_eventQueue.Count > 0)
                            {
                                item = _eventQueue[0];
                                _eventQueue.RemoveAt(0);
                            }
                        }
                        var eventForDispatch = item as EventForDispatch;
                        if (eventForDispatch != null)
                        {
                            var device = (FonaDevice) eventForDispatch.Sender;
                            if (eventForDispatch.EventArgs is RingingEventArgs)
                            {
                                if (device.Ringing != null)
                                    device.Ringing(device, (RingingEventArgs) eventForDispatch.EventArgs);
                            }
                            else if (eventForDispatch.EventArgs is CallerIdEventArgs)
                            {
                                if (device.CallerIdReceived != null)
                                    device.CallerIdReceived(device, (CallerIdEventArgs) eventForDispatch.EventArgs);
                            }
                            else if (eventForDispatch.EventArgs is SmsMessageReceivedEventArgs)
                            {
                                if (device.SmsMessageReceived != null)
                                    device.SmsMessageReceived(device,
                                        (SmsMessageReceivedEventArgs) eventForDispatch.EventArgs);
                            }
                            else if (eventForDispatch.EventArgs is HttpResponseEventArgs)
                            {
                                if (device.HttpResponseReceived!= null)
                                    device.HttpResponseReceived(device, (HttpResponseEventArgs)eventForDispatch.EventArgs);
                            }
                        }
                    } while (item != null);
                    _eventEnqueued.WaitOne();
                }
                catch (Exception exc)
                {
                    // yes, catch everything - this thread has to keep plugging on
                    Dbg("An exception has occurred while dispatching events : " + exc);
                }
            }
        }

        private byte[] ReadExistingBinary()
        {
            int arraySize = _port.BytesToRead;

            byte[] received = new byte[arraySize];

            _port.Read(received, 0, arraySize);

            return received;
        }

        /// <summary>
        /// Reads all immediately available bytes, based on the encoding, in both the stream and the input buffer of the SerialPort object.
        /// </summary>
        /// <returns>String</returns>
        private string ReadExisting()
        {
            try
            {
                return new string(Encoding.UTF8.GetChars(this.ReadExistingBinary()));
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private void DiscardBufferedInput()
        {
            lock (_responseQueueLock)
            {
                _port.DiscardInBuffer();
                _responseQueue.Clear();
                _buffer = "";
                _responseReceived.Reset();
            }
        }

        private void Write(string txt)
        {
            _port.Write(Encoding.UTF8.GetBytes(txt), 0, txt.Length);
        }

        private void WriteLine(string txt)
        {
#if VERBOSE
            Dbg("Sent: " + txt);
#endif
            this.Write(txt + "\r\n");
        }

        private string Unquote(string quotedString)
        {
            quotedString = quotedString.Trim();
            var quoteChar = quotedString[0];
            if (quoteChar!='\'' && quoteChar!='"')
                return quotedString;
            if (quotedString.LastIndexOf(quoteChar) != quotedString.Length - 1)
                return quotedString;
            quotedString = quotedString.Substring(1);
            quotedString = quotedString.Substring(0, quotedString.Length - 1);
            return /* the now unquoted */ quotedString;
        }

        #endregion

        [Conditional("DEBUG")]
        private static void Dbg(string msg)
        {
#if MF_FRAMEWORK
            Debug.Print(msg);
#else
            Debug.WriteLine(msg);
#endif
        }
    }
}
