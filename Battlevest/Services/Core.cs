using Battlevest.Data;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Automation;
using ECommons.Automation.NeoTaskManager.Tasks;
using ECommons.EzEventManager;
using ECommons.EzSharedDataManager;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using Action = System.Action;

namespace Battlevest.Services;
public unsafe class Core : IDisposable
{
    public bool Enabled = false;
    public LevePlan Selected = null;
    public bool StopNext = false;
    private Core()
    {
        Svc.Toasts.ErrorToast += Toasts_ErrorToast;
        new EzFrameworkUpdate(OnUpdate);
    }

    private void Toasts_ErrorToast(ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled)
    {
        if(!Enabled) return;
        if(AgentMap.Instance()->IsPlayerMoving == 1) return;
        if(message.ExtractText().EqualsIgnoreCase(Svc.Data.GetExcelSheet<LogMessage>().GetRow(562).Text.ExtractText()))
        {
            var fm = $"ForcedMelee_{Svc.Targets.Target?.EntityId}";
            var ignore = $"Ignore_{Svc.Targets.Target?.EntityId}";
            if(EzThrottler.Throttle(fm, 2000))
            {
                DuoLog.Warning($"No LoS on {Svc.Targets.Target}, force melee range");
            }
            else
            {
                EzThrottler.Throttle(ignore, 10000, true);
                DuoLog.Warning($"No LoS on {Svc.Targets.Target} in melee range, temporarily ignoring target");
            }
        }
    }

    private ExternalTerritoryConfig ExternalTerritoryConfig = new()
    {
        EnableAutoInteract = false,
        EnableTalkSkip = true,
        EnableQuestComplete = true,
    };
    private bool ExternalControl = false;

    public void RelinquishExternalControl()
    {
        ExternalControl = false;
        S.TextAdvanceIPC.DisableExternalControl(Svc.PluginInterface.InternalName);
        if(EzSharedData.TryGet<HashSet<string>>("YesAlready.StopRequests", out var sr))
        {
            sr.Remove(Svc.PluginInterface.InternalName);
        }
    }

    public void OnUpdate()
    {
        if(!Enabled)
        {
            if(ExternalControl)
            {
                RelinquishExternalControl();
            }
            return;
        }
        ExternalControl = true;
        if(!S.TextAdvanceIPC.IsInExternalControl())
        {
            S.TextAdvanceIPC.EnableExternalControl(Svc.PluginInterface.InternalName, ExternalTerritoryConfig);
        }
        if(EzSharedData.TryGet<HashSet<string>>("YesAlready.StopRequests", out var sr))
        {
            sr.Add(Svc.PluginInterface.InternalName);
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
        Utils.HandleTrade();
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
                    Utils.HandleCombat(npc() != null && Player.DistanceTo(npc()) < 10f, Selected);
                }
            }
            else
            {
                if (C.UseBossMod) S.BossModIPC.ClearActive();
                S.TextAdvanceIPC.Stop();
                var currentLeves = QuestManager.Instance()->LeveQuests.ToArray().Where(x => x.Flags == 0 && Selected.LeveList.Contains(x.LeveId)).OrderBy(x => Utils.GetDistanceToLeve(x.LeveId));
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
                        if(StopNext)
                        {
                            StopNext = false;
                            S.Core.Enabled = false;
                            return;
                        }
                        S.TaskManager.Enqueue(() =>
                        {
                            S.TextAdvanceIPC.Stop();
                            S.NavmeshIPC.Reload();
                        });
                        S.TaskManager.Enqueue(() => S.NavmeshIPC.IsReady(), new(timeLimitMS: 5 * 60 * 1000));
                        S.TaskManager.EnqueueTask(NeoTasks.ApproachObjectViaAutomove(npc, 6f));
                        S.TaskManager.EnqueueTask(NeoTasks.InteractWithObject(npc));
                        S.TaskManager.Enqueue(Utils.SelectBattleLeve);

                        S.TaskManager.Enqueue(Utils.RecursivelyAcceptLeves);

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

    public void Dispose()
    {
        Svc.Toasts.ErrorToast -= Toasts_ErrorToast;
    }
}
