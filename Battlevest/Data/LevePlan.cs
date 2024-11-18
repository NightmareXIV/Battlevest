using ECommons.ExcelServices;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Battlevest.Data;
public class LevePlan
{
    internal string ID = Guid.NewGuid().ToString();
    public uint NpcDataID = 0;
    public string Name = "";
    public uint Territory;
    public List<uint> LeveList = [];
    public int? Difficulty = null;
    public HashSet<uint> IgnoredMobs = [];
    public HashSet<uint> ForcedMobs = [];
    public HashSet<uint> Favorite = [];

    public string GetName()
    {
        if(this.Name != "") return this.Name;
        string text;
        if(LeveList.Count == 0)
        {
            text = "No quests selected";
        }
        else if(LeveList.Count < 3)
        {
            text = LeveList.Select(x => Svc.Data.GetExcelSheet<Leve>().GetRowOrDefault(x)?.Name.ExtractText() ?? "...").Print(", ");
        }
        else
        {
            text = LeveList[0..1].Select(x => Svc.Data.GetExcelSheet<Leve>().GetRowOrDefault(x)?.Name.ExtractText() ?? "...").Print(", ") + $" and {LeveList.Count - 2} more";
        }
        return $"{ExcelTerritoryHelper.GetName(Territory)} - {GetNPCName()} - {text}";
    }

    public string GetNPCName()
    {
        return Svc.Data.GetExcelSheet<ENpcResident>().GetRowOrDefault(this.NpcDataID)?.Singular.ToString() ?? $"Unk{this.NpcDataID:X8}";
    }

    public string GetZoneName()
    {
        return ExcelTerritoryHelper.GetName(this.Territory);
    }
}
