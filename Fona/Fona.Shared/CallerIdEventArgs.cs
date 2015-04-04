using System;
#if MF_FRAMEWORK
using Microsoft.SPOT;
#endif

namespace Molarity.Hardare.AdafruitFona
{
    /// <summary>
    /// The type of phone number
    /// </summary>
    public enum AddressType
    {
        /// <summary>
        /// The phone number type is not known
        /// </summary>
        Unknown = 129,
        /// <summary>
        /// A national phone number (does not include country information)
        /// </summary>
        National = 161,
        /// <summary>
        /// An international phone number, which includes complete country dialing information.
        /// </summary>
        International = 145,
        /// <summary>
        /// A network-specific phone number which will only work within the currently registered network.
        /// </summary>
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
