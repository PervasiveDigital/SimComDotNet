using System;
#if MF_FRAMEWORK
using Microsoft.SPOT;
#endif

namespace Molarity.Hardare.AdafruitFona
{
    /// <summary>
    /// The type of caller id number presented
    /// </summary>
    public enum AddressType
    {
        Unknown = 129,
        National = 161,
        International = 145,
        NetworkSpecific = 177
    }

    /// <summary>
    /// Information related to an incoming call
    /// </summary>
    public class CallerIdEventArgs : EventArgs
    {
        /// <summary>
        /// Initialize a CallerIdEventArgs object
        /// </summary>
        /// <param name="number">The number presented by the caller id information from the network</param>
        /// <param name="addrType">A code that describes the type of phone number represented by the Number member</param>
        public CallerIdEventArgs(string number, AddressType addrType)
        {
            this.Number = number;
            this.AddressType = addrType;
        }

        /// <summary>
        /// The incoming phone number
        /// </summary>
        public string Number { get; private set; }
        /// <summary>
        /// The type of number represented by the incoming phone number
        /// </summary>
        public AddressType AddressType { get; private set; }
    }
}
