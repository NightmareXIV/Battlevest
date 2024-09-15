using Battlevest.Data;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Automation;
using ECommons.Automation.NeoTaskManager.Tasks;
using ECommons.EzEventManager;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using Action = System.Action;

namespace Battlevest.Services;
public unsafe class Core
{
    public bool Enabled = false;
    public LevePlan Selected = null;
    private Core()
    {
        new EzFrameworkUpdate(OnUpdate);
    }

    ExternalTerritoryConfig ExternalTerritoryConfig = new()
    {
        EnableAutoInteract = false,
        EnableTalkSkip = true,
    };
    bool ExternalControl = false;

    public void OnUpdate()
    {
        if(!Enabled)
        {
            if(ExternalControl)
            {
                ExternalControl = false;
                S.TextAdvanceIPC.DisableExternalControl(Svc.PluginInterface.InternalName);
            }
            return;
        }
        ExternalControl = true;
        if(!S.TextAdvanceIPC.IsInExternalControl())
        {
            S.TextAdvanceIPC.EnableExternalControl(Svc.PluginInterface.InternalName, ExternalTerritoryConfig);
        }
        if(!IsScreenReady())
        {
            S.TextAdvanceIPC.Stop();
            return;
        }
        if(Svc.Condition[ConditionFlag.Occupied33])
        {
            S.TextAdvanceIPC.Stop();
        }
        Utils.HandleYesno();
        if(Selected != null && Player.Interactable && !IsOccupied() && Selected.Territory == Player.Territory && EzThrottler.Check("Wait") && !S.TaskManager.IsBusy)
        {
            IGameObject npc() => Svc.Objects.OrderBy(x => Player.DistanceTo(x.Position)).FirstOrDefault(x => x.DataId == Selected.NpcDataID);
            if(Svc.Condition[ConditionFlag.BoundByDuty])
            {
                if(Svc.Condition[ConditionFlag.Occupied33])
                {
                    S.TextAdvanceIPC.Stop();
                }
                else
                {
                    Utils.HandleCombat(npc() != null && Player.DistanceTo(npc()) < 10f, out _);
                }
            }
            else
            {
                S.TextAdvanceIPC.Stop();
                var currentLeves = QuestManager.Instance()->LeveQuests.ToArray().Where(x => x.Flags == 0 && Selected.LeveList.Contains(x.LeveId));
                if(currentLeves.Any())
                {
                    Utils.Initiate(currentLeves.First().LeveId);
                    EzThrottler.Throttle("Wait", 2000, true);
                }
                else
                {
                    if(npc() != null)
                    {
                        EzThrottler.Throttle("Wait", 5000, true);
                        if(QuestManager.Instance()->NumLeveAllowances == 0)
                        {
                            DuoLog.Warning("No more leve allowances!");
                            S.Core.Enabled = false;
                            return;
                        }
                        S.TaskManager.EnqueueTask(NeoTasks.ApproachObjectViaAutomove(npc, 6f));
                        S.TaskManager.EnqueueTask(NeoTasks.InteractWithObject(npc));
                        S.TaskManager.Enqueue(Utils.SelectBattleLeve);
                        for(int i = 0; i < 10; i++)
                        {
                            S.TaskManager.Enqueue(() =>
                            {
                                var acceptableLeves = Selected.LeveList.ToDictionary(x => Svc.Data.GetExcelSheet<Leve>().GetRow(x).Name.ExtractText(), x => x);
                                if(TryGetAddonMaster<GuildLeve>("GuildLeve", out var m) && m.IsAddonReady)
                                {
                                    foreach(var l in m.Levequests)
                                    {
                                        if(acceptableLeves.TryGetValue(l.Name, out var result))
                                        {
                                            l.Select();
                                            return true;
                                        }
                                    }
                                }
                                return false;
                            });
                        }
                        S.TaskManager.Enqueue(() =>
                        {
                            if(TryGetAddonMaster<AddonMaster.JournalDetail>("JournalDetail", out var m))
                            {
                                if(EzThrottler.Throttle("Accept"))
                                {
                                    m.AcceptMap();
                                }
                                return true;
                            }
                            return false;
                        });
                        S.TaskManager.Enqueue(() =>
                        {
                            if(!IsOccupied()) return true;
                            if(TryGetAddonByName<AtkUnitBase>("GuildLeve", out var addon) && IsAddonReady(addon))
                            {
                                if(EzThrottler.Throttle("CloseGuildLeve")) Callback.Fire(addon, true, -1);
                            }
                            if(TryGetAddonMaster<AddonMaster.SelectString>("SelectString", out var m) && m.IsAddonReady)
                            {
                                m.Addon->Close(true);
                            }
                            return false;
                        });
                        S.TaskManager.Enqueue((Action)(() => EzThrottler.Throttle("Wait", 1000, true)));
                    }
                }
            }
        }
    }
}
