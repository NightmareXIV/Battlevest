using Battlevest.Data;
using Dalamud.Game.ClientState.Objects.Enums;
using ECommons.Automation;
using ECommons.ExcelServices;
using ECommons.Funding;
using ECommons.SimpleGui;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;

namespace Battlevest.Gui;
public unsafe class MainWindow : ConfigWindow
{
    public WindowSystem WindowSystem = new();
    ref LevePlan Selected => ref S.Core.Selected;
    public MainWindow()
    {
        EzConfigGui.Init(this);
        Svc.PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        WindowSystem.AddWindow(this);
    }

    public override void Draw()
    {
        ImGuiEx.EzTabBar("", PatreonBanner.Text,
            ("Options", DrawOptions, null, true),
            ("Plans", DrawPlans, null, true),
            InternalLog.ImGuiTab(),
            ("Debug", DrawDebug, null, true)
            );
    }

    void DrawOptions()
    {
        ImGuiEx.Text("Requirements:");
        ImGuiEx.TextWrapped("- TextAdvance installed, enabled and \"Navigation\" enabled it it's options");
        ImGuiEx.PluginAvailabilityIndicator([new("TextAdvance", new Version(3, 2, 3, 6))]);
        ImGuiEx.TextWrapped("- Vnavmesh installed and enabled");
        ImGuiEx.PluginAvailabilityIndicator([new("vnavmesh")]);
        ImGuiEx.TextWrapped("- Sloth Combo or any other rotation helper plugin that can put your whole rotation on a single button, or rotation plugin that will auto-attack any targeted hostile monster (in this case, set your keybind to None). If you are overlevelled, you can probably get away with just spamming a single GCD skill. Preferably, use ranged class.");
        ImGui.SetNextItemWidth(200f);
        ImGuiEx.EnumCombo("Key to spam for attack", ref C.Key);
    }

    void DrawPlans()
    {
        ImGuiEx.InputWithRightButtonsArea(() =>
        {
            if(ImGui.BeginCombo("##leveselect", Selected?.GetName() ?? "No plan selected"))
            {
                foreach(var x in C.Plans)
                {
                    if(ImGui.Selectable($"{x.GetName()}##{x.ID}"))
                    {
                        Selected = x;
                    }
                }
                ImGui.EndCombo();
            }
        }, () =>
        {
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add from target", Svc.Targets.Target?.ObjectKind == ObjectKind.EventNpc))
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
            if(Selected != null)
            {
                ImGui.SameLine();
                if(ImGuiEx.IconButton(FontAwesomeIcon.Trash, enabled: ImGuiEx.Ctrl))
                {
                    new TickScheduler(() => C.Plans.Remove(Selected));
                }
                ImGuiEx.Tooltip("Hold CTRL and click to delete");
            }
        });

        if(Selected != null)
        {
            ImGuiEx.Text($"{ExcelTerritoryHelper.GetName(Selected.Territory)}\n{Selected.NpcName} (#{Selected.NpcDataID:X8})");
            ImGuiEx.TextWrapped("Select levequests you want to do.");
            ImGuiEx.TextWrapped(EColor.RedBright, "Only \"kill enemies\" leve types are supported");
            foreach(var x in Svc.Data.GetExcelSheet<Leve>().Where(l => l.LevelLevemete.Value?.Object == Selected.NpcDataID))
            {
                if(x.ClassJobCategory.Value.IsJobInCategory(Job.PLD))
                {
                    ImGuiEx.CollectionCheckbox($"Lv. {x.ClassJobLevel} - {x.Name.ExtractText()}", x.RowId, Selected.LeveList);
                }
            }
        }
    }

    void DrawDebug()
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
