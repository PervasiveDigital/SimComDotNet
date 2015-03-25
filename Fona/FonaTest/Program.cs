using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Molarity.Hardare.AdafruitFona;

namespace FonaTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var port = new SerialPort("COM5"/*args[0]*/, 9600, Parity.None, 8, StopBits.One);
            var fona = new FonaDevice(port);

            fona.Reset();

            var ccid = fona.GetSimCcid();
            Console.WriteLine(ccid);
        }
    }
}
