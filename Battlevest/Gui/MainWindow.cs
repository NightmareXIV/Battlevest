using Battlevest.Data;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Components;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Memory;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.Funding;
using ECommons.GameHelpers;
using ECommons.Reflection;
using ECommons.SimpleGui;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using NightmareUI;
using NightmareUI.PrimaryUI;

namespace Battlevest.Gui;
public unsafe class MainWindow : ConfigWindow
{
    private ref LevePlan Selected => ref S.Core.Selected;
    public readonly List<LevePlan> Presets = [.. PredefinedPresets.GetList()];
    private bool Readonly;
    public MainWindow()
    {
        EzConfigGui.Init(this);
        EzConfigGui.WindowSystem.AddWindow(new StatusWindow());
    }

    public override void Draw()
    {
        ImGuiEx.LineCentered("Beta", () => ImGuiEx.Text(EColor.OrangeBright, "Beta version"));
        PatreonBanner.DrawRight();
        ImGuiEx.EzTabBar("", PatreonBanner.Text,
            ("Options", DrawOptions, null, true),
            ("Plans", DrawPlans, null, true),
            InternalLog.ImGuiTab(),
            ("Debug", DrawDebug, null, true)
            );
    }

    private void DrawOptions()
    {
        new NuiBuilder().Section("Requirements").Widget(() =>
        {
            ImGuiEx.TextWrapped("- TextAdvance installed, enabled and \"Navigation\" enabled it it's options");
            ImGuiEx.PluginAvailabilityIndicator([new("TextAdvance", new Version(3, 2, 3, 7))]);
            ImGuiEx.TextWrapped("- Vnavmesh installed and enabled");
            ImGuiEx.PluginAvailabilityIndicator([new("vnavmesh")]);
            ImGuiEx.TextWrapped("- Wrath Combo or any other combo that can put significant part of your rotation to one button. If you are overlevelled, you can skip combo and just spam single GCD. Note that autorotation plugins will likely not work with Battlevest, so do not enable them. ");
            ImGuiEx.TextWrapped("Additionally:");
            ImGuiEx.TextWrapped("- Best results are achieved on ranged jobs");
            ImGuiEx.TextWrapped("- BossMod's AI can be used to avoid AOE");
        })
        .Section("Settings").Widget(() =>
        {
            ImGui.Checkbox("Use single key press to attack mobs", ref C.EnableKeySpam);
            ImGui.Indent();
            ImGuiEx.Text("Select mode:");
            ImGuiEx.RadioButtonBool("Press the key", "Execute hotbar slot", ref C.UseKeyMode);
            ImGui.Indent();
            if(C.UseKeyMode)
            {
                ImGui.SetNextItemWidth(200f);
                ImGuiEx.EnumCombo("Key to spam for attack", ref C.Key);
            }
            else
            {
                ImGui.SetNextItemWidth(100f);
                ImGui.InputInt("Hotbar index", ref C.HotbarSlot.Item1);
                ImGui.SetNextItemWidth(100f);
                ImGui.InputInt("Slot index", ref C.HotbarSlot.Item2);
                ImGui.SetNextItemWidth(200f);
                if(ImGui.BeginCombo("##sel", "Select..."))
                {
                    foreach(var x in Utils.GetHotbarActions())
                    {
                        if(ImGui.Selectable($"Hotbar {x.Hotbar} Slot {x.Slot}: {ExcelActionHelper.GetActionName(x.Action)}"))
                        {
                            C.HotbarSlot = (x.Hotbar, x.Slot);
                        }
                    }
                    ImGui.EndCombo();
                }
            }
            ImGui.Unindent();
            ImGui.Unindent();
            ImGui.SetNextItemWidth(100f);
            ImGui.InputInt("Stop upon reaching this amount of allowances", ref C.StopAt);
            ImGui.Checkbox("Allow accepting multiple levequests", ref C.AllowMultiple);
            ImGui.Checkbox("Allow flight", ref C.AllowFlight);
            ImGuiEx.HelpMarker("Also must be enabled in TextAdvance's settings.\nIn general, less reliable than walking.", EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
        }).Draw();
    }

    private void DrawPlans()
    {
        if(S.Core.Enabled)
        {
            if(ImGui.Button("Stop plugin"))
            {
                Utils.Stop();
            }
        }
        else
        {
            NuiTools.ButtonTabs("", [[new("Predefined", () => Readonly = true), new("Create your own", () => Readonly = false)]], child: false);
            if(!Readonly)
            {
                if(Player.TerritoryIntendedUse == TerritoryIntendedUseEnum.City_Area)
                {
                    ImGuiEx.LineCentered("nocity", () => ImGuiEx.Text(EColor.RedBright, "City leves are unsupported."));
                }
                else
                {
                    ImGuiEx.LineCentered("share", () =>
                    {
                        ImGuiEx.Text("Made a well working plan? Share it!");
                        if(ImGuiEx.HoveredAndClicked())
                        {
                            ShellStart("https://github.com/NightmareXIV/Battlevest/issues/new");
                        }
                    });
                }
            }
            if(Readonly) ImGuiEx.LineCentered("predef", () => ImGuiEx.Text(EColor.YellowBright, "Predefined plans can not be edited."));
            ImGuiEx.InputWithRightButtonsArea(() =>
            {
                var plans = Readonly ? Presets : C.Plans;
                if(!plans.Contains(Selected)) Selected = null;
                if(ImGui.BeginCombo("##leveselect", Selected?.GetName() ?? "No plan selected"))
                {
                    foreach(var x in plans.Where(x => Player.Territory == x.Territory))
                    {
                        if(ImGui.Selectable($"{x.GetName()}##{x.ID}"))
                        {
                            Selected = x;
                        }
                    }
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey3);
                    foreach(var x in plans.Where(x => Player.Territory != x.Territory))
                    {
                        if(ImGui.Selectable($"{x.GetName()}##{x.ID}"))
                        {
                            Selected = x;
                        }
                    }
                    ImGui.PopStyleColor();
                    ImGui.EndCombo();
                }
            }, () =>
            {
                if(!Readonly)
                {
                    if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add from target", Svc.Targets.Target?.ObjectKind == ObjectKind.EventNpc) && Player.TerritoryIntendedUse != TerritoryIntendedUseEnum.City_Area)
                    {
                        var plan = new LevePlan()
                        {
                            NpcDataID = Svc.Targets.Target.DataId,
                            Territory = Svc.ClientState.TerritoryType,
                        };
                        C.Plans.Add(plan);
                        Selected = plan;
                    }
                    ImGuiEx.Tooltip("To create plan, target levemete and press this button.");
                    ImGui.SameLine(0, 1);
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Paste))
                    {
                        try
                        {
                            var pl = EzConfig.DefaultSerializationFactory.Deserialize<LevePlan>(Paste());
                            C.Plans.Add(pl);
                            Selected = pl;
                        }
                        catch(Exception e)
                        {
                            e.LogDuo();
                        }
                    }
                    ImGuiEx.Tooltip("Paste");
                }
                if(Selected != null)
                {
                    ImGui.SameLine(0, 1);
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Copy))
                    {
                        Copy(EzConfig.DefaultSerializationFactory.Serialize(Selected, false));
                    }
                    ImGuiEx.Tooltip("Copy");
                    if(!Readonly)
                    {
                        ImGui.SameLine(0, 1);
                        if(ImGuiEx.IconButton(FontAwesomeIcon.Trash, enabled: ImGuiEx.Ctrl))
                        {
                            new TickScheduler(
                                () =>
                                {
                                    C.Plans.Remove(Selected);
                                }
                                );
                        }
                        ImGuiEx.Tooltip("Hold CTRL and click to delete");
                    }
                }
            });
            if(Selected != null)
            {
                if(!Readonly)
                {
                    ImGuiEx.SetNextItemFullWidth();
                    ImGui.InputTextWithHint("##name", Selected.GetName(), ref Selected.Name, 200);
                }
                ImGuiEx.LineCentered("npc", () => ImGuiEx.Text($"NPC: {Selected.GetNPCName()} | Zone: {Selected.GetZoneName()}"));
                ImGuiEx.LineCentered("init", () =>
                {
                    if(Player.Territory != Selected.Territory)
                    {
                        ImGuiEx.Text(EColor.RedBright, "Current zone is inappropriate for this plan");
                    }
                    else if(QuestManager.Instance()->LeveQuests.ToArray().Any(x => x.Flags == 0 && Selected.LeveList.Contains(x.LeveId)) || (Svc.Objects.Any(x => x.DataId == Selected.NpcDataID && x.IsTargetable && Player.DistanceTo(x) < 6)))
                    {
                        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Play, "Begin", Selected.LeveList.Count > 0))
                        {
                            S.Core.StopNext = false;
                            S.Core.Enabled = true;
                        }
                    }
                    else
                    {
                        ImGuiEx.Text(EColor.RedBright, "Approach NPC to begin this leve plan");
                    }
                });
                if(!Readonly)
                {
                    ImGuiEx.LineCentered("sel1", () => ImGuiEx.Text("Select levequests you want to do."));
                    ImGuiEx.LineCentered("sel2", () => ImGuiEx.Text(EColor.RedBright, "Only \"kill enemies\" leve types are supported"));
                }
                if(Readonly) ImGui.BeginDisabled();
                foreach(var x in Svc.Data.GetExcelSheet<Leve>().Where(l => l.LevelLevemete.ValueNullable?.Object.RowId == Selected.NpcDataID))
                {
                    if(x.ClassJobCategory.Value.IsJobInCategory(Job.PLD))
                    {
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGuiEx.CollectionButtonCheckbox(FontAwesomeIcon.Heart.ToIconString() + $"##{x.RowId}", x.RowId, Selected.Favorite);
                        ImGui.PopFont();
                        ImGuiEx.Tooltip("Mark as favorite. Favorite leves will be prioritized for picking. Usually you'd want to make shortest leves favorite.");
                        ImGui.SameLine();
                        ImGuiEx.CollectionCheckbox($"Lv. {x.ClassJobLevel} - {x.Name.GetText()}", x.RowId, Selected.LeveList);
                    }
                }
                ImGui.Checkbox("Stop upon reaching required seals amount to unlock Expert Delivery", ref Selected.StopOnGcCap);
                if(Readonly) ImGui.EndDisabled();
            }
        }

        if(Selected != null)
        {
            ImGuiEx.TreeNodeCollapsingHeader($"Preferred mobs ({Selected.ForcedMobs.Count} mobs)###forced", () =>
            {
                if(!Readonly && ImGuiEx.IconButtonWithText(FontAwesomeIcon.FastForward, "Add target as forced mob", Svc.Targets.Target is IBattleNpc))
                {
                    Selected.ForcedMobs.Add(((IBattleNpc)Svc.Targets.Target).NameId);
                }
                List<ImGuiEx.EzTableEntry> e = [];
                foreach(var x in Selected.ForcedMobs)
                {
                    e.Add(new("Mob name", true, () => ImGuiEx.Text(Svc.Data.GetExcelSheet<BNpcName>().GetRowOrDefault(x)?.Singular.ToString() ?? x.ToString())), new("##del", () =>
                    {
                        if(!Readonly && ImGui.SmallButton($"Delete##f{x}"))
                        {
                            new TickScheduler(() => Selected.ForcedMobs.Remove(x));
                        }
                    }));
                }
                ImGuiEx.EzTable(ImGuiTableFlags.BordersInner | ImGuiTableFlags.SizingFixedFit, e);
            });

            ImGuiEx.TreeNodeCollapsingHeader($"Ignored mobs ({Selected.IgnoredMobs.Count} mobs)###ignored", () =>
            {
                if(!Readonly && ImGuiEx.IconButtonWithText(FontAwesomeIcon.Ban, "Add target as ignored mob", Svc.Targets.Target is IBattleNpc))
                {
                    Selected.IgnoredMobs.Add(((IBattleNpc)Svc.Targets.Target).NameId);
                }
                List<ImGuiEx.EzTableEntry> e = [];
                foreach(var x in Selected.IgnoredMobs)
                {
                    e.Add(new("Mob name", true, () => ImGuiEx.Text(Svc.Data.GetExcelSheet<BNpcName>().GetRowOrDefault(x)?.Singular.ToString() ?? x.ToString())), new("##del", () =>
                    {
                        if(!Readonly && ImGui.SmallButton($"Delete##i{x}"))
                        {
                            new TickScheduler(() => Selected.IgnoredMobs.Remove(x));
                        }
                    }));
                }
                ImGuiEx.EzTable(ImGuiTableFlags.BordersInner | ImGuiTableFlags.SizingFixedFit, e);
            });
        }
    }

    private void DrawDebug()
    {
        ImGuiEx.Text($"Remains leves: {QuestManager.Instance()->NumLeveAllowances}");
        if(ImGui.CollapsingHeader("HUD"))
        {
            ImGuiEx.Text($"Initial markers:");
            var markers = AgentHUD.Instance()->MapMarkers;
            foreach(var x in markers.Where(s => s.IconId == 60492))
            {
                ImGuiEx.Text($"Coord: {x.X}, {x.Z}; radius: {x.Radius} [{MemoryHelper.ReadSeString(x.TooltipString)}]");
            }
            ImGuiEx.Text("All markers:");

            foreach(var x in markers)
            {
                ImGuiEx.Text($"IconID: {x.IconId}, Coord: {x.X}, {x.Z}; radius: {x.Radius} [{MemoryHelper.ReadSeString(x.TooltipString)}]");
            }
        }
        if(ImGui.Button("Force melee on target")) EzThrottler.Throttle($"ForcedMelee_{Svc.Targets.Target?.EntityId}", 10000, true);
        if(ImGui.Button("Ignore target")) EzThrottler.Throttle($"Ignore_{Svc.Targets.Target?.EntityId}", 10000, true);
        if(ImGui.CollapsingHeader("Debug leves"))
        {
            var leves = QuestManager.Instance()->LeveQuests;
            foreach(var x in leves)
            {
                ImGuiEx.Text($"{x.LeveId} - {Svc.Data.GetExcelSheet<Leve>().GetRow(x.LeveId)} - {x.Flags:B16}");
            }
        }
        if(ImGui.CollapsingHeader("Debug"))
        {
            if(TryGetAddonMaster<GuildLeve>("GuildLeve", out var m) && m.IsAddonReady)
            {
                foreach(var l in m.Levequests)
                {
                    ImGuiEx.Text($"{l.Name} ({l.Level})");
                    ImGui.SameLine();
                    if(ImGui.SmallButton("Select##" + l.Name)) l.Select();
                }
                ref var r = ref Ref<int>.Get("Leve");
                ImGui.InputInt("id", ref r);
                if(ImGui.Button("Callback"))
                {
                    Callback.Fire(m.Base, true, 13, 1, r);
                }
                if(TryGetAddonMaster<AddonMaster.JournalDetail>("JournalDetail", out var det) && det.IsAddonReady)
                {
                    if(det.CanInitiate)
                    {
                        if(ImGui.Button("Initiate")) det.Initiate();
                    }
                }
            }
            ImGui.Separator();
            var quests = QuestManager.Instance()->LeveQuests;
            foreach(var x in quests)
            {
                if(x.LeveId != 0)
                {
                    ImGuiEx.Text($"Accepted leve: {Svc.Data.GetExcelSheet<Leve>().GetRow(x.LeveId).Name.GetText()}");
                    if(ImGui.Button("Initiate##" + x.LeveId.ToString()))
                    {
                        Utils.Initiate(x.LeveId);
                    }
                }
            }
        }
    }
}
