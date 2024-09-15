using Battlevest.Sheets;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Automation;
using ECommons.Automation.UIInput;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Interop;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;

namespace Battlevest;
public unsafe class Utils
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
        if(TryGetAddonMaster<AddonMaster.SelectYesno>("SelectYesno", out var m))
        {
            if(m.Text.EqualsAny(Svc.Data.GetExcelSheet<Addon>().GetRow(608).Text.ExtractText())
                ||
                m.Text.ContainsAny(StringComparison.OrdinalIgnoreCase, Svc.Data.GetExcelSheet<Leve_LeveDirector>().GetRow(0).Value.ExtractText(true)))
            {
                S.TextAdvanceIPC.Stop();
                if(EzThrottler.Throttle("YesNo"))
                {
                    m.Yes();
                    return true;
                }
            }
        }
        return false;
    }

    public static void HandleCombat(bool forceMount, out string status)
    {
        status = "Nothing...";
        if(IsOccupied() || Svc.Condition[ConditionFlag.Casting])
        {
            return;
        }
        var marks = AgentHUD.Instance()->MapMarkers.Where(x => x.IconId == 60492).OrderBy(x => Player.DistanceTo(new Vector3(x.X, x.Y, x.Z)));
        var combatTarget = Svc.Objects.OfType<IBattleNpc>().OrderBy(Player.DistanceTo).FirstOrDefault(x => !x.IsDead && x.GetNameplateKind().EqualsAny(NameplateKind.HostileEngagedSelfUndamaged, NameplateKind.HostileEngagedSelfDamaged) && x.Struct()->NamePlateIconId == 71244);
        combatTarget ??= Svc.Objects.OfType<IBattleNpc>().OrderBy(Player.DistanceTo).FirstOrDefault(x => !x.IsDead && x.IsHostile() && x.Struct()->NamePlateIconId == 71244);
        if(combatTarget != null && combatTarget.IsTargetable)
        {
            if(Player.DistanceTo(combatTarget) < 20f + (AgentMap.Instance()->IsPlayerMoving == 1 ? -5f : 0f) && Math.Abs(Player.Position.Y - combatTarget.Position.Y + (AgentMap.Instance()->IsPlayerMoving == 1 ? -2f : 0f)) < 10f)
            {
                if(Svc.Targets.Target != combatTarget) Svc.Targets.Target = combatTarget;
                S.TextAdvanceIPC.Stop();
                if(Svc.Condition[ConditionFlag.Mounted])
                {
                    if(EzThrottler.Throttle("Dismount", 1000))
                    {
                        Chat.Instance.ExecuteCommand("/generalaction \"Dismount\"");
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
                    var shouldMount = Player.DistanceTo(combatTarget) > 30f;
                    S.TextAdvanceIPC.EnqueueMoveTo2DPoint(new()
                    {
                        Mount = forceMount || shouldMount,
                        Fly = false,
                        NoInteract = true,
                        Position = combatTarget.Position
                    });
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
                        Mount = forceMount || dst > 40f,
                        Fly = false,
                        NoInteract = true,
                        Position = new(mark.X, mark.Y, mark.Z)
                    });
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
        S.TaskManager.Enqueue(() =>
        {
            if(TryGetAddonMaster<AddonMaster.JournalDetail>("JournalDetail", out var m) && m.IsAddonReady) return true;
            if(EzThrottler.Throttle("ALQ.ThrottleOpen", 5000))
            {
                AgentQuestJournal.Instance()->OpenForQuest(questId, 2, keepOpen: true);
            }
            return false;
        });
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
        });
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
        });
    }
}
