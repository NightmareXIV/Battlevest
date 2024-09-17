using Battlevest.Data;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.Funding;
using ECommons.GameHelpers;
using ECommons.Reflection;
using ECommons.SimpleGui;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;

namespace Battlevest.Gui;
public unsafe class MainWindow : ConfigWindow
{
    private ref LevePlan Selected => ref S.Core.Selected;
    public MainWindow()
    {
        EzConfigGui.Init(this);
        EzConfigGui.WindowSystem.AddWindow(new StatusWindow());
    }

    public override void Draw()
    {
        ImGuiEx.LineCentered(() => ImGuiEx.Text(EColor.RedBright, "Alpha version"));
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
        ImGuiEx.Text("Requirements:");
        ImGuiEx.TextWrapped("- TextAdvance installed, enabled and \"Navigation\" enabled it it's options");
        ImGuiEx.PluginAvailabilityIndicator([new("TextAdvance", new Version(3, 2, 3, 6))]);
        ImGuiEx.TextWrapped("- Vnavmesh installed and enabled");
        ImGuiEx.PluginAvailabilityIndicator([new("vnavmesh")]);
        ImGuiEx.TextWrapped("- Sloth Combo or any other rotation helper plugin that can put your ranged rotation on a single button, or rotation plugin that will auto-attack any targeted hostile monster (in this case, set your keybind to None). If you are overlevelled, you can probably get away with just spamming a single GCD skill. ");
        ImGuiEx.TextWrapped("Additionally:");
        ImGuiEx.TextWrapped("- Best results are achieved on ranged jobs");
        ImGuiEx.TextWrapped("- BossMod's AI can be used to avoid AOE");
        ImGui.SetNextItemWidth(200f);
        ImGuiEx.EnumCombo("Key to spam for attack", ref C.Key);
        //ImGui.Checkbox("Allow flight (also must be enabled in TextAdvance)");
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
            if(Player.TerritoryIntendedUse == TerritoryIntendedUseEnum.City_Area)
            {
                ImGuiEx.Text(EColor.RedBright, "City leves are unsupported.");
            }
            ImGuiEx.InputWithRightButtonsArea(() =>
            {
                if(ImGui.BeginCombo("##leveselect", Selected?.GetName() ?? "No plan selected"))
                {
                    foreach(var x in C.Plans.Where(x => Player.Territory == x.Territory))
                    {
                        if(ImGui.Selectable($"{x.GetName()}##{x.ID}"))
                        {
                            Selected = x;
                        }
                    }
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey3);
                    foreach(var x in C.Plans.Where(x => Player.Territory != x.Territory))
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
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add from target", Svc.Targets.Target?.ObjectKind == ObjectKind.EventNpc) && Player.TerritoryIntendedUse != TerritoryIntendedUseEnum.City_Area)
                {
                    var plan = new LevePlan()
                    {
                        NpcDataID = Svc.Targets.Target.DataId,
                        NpcName = Svc.Targets.Target.Name.ExtractText(),
                        Territory = Svc.ClientState.TerritoryType,
                    };
                    C.Plans.Add(plan);
                    Selected = plan;
                }
                ImGuiEx.Tooltip("To create plan, target levemete and press this button.");
                ImGui.SameLine(0,1);
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
                if(Selected != null)
                {
                    ImGui.SameLine(0, 1);
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Copy))
                    {
                        Copy(EzConfig.DefaultSerializationFactory.Serialize(Selected, false));
                    }
                    ImGuiEx.Tooltip("Copy");
                    ImGui.SameLine(0, 1);
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Trash, enabled: ImGuiEx.Ctrl))
                    {
                        new TickScheduler(
                            () =>
                            {
                                C.Plans.Remove(Selected);
                                Selected = null;
                            }
                            );
                    }
                    ImGuiEx.Tooltip("Hold CTRL and click to delete");
                }
            });
            if(Selected != null)
            {
                ImGuiEx.TextWrapped(Selected.GetName());
                ImGuiEx.TextWrapped("Select levequests you want to do.");
                ImGuiEx.TextWrapped(EColor.RedBright, "Only \"kill enemies\" leve types are supported");
                foreach(var x in Svc.Data.GetExcelSheet<Leve>().Where(l => l.LevelLevemete.Value?.Object == Selected.NpcDataID))
                {
                    if(x.ClassJobCategory.Value.IsJobInCategory(Job.PLD))
                    {
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGuiEx.CollectionButtonCheckbox(FontAwesomeIcon.Heart.ToIconString() + $"##{x.RowId}", x.RowId, Selected.Favorite);
                        ImGui.PopFont();
                        ImGuiEx.Tooltip("Mark as favorite. Favorite leves will be prioritized for picking. Usually you'd want to make shortest leves favorite.");
                        ImGui.SameLine();
                        ImGuiEx.CollectionCheckbox($"Lv. {x.ClassJobLevel} - {x.Name.ExtractText()}", x.RowId, Selected.LeveList);
                    }
                }

                /*ImGuiEx.InputInt(150f, "Difficulty", ref Selected.Difficulty);
                if(Selected.Difficulty != null)
                {
                    if(Selected.Difficulty.Value < 0) Selected.Difficulty = 0;
                    if(Selected.Difficulty.Value > 4) Selected.Difficulty = 4;
                }*/

                if(Player.Territory != Selected.Territory)
                {
                    ImGuiEx.Text(EColor.RedBright, "Current zone is inappropriate for this plan");
                }
                else if(QuestManager.Instance()->LeveQuests.ToArray().Any(x => x.Flags == 0 && Selected.LeveList.Contains(x.LeveId)) || Svc.Objects.Any(x => x.DataId == Selected.NpcDataID && x.IsTargetable && Player.DistanceTo(x) < 6))
                {
                    if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Play, "Begin", Selected.LeveList.Count > 0))
                    {
                        S.Core.Enabled = true;
                    }
                }
                else
                {
                    ImGuiEx.Text(EColor.RedBright, "Approach NPC to begin this leve plan");
                }
            }
        }

        if(Selected != null)
        {
            ImGuiEx.TreeNodeCollapsingHeader($"Configure forced mobs ({Selected.ForcedMobs.Count} mobs)###forced", () =>
            {
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.FastForward, "Add target as forced mob", Svc.Targets.Target is IBattleNpc))
                {
                    Selected.ForcedMobs.Add(((IBattleNpc)Svc.Targets.Target).NameId);
                }
                List<ImGuiEx.EzTableEntry> e = [];
                foreach(var x in Selected.ForcedMobs)
                {
                    e.Add(new("Mob name", true, () => ImGuiEx.Text(Svc.Data.GetExcelSheet<BNpcName>().GetRow(x)?.Singular ?? x.ToString())), new("##del", () =>
                    {
                        if(ImGui.SmallButton($"Delete##f{x}"))
                        {
                            new TickScheduler(() => Selected.ForcedMobs.Remove(x));
                        }
                    }));
                }
                ImGuiEx.EzTable(ImGuiTableFlags.BordersInner | ImGuiTableFlags.SizingFixedFit, e);
            });

            ImGuiEx.TreeNodeCollapsingHeader($"Configure ignored mobs ({Selected.IgnoredMobs.Count} mobs)###ignored", () =>
            {
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Ban, "Add target as ignored mob", Svc.Targets.Target is IBattleNpc))
                {
                    Selected.IgnoredMobs.Add(((IBattleNpc)Svc.Targets.Target).NameId);
                }
                List<ImGuiEx.EzTableEntry> e = [];
                foreach(var x in Selected.IgnoredMobs)
                {
                    e.Add(new("Mob name", true, () => ImGuiEx.Text(Svc.Data.GetExcelSheet<BNpcName>().GetRow(x)?.Singular ?? x.ToString())), new("##del", () =>
                    {
                        if(ImGui.SmallButton($"Delete##i{x}"))
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
                    ImGuiEx.Text($"Accepted leve: {Svc.Data.GetExcelSheet<Leve>().GetRow(x.LeveId).Name.ExtractText()}");
                    if(ImGui.Button("Initiate##" + x.LeveId.ToString()))
                    {
                        Utils.Initiate(x.LeveId);
                    }
                }
            }
        }
    }
}
