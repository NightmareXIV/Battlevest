using ECommons.Automation.NeoTaskManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlevest.Services;
public class TaskManager : ECommons.Automation.NeoTaskManager.TaskManager
{
    private TaskManager() : base(new(showDebug:true, abortOnTimeout:true))
    {
    }
}
