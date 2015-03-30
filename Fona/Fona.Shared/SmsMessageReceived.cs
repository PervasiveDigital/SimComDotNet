using System;
#if MF_FRAMEWORK
using Microsoft.SPOT;
#endif

namespace Molarity.Hardare.AdafruitFona
{
    /// <summary>
    /// A code indicating where SMS messages are stored
    /// </summary>
    public enum SmsStorage
    {
        /// <summary>
        /// The storage location is unknown
        /// </summary>
        Unknown,
        /// <summary>
        /// The message is stored on the sim
        /// </summary>
        SimMessageStorage,
        /// <summary>
        /// The message is stored in phone memory
        /// </summary>
        PhoneMessageStorage,
        /// <summary>
        /// Storage to the SIM will be preferred
        /// </summary>
        SimMessageStoragePreferred,
        /// <summary>
        /// Storage to the phone will be preferred
        /// </summary>
        PhoneMessageStoragePreferred,
        /// <summary>
        /// Storage to the SIM or phone is possible (though the sim is preferred)
        /// </summary>
        SimOrPhoneStorage
    }


    /// <summary>
    /// Event arguments describing an incoming text
    /// </summary>
    public class SmsMessageReceivedEventArgs : EventArgs
    {
        private FonaDevice _device;

        /// <summary>
        /// Initialize the event arguments describing an incoming text
        /// </summary>
        /// <param name="storageCode">Code indicating where the message was stored</param>
        /// <param name="newMessageIndex">The storage index of the received message</param>
        public SmsMessageReceivedEventArgs(FonaDevice device, string storageCode, int newMessageIndex)
        {
            _device = device;

            switch (storageCode)
            {
                case "SM":
                    this.Storage = SmsStorage.SimMessageStorage;
                    break;
                case "ME":
                    this.Storage = SmsStorage.PhoneMessageStorage;
                    break;
                case "SM_P:":
                    this.Storage = SmsStorage.SimMessageStoragePreferred;
                    break;
                case "ME_P":
                    this.Storage = SmsStorage.PhoneMessageStoragePreferred;
                    break;
                case "MT":
                    this.Storage = SmsStorage.SimOrPhoneStorage;
                    break;
                default:
                    this.Storage = SmsStorage.Unknown;
                    break;
            }
            this.MessageIndex = newMessageIndex;
        }

        /// <summary>
        /// The index where the new message is stored.  You can use this to retrieve the text of the message.
        /// </summary>
        public int MessageIndex { get; private set; }

        /// <summary>
        /// Indicates where this message was stored
        /// </summary>
        public SmsStorage Storage { get; private set; }
    }
}
