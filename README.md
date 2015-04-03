# .Net Support for Adafruit Fona

This project brings support for the Adafruit Fona GSM/GPRS breakout board to .net.
Nuget packages are available for .Net on Windows, .Net (mono) on Linux and Raspberry Pi,
and the .Net Micro Framework on pretty much any platform that can run the .Net Micro Framework

The best way to use this project is to install the correct nuget package in your solution or project file.

Install **Molarity.Hardware.AdafruitFona-NETMF** for use with the .Net Micro Framework.

Install **Molarity.Hardware.AdafruitFona-Win** for use with .Net languages on Windows systems and Linux
(mono) systems that do not support GPIO.

Install **Molarity.Hardware.AdafruitFona-RPi** for use with .Net (mono) on Linux on Raspberry Pi systems.

This project is still in 'pre-release', so when searching for the nuget packages be sure to either select
'Include Prerelease' in the ui, or use the -Pre flag from the Package Manager command line.

Documentation will be provided on the github wiki here : https://github.com/martincalsyn/Fona.net/wiki

## A Note on GPIO support
Currently, only the .Net Micro Framework version (Molarity.Hardware.AdafruitFona-NETMF) supports control
of the Fona board via GPIO (General Purpose I/O, or digital input and output pins). We will be adding 
Raspberry Pi GPIO support shortly, and will add Windows 10 IoT GPIO support when it becomes available.

GPIO lines are completely optional, but do improve your control over the board. In particular, you
need GPIO lines in order to do a hardware reset or to power the board on or off. GPIO can also be
used to enable hardware interrupt based notification of incoming calls, although software support
for this is also implemented.  Without GPIO, you will still be able to make and receive calls and texts
and use GPRS (data) features.
