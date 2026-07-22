using Battlevest.Data;
using ECommons.Configuration;
using ECommons.EzIpcManager;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Battlevest.Services;

public class IPCProvider
{
    private IPCProvider()
    {
        EzIPC.Init(this);
    }

    [EzIPC]
    bool IsEnabled()
    {
        return S.Core.Enabled;
    }

    [EzIPC]
    void BeginPlanFromString(string str)
    {
        PluginLog.Information($"Starting from IPC: {str}");
        var plan = EzConfig.DefaultSerializationFactory.Deserialize<LevePlan>(str);
        if(plan == null)
        {
            PluginLog.Error("Plan could not be read");
            return;
        }
        if(!plan.CanStart())
        {
            PluginLog.Error("Specified plan could not be started.");
            return;
        }
        if(plan.LeveList.Count == 0)
        {
            PluginLog.Error("Specified plan contains no leves.");
            return;
        }
        S.Core.Selected = plan;
        S.Core.Enabled = true;
        if(plan.SingleUse)
        {
            S.Core.StopNext = true;
        }
    }
}
