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
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: fonatest COMX (for example: fonatest COM1)");
                return;
            }

            var port = new SerialPort(args[0], 9600, Parity.None, 8, StopBits.One);
            var fona = new FonaDevice(port);

            fona.Reset();

            bool done = false;
            do
            {
                Console.WriteLine();
                Console.Write("Fona test> ");
                var command = Console.ReadLine();
                var tokens = command.Split(new[] {' ', '\t'});
                if (tokens.Length > 0)
                {
                    try
                    {
                        switch (tokens[0].ToLower())
                        {
                            case "?":
                            case "help":
                            case "h":
                                ShowHelp();
                                break;
                            case "quit":
                            case "q":
                                done = true;
                                break;
                            case "reset":
                                fona.Reset();
                                Console.WriteLine("hard reset completed");
                                break;
                            case "getsimccid":
                                Console.WriteLine("SIM ccid : " + fona.GetSimCcid());
                                break;
                            case "getimei":
                                Console.WriteLine("IMEI : " + fona.GetImei());
                                break;
                            case "unlock":
                                string code;
                                if (tokens.Length > 1)
                                {
                                    code = tokens[1];
                                }
                                else
                                {
                                    Console.Write("unlock code:");
                                    code = Console.ReadLine();
                                }
                                if (!string.IsNullOrEmpty(code))
                                    fona.UnlockSim(code);
                                break;
                            default:
                                Console.WriteLine("Unrecognized command. Type 'help' for a list of commands.");
                                break;
                        }
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine(exc);
                    }
                }

            } while (!done);
        }

        private static void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine("Valid commands: (all commands are case-insensitive)");
            Console.WriteLine("\tGetSimCcid");
            Console.WriteLine("\tGetIMEI");
            Console.WriteLine("\treset");
            Console.WriteLine("\tunlock");
            Console.WriteLine("\tquit");
            Console.WriteLine();
        }
    }
}
