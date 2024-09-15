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
}
