using ECommons.ExcelServices;
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
    public string NpcName = "";
    public uint Territory;
    public List<uint> LeveList = [];

    public string GetName()
    {
        return $"{ExcelTerritoryHelper.GetName(this.Territory)} - {this.NpcName} - {LeveList.Count} leves";
    }
}
