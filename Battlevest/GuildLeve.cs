using Dalamud.Memory;
using ECommons.Automation;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace Battlevest;
public unsafe class GuildLeve : AddonMasterBase<AddonGuildLeve>
{
    public GuildLeve(nint addon) : base(addon)
    {
    }

    public GuildLeve(void* addon) : base(addon)
    {
    }

    public uint NumEntries => Addon->AtkValues[25].UInt;

    public Levequest[] Levequests
    {
        get
        {
            var ret = new List<Levequest>();
            for(var i = 0; i < NumEntries; i++)
            {
                var leveName = Addon->AtkValues[626 + i * 2];
                var leveLevel = Addon->AtkValues[627 + i * 2];
                if(leveName.Type.EqualsAny(ValueType.String, ValueType.ManagedString, ValueType.String8))
                {
                    var leve = new Levequest(this, i)
                    {
                        Name = MemoryHelper.ReadSeStringNullTerminated((nint)leveName.String).ExtractText()
                    };
                    if(leveLevel.Type.EqualsAny(ValueType.String, ValueType.ManagedString, ValueType.String8))
                    {
                        leve.Level = MemoryHelper.ReadSeStringNullTerminated((nint)leveLevel.String).ExtractText();
                    }
                    ret.Add(leve);
                }
                else
                {
                    break;
                }
            }
            return [.. ret];
        }
    }

    public class Levequest(GuildLeve master, int index)
    {
        public string Name;
        public string? Level;

        public void Select()
        {
            var quest = Svc.Data.GetExcelSheet<Leve>().FirstOrDefault(x => x.Name.ExtractText() == Name);
            if(quest == null)
            {
                PluginLog.Error($"Failed to select levequest, requested name not found: {Name}");
            }
            else
            {
                Callback.Fire(master.Base, true, 13, index, (int)quest.RowId);
            }
        }
    }
}