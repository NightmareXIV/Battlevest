using ECommons.Automation;
using ECommons.Funding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlevest.Gui;
public class StatusWindow : Window
{
    public StatusWindow() : base("Battlevest Active", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.IsOpen = true;
        this.RespectCloseHotkey = false;
        this.ShowCloseButton = false;
    }

    public override void Draw()
    {
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Stop, "Stop"))
        {
            Utils.Stop();
        }
        ImGui.SameLine();
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Cog, "Config"))
        {
            S.MainWindow.IsOpen = true;
        }
        ImGui.SameLine();
        PatreonBanner.DrawButton();
    }

    public override bool DrawConditions()
    {
        return S.Core.Enabled;
    }
}
