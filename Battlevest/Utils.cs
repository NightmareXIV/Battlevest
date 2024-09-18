using Battlevest.Data;
using Battlevest.Sheets;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Memory;
using ECommons.Automation;
using ECommons.Automation.UIInput;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Interop;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;

namespace Battlevest;
public unsafe static class Utils
{
    public static void Stop()
    {
        S.Core.Enabled = false;
        S.TaskManager.Abort();
        S.TextAdvanceIPC.Stop();
    }

    public static bool SelectBattleLeve()
    {
        if(TryGetAddonMaster<AddonMaster.SelectString>("SelectString", out var m) && m.IsAddonReady)
        {
            foreach(var x in m.Entries)
            {
                if(x.Text.EqualsAny(Svc.Data.GetExcelSheet<Leve_GuildLeveAssignment>().GetRow(13).Value.ExtractText()))
                {
                    if(EzThrottler.Throttle("HandleSelectString"))
                    {
                        x.Select();
                        return false;
                    }
                    return false;
                }
            }
            foreach(var x in m.Entries)
            {
                if(x.Text.EqualsAny(Svc.Data.GetExcelSheet<Leve_GuildLeveAssignment>().GetRow(1).Value.ExtractText(), Svc.Data.GetExcelSheet<Leve_GuildLeveAssignment>().GetRow(9).Value.ExtractText(), Svc.Data.GetExcelSheet<Leve_GuildLeveAssignment>().GetRow(10).Value.ExtractText(), Svc.Data.GetExcelSheet<Leve_GuildLeveAssignment>().GetRow(11).Value.ExtractText()))
                {
                    if(EzThrottler.Throttle("HandleSelectString"))
                    {
                        x.Select();
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public static bool HandleYesno()
    {
        if(TryGetAddonMaster<AddonMaster.SelectYesno>("SelectYesno", out var m) && m.IsAddonReady)
        {
            var isLeveFinish = m.Text.ContainsAny(StringComparison.OrdinalIgnoreCase, Svc.Data.GetExcelSheet<Leve_LeveDirector>().GetRow(0).Value.ExtractText(true));
            if(isLeveFinish || m.Text.EqualsAny(Svc.Data.GetExcelSheet<Addon>().GetRow(608).Text.ExtractText()))
            {
                S.TextAdvanceIPC.Stop();
                if(EzThrottler.Throttle("YesNo"))
                {
                    var shouldNo = isLeveFinish && QuestManager.Instance()->LeveQuests.ToArray().Any(x => x.Flags == 0 && S.Core.Selected.LeveList.Contains(x.LeveId));
                    if(shouldNo)
                    {
                        m.No();
                    }
                    else
                    {
                        m.Yes();
                    }
                    return true;
                }
            }
        }
        return false;
    }

    public static void HandleCombat(bool forceMount, LevePlan plan)
    {
        if(IsOccupied() || Svc.Condition[ConditionFlag.Casting])
        {
            return;
        }
        var isMelee = Player.Object.GetRole().EqualsAny(CombatRole.Tank) || Player.Job.GetUpgradedJob().EqualsAny(Job.RPR, Job.VPR, Job.SAM, Job.DRG, Job.MNK, Job.NIN);
        var marks = AgentHUD.Instance()->MapMarkers.Where(x => x.IconId == 60492 && x.Radius < 50).OrderBy(x => Player.DistanceTo(new Vector3(x.X, x.Y, x.Z)));
        var validObjects = Svc.Objects.OfType<IBattleNpc>().Where(x => !plan.IgnoredMobs.Contains(x.NameId) && !x.IsDead && x.IsHostile() && x.Struct()->NamePlateIconId == 71244 && EzThrottler.Check($"Ignore_{x.EntityId}")).OrderBy(Player.DistanceTo);
        //forced mobs first
        var combatTarget = validObjects.FirstOrDefault(x => plan.ForcedMobs.Contains(x.NameId));
        //then engaged
        combatTarget ??= validObjects.FirstOrDefault(x => x.GetNameplateKind().EqualsAny(NameplateKind.HostileEngagedSelfUndamaged, NameplateKind.HostileEngagedSelfDamaged));
        //then the rest
        combatTarget ??= validObjects.FirstOrDefault();
        if(combatTarget != null && combatTarget.IsTargetable)
        {
            if(!EzThrottler.Check($"ForcedMelee_{combatTarget.EntityId}")) isMelee = true;
            var distance = isMelee ? 3f + (AgentMap.Instance()->IsPlayerMoving == 1 ? -1.5f : 0f) : 20f + (AgentMap.Instance()->IsPlayerMoving == 1 ? -5f : 0f);
            if(Player.DistanceTo(combatTarget) < distance && Math.Abs(Player.Position.Y - combatTarget.Position.Y + (AgentMap.Instance()->IsPlayerMoving == 1 ? -2f : 0f)) < 10)
            {
                if(Svc.Targets.Target != combatTarget) Svc.Targets.Target = combatTarget;
                S.TextAdvanceIPC.Stop();
                if(Svc.Condition[ConditionFlag.Mounted])
                {
                    if(EzThrottler.Throttle("Dismount", 1000))
                    {
                        Chat.Instance.ExecuteGeneralAction(23);
                    }
                }
                else
                {
                    if(!Player.IsAnimationLocked && AgentMap.Instance()->IsPlayerMoving == 0 && C.Key != LimitedKeys.None && EzThrottler.Throttle("Keypress"))
                    {
                        WindowsKeypress.SendKeypress(C.Key);
                    }
                }
                EzThrottler.Throttle("TAPath", 1000, true);
            }
            else
            {
                if(!S.TextAdvanceIPC.IsBusy() && EzThrottler.Throttle("TAPath"))
                {
                    var shouldMount = Player.DistanceTo(combatTarget) > 50f;
                    S.TextAdvanceIPC.EnqueueMoveTo3DPoint(new()
                    {
                        Mount = forceMount || shouldMount,
                        Fly = C.AllowFlight,
                        NoInteract = true,
                        Position = combatTarget.Position
                    }, 0.5f);
                }
            }
        }
        else
        {
            //move to last marker
            if(!S.TextAdvanceIPC.IsBusy() && marks.Any() && EzThrottler.Throttle("TAPath"))
            {
                var mark = marks.Last();
                var dst = Player.DistanceTo(new Vector2(mark.X, mark.Z));
                if(dst > 20f)
                {
                    S.TextAdvanceIPC.EnqueueMoveTo2DPoint(new()
                    {
                        Mount = forceMount || dst > 50f,
                        Fly = C.AllowFlight,
                        NoInteract = true,
                        Position = new(mark.X, mark.Y, mark.Z)
                    }, 3f);
                }
            }
        }
    }


    public static void Initiate(uint questId)
    {
        if(Svc.Condition[ConditionFlag.BoundByDuty])
        {
            DuoLog.Error($"Can not initiate, already doing levequest");
            return;
        }
        if(!EzThrottler.Check("InitiateThrottle")) return;
        S.TaskManager.Enqueue(() =>
        {
            var sortedMarkers = AgentHUD.Instance()->MapMarkers.ToArray().OrderBy(x => Player.DistanceTo(new Vector2(x.X, x.Z)));
            if(sortedMarkers.TryGetFirst(x => x.IconId == 60492 && MemoryHelper.ReadSeString(x.TooltipString).ExtractText() == Svc.Data.GetExcelSheet<Leve>().GetRow(questId).Name.ExtractText(), out var mark) || sortedMarkers.TryGetFirst(x => x.IconId == 60492, out mark))
            {
                var d2d = Player.DistanceTo(new Vector2(mark.X, mark.Z));
                if(d2d > 30)
                {
                    if(!S.TextAdvanceIPC.IsBusy() && EzThrottler.Throttle("PreliminaryMoveTo", 1000))
                    {
                        S.TextAdvanceIPC.EnqueueMoveTo2DPoint(new()
                        {
                            DataID = 0,
                            Fly = C.AllowFlight,
                            Mount = true,
                            NoInteract = true,
                            Position = new Vector3(mark.X, 0, mark.Z),
                        }, 3f);
                    }
                }
                else
                {
                    S.TextAdvanceIPC.Stop();
                    return true;
                }
            }
            return false;
        }, $"Move to position, id={questId}", new(timeLimitMS:120*1000, abortOnTimeout:false));
        S.TaskManager.Enqueue(() =>
        {
            if(TryGetAddonMaster<AddonMaster.JournalDetail>("JournalDetail", out var m) && m.IsAddonReady) return true;
            if(EzThrottler.Throttle("ALQ.ThrottleOpen", 5000))
            {
                AgentQuestJournal.Instance()->OpenForQuest(questId, 2, keepOpen: true);
            }
            return false;
        }, $"Open journal, id={questId}");
        S.TaskManager.Enqueue(() =>
        {
            if(TryGetAddonMaster<AddonMaster.JournalDetail>("JournalDetail", out var m) && m.IsAddonReady)
            {
                if(m.CanInitiate)
                {
                    m.Initiate();
                    return true;
                }
            }
            return false;
        }, $"Initiate, id={questId}");
        S.TaskManager.Enqueue(() =>
        {
            if(Svc.Condition[ConditionFlag.BoundByDuty]) return true;
            if(TryGetAddonByName<AtkUnitBase>("GuildLeveDifficulty", out var addon) && IsAddonReady(addon))
            {
                var btn = addon->GetButtonNodeById(7);
                if(btn->IsEnabled && EzThrottler.Throttle("ALQ.Click"))
                {
                    btn->ClickAddonButton(addon);
                    return true;
                }
            }
            return false;
        }, $"Select difficulty, id={questId}");
        S.TaskManager.Enqueue(() =>
        {
            EzThrottler.Throttle("Wait", 2000, true);
            EzThrottler.Throttle("InitiateThrottle", 20000, true);
        }, $"Throttle initiation, id={questId}");
    }

    public static bool RecursivelyAcceptLeves()
    {
        if(TryGetAddonMaster<GuildLeve>("GuildLeve", out var m) && m.IsAddonReady)
        {
            var currentLeves = m.Levequests.Length;
            var acceptableLeves = S.Core.Selected.LeveList.ToDictionary(x => Svc.Data.GetExcelSheet<Leve>().GetRow(x).Name.ExtractText(), x => x);
            var preferredLeves = acceptableLeves.Where(x => S.Core.Selected.Favorite.Contains(x.Value)).ToDictionary();
            GuildLeve.Levequest selectedLeve = null;
            foreach(var l in m.Levequests)
            {
                if(preferredLeves.TryGetValue(l.Name, out var result) || acceptableLeves.TryGetValue(l.Name, out result))
                {
                    selectedLeve = l;
                    break;
                }
            }
            if(selectedLeve != null)
            {
                S.TaskManager.BeginStack();
                try
                {
                    for(int i = 0; i < 10; i++)
                    {
                        S.TaskManager.Enqueue(() => SelectLeveInternal(selectedLeve.Name), $"Select {selectedLeve.Name}");
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
                    }, $"Accept {selectedLeve.Name}");
                    S.TaskManager.Enqueue(() => TryGetAddonMaster<GuildLeve>("GuildLeve", out var m) && m.IsAddonReady && m.Levequests.Length != currentLeves, "Wait for acceptance");
                    if(QuestManager.Instance()->NumLeveAllowances > C.StopAt && C.AllowMultiple) S.TaskManager.Enqueue(RecursivelyAcceptLeves);
                }
                catch(Exception e)
                {
                    e.Log();
                }
                S.TaskManager.InsertStack();
            }
            return true;
        }
        return false;
    }

    private static bool SelectLeveInternal(string name)
    {
        if(TryGetAddonMaster<GuildLeve>("GuildLeve", out var m) && m.IsAddonReady)
        {
            foreach(var l in m.Levequests)
            {
                if(l.Name == name)
                {
                    l.Select();
                    return true;
                }
            }
        }
        return false;
    }

    public static float GetDistanceToLeve(uint leveId)
    {
        var leveName = Svc.Data.GetExcelSheet<Leve>().GetRow(leveId).Name.ExtractText();
        if(AgentHUD.Instance()->MapMarkers.TryGetFirst(x => x.IconId == 60492 && MemoryHelper.ReadSeString(x.TooltipString).ExtractText() == leveName, out var m))
        {
            return Player.DistanceTo(new Vector2(m.X, m.Z));
        }
        return 999999;
    }
}
