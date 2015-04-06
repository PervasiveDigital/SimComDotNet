using System;
#if MF_FRAMEWORK
using Microsoft.SPOT;
#endif

namespace Molarity.Hardare.AdafruitFona
{
    /// <summary>
    /// A status code defining the origin and status of a given SMS message.
    /// An SMS message can be associated with one status (not including All).
    /// You can use combinations of flags to filter messages for retrieval.
    /// </summary>
    [Flags]
    public enum SmsMessageStatus : byte
    {
        /// <summary>
        /// The status of the message is not known
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// The message was received and has not been read
        /// </summary>
        ReceivedUnread = 1,
        /// <summary>
        /// The message was received and has been read
        /// </summary>
        ReceivedRead = 2,
        /// <summary>
        /// The message was composed and stored but has not been sent
        /// </summary>
        StoredUnsent = 4,
        /// <summary>
        /// The message was composed, stored and has been sent
        /// </summary>
        StoredSent = 8,
        /// <summary>
        /// All valid statuses - used only to filter messages for retrieval. This is not
        /// a valid status for a single message.
        /// </summary>
        All = 0xf
    }

    /// <summary>
    /// An SMS Message
    /// </summary>
    public class SmsMessage
    {
        internal SmsMessage(int index, SmsMessageStatus status, string senderPhoneNumber, 
            AddressType addrType, DateTime timestamp, string body)
        {
            this.Index = index;
            this.Status = status;
            this.Number = senderPhoneNumber;
            this.Timestamp = timestamp;
            this.AddressType = addrType;
            this.Body = body;
        }

        /// <summary>
        /// The index of this message in the private store. This can be used for accessing or deleting the message.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// The status and origin of this message
        /// </summary>
        public SmsMessageStatus Status { get; private set; }

        /// <summary>
        /// The phone number of the message sender
        /// </summary>
        public string Number { get; private set; }

        /// <summary>
        /// The type of phone number presented in the 'Number' member
        /// </summary>
        public AddressType AddressType { get; private set; }

        /// <summary>
        /// The time reported by the Fona for this message
        /// </summary>
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// The textual body of the message. May contain multiple lines.
        /// </summary>
        public string Body { get; private set; }

        internal static SmsMessageStatus ParseMessageStatus(string statusName)
        {
            switch (statusName)
            {
                case "REC UNREAD":
                    return SmsMessageStatus.ReceivedUnread;
                case "REC READ":
                    return SmsMessageStatus.ReceivedRead;
                case "STO UNSENT":
                    return SmsMessageStatus.StoredUnsent;
                case "STO SENT":
                    return SmsMessageStatus.StoredSent;
                case "ALL":
                    return SmsMessageStatus.All;
                default:
                    return SmsMessageStatus.Unknown;
            }
        }

        internal static string ConvertToStatString(SmsMessageStatus status)
        {
            switch (status)
            {
                case SmsMessageStatus.ReceivedUnread:
                    return "REC UNREAD";
                case SmsMessageStatus.ReceivedRead:
                    return "REC READ";
                case SmsMessageStatus.StoredUnsent:
                    return "STO UNSENT";
                case SmsMessageStatus.StoredSent:
                    return "STO SENT";
                case SmsMessageStatus.All:
                    return "ALL";
                default:
                    return "";
            }
        }
    }
}
