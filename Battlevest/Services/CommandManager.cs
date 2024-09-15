using ECommons.SimpleGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlevest.Services;
public class CommandManager
{
    private CommandManager()
    {
        EzCmd.Add("/battlevest", EzConfigGui.Open, "Open configuration");
    }
}
