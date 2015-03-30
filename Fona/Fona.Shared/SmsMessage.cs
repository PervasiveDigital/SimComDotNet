using System;
#if MF_FRAMEWORK
using Microsoft.SPOT;
#endif

namespace Molarity.Hardare.AdafruitFona
{
    public enum SmsMessageStatus
    {
        Unknown,
        ReceivedUnread,
        ReceivedRead,
        StoredUnsent,
        StoredSent,
        All
    }

    /// <summary>
    /// An SMS Message
    /// </summary>
    public class SmsMessage
    {
        public SmsMessage(SmsMessageStatus status, string senderPhoneNumber, AddressType addrType, DateTime timestamp, string body)
        {
            this.Status = status;
            this.Number = senderPhoneNumber;
            this.Timestamp = timestamp;
            this.AddressType = addrType;
            this.Body = body;
        }

        public SmsMessageStatus Status { get; private set; }

        public string Number { get; private set; }

        public AddressType AddressType { get; private set; }

        public DateTime Timestamp { get; private set; }

        public string Body { get; private set; }

        public static SmsMessageStatus ParseMessageStatus(string statusName)
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
    }
}
