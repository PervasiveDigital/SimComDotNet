using System;

namespace Molarity.Hardare.AdafruitFona
{
    /// <summary>
    /// Used to single an exception in the FonaDevice class
    /// </summary>
    public class FonaException : Exception
    {
        /// <summary>
        /// Initialize a Fona exception
        /// </summary>
        /// <param name="message">Message indicating the reason for the exception</param>
        public FonaException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initialize a Fona exception
        /// </summary>
        /// <param name="message">A message describing the exception</param>
        /// <param name="inner">An inner exception that probably led to this exception being thrown</param>
        public FonaException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    /// <summary>
    /// An exception ocurred while sending or receiving a command to/from the Fona device
    /// </summary>
    public class FonaCommandException : FonaException
    {
        private readonly string _command;
        private readonly string _expected;
        private readonly string _actual;

        /// <summary>
        /// Initialize a Fona command exception
        /// </summary>
        /// <param name="command">The command that we were trying to send</param>
        /// <param name="expected">The expected response</param>
        /// <param name="actual">The actual response received (probably an error code)</param>
        public FonaCommandException(string command, string expected, string actual)
#if MF_FRAMEWORK
            : base("Unexpected response to a command")
#else
            : base(string.Format("Command {0} expected {1} but received {2}", command, expected, actual))
#endif
        {
            _command = command;
            _expected = expected;
            _actual = actual;
        }

        /// <summary>
        /// The command that was sent
        /// </summary>
        public string Command { get { return _command; } }

        /// <summary>
        /// The expected response to the command
        /// </summary>
        public string Expected { get { return _expected; } }

        /// <summary>
        /// The actual response received
        /// </summary>
        public string Actual{ get { return _actual; } }
    }

    /// <summary>
    /// An unexpected string (probably an error) was returned from the Fona device in response to a command.
    /// </summary>
    public class FonaExpectException : FonaException
    {
        private readonly string _expected;
        private readonly string _actual;

        /// <summary>
        /// Initialize an 'expect' exception. This exception indicates that an unexpected response was received.
        /// </summary>
        /// <param name="expected">The expected response</param>
        /// <param name="actual">The actual response received</param>
        public FonaExpectException(string expected, string actual)
#if MF_FRAMEWORK
            : base("Unexpected response to a command")
#else
            : base(string.Format("Expected {0} but received {1}", expected, actual))
#endif
        {
            _expected = expected;
            _actual = actual;
        }

        /// <summary>
        /// The expected response
        /// </summary>
        public string Expected { get { return _expected; } }

        /// <summary>
        /// The actual response received
        /// </summary>
        public string Actual { get { return _actual; } }
    }

    /// <summary>
    /// A timeout ocurred while waiting for a response from the Fona device.
    /// </summary>
    public class FonaCommandTimeout : Exception
    {
        /// <summary>
        /// Initialize a timeout exception, which indicates that a timeout ocurred while we 
        /// were waiting for a response from the Fona device.
        /// </summary>
        public FonaCommandTimeout() : base("Timed out while waiting for a response from the Fona device")
        {
        }
    }
}
