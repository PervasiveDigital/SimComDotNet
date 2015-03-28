using System;

namespace Molarity.Hardare.AdafruitFona
{
    public abstract class FonaException : Exception
    {
        protected FonaException(string message) : base(message)
        {
        }

        protected FonaException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    public class FonaCommandException : Exception
    {
        private readonly string _command;
        private readonly string _expected;
        private readonly string _actual;

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

        public string Command { get { return _command; } }
        public string Expected { get { return _expected; } }
        public string Actual{ get { return _actual; } }
    }

    public class FonaExpectException : Exception
    {
        private readonly string _expected;
        private readonly string _actual;

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

        public string Expected { get { return _expected; } }
        public string Actual { get { return _actual; } }
    }

    public class FonaCommandTimeout : Exception
    {
        public FonaCommandTimeout() : base("Timed out while waiting for a response from the Fona device")
        {
        }
    }
}
