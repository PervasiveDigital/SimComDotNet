using System;
using System.Collections;
using System.Text;

namespace Molarity.Hardware
{
    /// <summary>
    /// Extends the .NET Micro Framework SerialPort Class with additional methods from the Full .NET Framework SerialPort Class
    /// as well as other useful methods.
    /// </summary>
    public class AugmentedSerialPort : System.IO.Ports.SerialPort
    {
        // CONSTRUCTORS -- Pass the Buck
        public AugmentedSerialPort(string portName, int baudRate, System.IO.Ports.Parity parity, int dataBits, System.IO.Ports.StopBits stopBits)
            : base(portName, baudRate, parity, dataBits, stopBits) { }
        public AugmentedSerialPort(string portName, int baudRate, System.IO.Ports.Parity parity, int dataBits)
            : base(portName, baudRate, parity, dataBits) { }
        public AugmentedSerialPort(string portName, int baudRate, System.IO.Ports.Parity parity)
            : base(portName, baudRate, parity) { }
        public AugmentedSerialPort(string portName, int baudRate)
            : base(portName, baudRate) { }
        public AugmentedSerialPort(string portName)
            : base(portName) { }

        /// <summary>
        /// Writes the specified string to the serial port.
        /// </summary>
        /// <param name="txt"></param>
        public void Write(string txt)
        {
            base.Write(Encoding.UTF8.GetBytes(txt), 0, txt.Length);
        }

        /// <summary>
        /// Writes the specified string and the NewLine value to the output buffer.
        /// </summary>
        /// <param name="txt"></param>
        public void WriteLine(string txt)
        {
            this.Write(txt + this.NewLine);
        }

        /// <summary>
        /// Reads all immediately available bytes, as binary data, in both the stream and the input buffer of the SerialPort object.
        /// </summary>
        /// <returns>byte[]</returns>
        public byte[] ReadExistingBinary()
        {
            int arraySize = this.BytesToRead;

            byte[] received = new byte[arraySize];

            this.Read(received, 0, arraySize);

            return received;
        }

        /// <summary>
        /// Reads all immediately available bytes, based on the encoding, in both the stream and the input buffer of the SerialPort object.
        /// </summary>
        /// <returns>String</returns>
        public string ReadExisting()
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

        /// <summary>
        /// Opens a new serial port connection.
        /// </summary>
        public new void Open()
        {
            this._remainder = string.Empty;  // clear the remainder so it doesn't get mixed with data from the new session
            base.Open();
        }

        /// <summary>
        /// Stores any incomplete message that hasn't yet been terminated with a delimiter.
        /// This will be concatenated with new data from the next DataReceived event to (hopefully) form a complete message. 
        /// This property is only populated after the Deserialize() method has been called.
        /// </summary>
        private string _remainder = string.Empty;
        public string Remainder
        {
            get { return this._remainder; }
        }

        /// <summary>
        /// Splits data from a serial buffer into separate messages, provided that each message is delimited by one or more end-of-line character(s).
        /// </summary>
        /// <param name="delimiter">Character sequence that terminates a message line.</param>
        /// <returns>
        /// An array of strings whose items correspond to individual messages, without the delimiters.
        /// Only complete, properly terminated messages are included. Incomplete message fragments are saved to be appended to
        /// the next received data.
        /// 
        /// If no complete messages are found in the serial buffer, the output array will be empty with Length = 0.
        /// </returns>
        private string[] Deserialize(string delimiter)
        {
            string receivedData = string.Concat(_remainder, this.ReadExisting());   // attach the previous remainder to the new data
            return SplitString(receivedData, out _remainder, delimiter);  // return itemized messages and store remainder for next pass
        }

        private string[] Deserialize()
        {
            return Deserialize(this.NewLine);
        }

        private readonly ArrayList _pendingLines = new ArrayList();
        public string ReadLine()
        {
            string result = null;

            do
            {
                if (_pendingLines.Count == 0)
                {
                    var newLines = Deserialize();
                    foreach (var line in newLines)
                    {
                        _pendingLines.Add(line);
                    }
                }
                if (_pendingLines.Count > 0)
                {
                    result = (string)_pendingLines[0];
                    _pendingLines.RemoveAt(0);
                }
            } while (result == null);

            return result;
        }

        public void DiscardBufferedInput()
        {
            _remainder = string.Empty;
            _pendingLines.Clear();
            this.DiscardInBuffer();
        }

        /// <summary>
        /// Splits a stream into separate lines, given a delimiter.
        /// </summary>
        /// <param name="input">
        /// The string that will be deserialized.
        /// 
        /// Example:
        /// Assume a device transmits serial messages, and each message is separated by \r\n (carriage return + line feed).
        /// 
        /// For illustration, picture the following output from such a device:
        /// First message.\r\n
        /// Second message.\r\n
        /// Third message.\r\n
        /// Fourth message.\r\n
        /// 
        /// Once a SerialPort object receives the first bytes, the DataReceived event will be fired,
        /// and the interrupt handler may read a string from the serial buffer like so:
        /// "First message.\r\nSecond message.\r\nThird message.\r\nFourth me"
        /// 
        /// The message above has been cut off to simulate the DataReceived event being fired before the sender has finished 
        /// transmitting all messages (the "ssage.\r\n" characters have not yet traveled down the wire, so to speak).
        /// At the moment the DataReceived event is fired, the interrupt handler only has access to the (truncated) 
        /// input message above.
        /// 
        /// In this example, the string from the serial buffer will be the input to this method.
        /// </param>
        /// <param name="remainder">
        /// Any incomplete messages that have not yet been properly terminated will be returned via this output parameter.
        /// In the above example, this parameter will return "Fourth me". Ideally, this output parameter will be appended to the next
        /// transmission to reconstruct the next complete message.
        /// </param>
        /// <param name="delimiter">
        /// A string specifying the delimiter between messages. 
        /// If omitted, this defaults to "\r\n" (carriage return + line feed).
        /// </param>
        /// <param name="includeDelimiterInOutput">
        /// Determines whether each item in the output array will include the specified delimiter.
        /// If True, the delimiter will be included at the end of each string in the output array.
        /// If False (default), the delimiter will be excluded from the output strings.
        /// </param>
        /// <returns>
        /// string[]
        /// Every item in this string array will be an individual, complete message. The first element
        /// in the array corresponds to the first message, and so forth. The length of the array will be equal to the number of
        /// complete messages extracted from the input string.
        /// 
        /// From the above example, if includeDelimiterInOutput == True, this output will be:
        /// output[0] = "First message.\r\n"
        /// output[1] = "Second message.\r\n"
        /// output[2] = "Third message.\r\n"
        /// 
        /// If no complete messages have been received, the output array will be empty with Length = 0.
        /// </returns>
        private static string[] SplitString(string input, out string remainder, string delimiter = "\r\n", bool includeDelimiterInOutput = false)
        {
            string[] prelimOutput = input.Split(delimiter.ToCharArray());

            // Check last element of prelimOutput to determine if it was a delimiter.
            // We know that the last element was a delimiter if the string.Split() method makes it empty.
            if (prelimOutput[prelimOutput.Length - 1] == string.Empty)
                remainder = string.Empty;   // input string terminated in a delimiter, so there is no remainder
            else
            {
                remainder = prelimOutput[prelimOutput.Length - 1];  // store the remainder
                prelimOutput[prelimOutput.Length - 1] = string.Empty;   // remove the remainder string from prelimOutput to avoid redundancy
            }

            if (includeDelimiterInOutput == true)
                return ScrubStringArray(prelimOutput, removeString: string.Empty, delimiter: delimiter);
            else
                return ScrubStringArray(prelimOutput, removeString: string.Empty, delimiter: string.Empty);
        }

        /// <summary>
        /// Removes items in an input array that are equal to a specified string.
        /// </summary>
        /// <param name="input">String array to scrub.</param>
        /// <param name="removeString">String whose occurrences will be removed if an item consists of it. Default: string.Empty.</param>
        /// <param name="delimiter">
        /// Delimiter that will be appended to the end of each element in the output array. Default: \r\n (carriage return + line feed).
        /// To omit delimiters from the end of each message, set this parameter to string.Empty.
        /// </param>
        /// <returns>
        /// String array containing only desired strings. The length of this output will likely be shorter than the input array.
        /// </returns>
        private static string[] ScrubStringArray(string[] input, string removeString = "", string delimiter = "\r\n")
        {
            // Note: I originally wanted to use a System.Collections.ArrayList object here and then run the ToArray() method,
            // but the compiler throws runtime exceptions for some reason, so I've resorted to this manual array-copying approach instead.

            int numOutputElements = 0;

            // Determine the bounds of the output array by looking for input elements that meet inclusion criterion
            for (int k = 0; k < input.Length; k++)
            {
                if (input[k] != removeString)
                    numOutputElements++;
            }

            // Declare and populate output array
            string[] output = new string[numOutputElements];

            int m = 0;  // output index
            for (int k = 0; k < input.Length; k++)
            {
                if (input[k] != removeString)
                {
                    output[m] = input[k] + delimiter;
                    m++;
                }
            }

            return output;
        }

        private string _newline = "\r\n";
        public string NewLine { get { return _newline; } set { _newline = value; } }

    }
}
