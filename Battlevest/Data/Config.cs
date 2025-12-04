using ECommons.Configuration;
using ECommons.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Battlevest.Data;
public class Config : IEzConfig
{
    public List<LevePlan> Plans = [];
    public LimitedKeys Key = LimitedKeys.Digit_1;
    public int StopAt = 0;
    public bool AllowMultiple = true;
    public bool AllowFlight = false;
    public bool EnableKeySpam = true;
    public (int Hotbar, int Slot) HotbarSlot = (0, 0);
    public bool UseKeyMode = true;
    public bool UseBossMod = false;
    public bool UseRSR = false;
}
