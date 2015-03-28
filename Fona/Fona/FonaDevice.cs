using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// FonaDevice implementation for .net (no GPIO)

namespace Molarity.Hardare.AdafruitFona
{
    public partial class FonaDevice
    {
        private void DoHardwareReset()
        {
            // No GPIO, so no hardware reset
        }

        private bool HardwareRingIndicationEnabled
        {
            get { return false; }
        }
    }
}
