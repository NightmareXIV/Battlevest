using ECommons.ExcelServices;
using Lumina.Excel;

namespace Battlevest.Sheets;
public static class CustomSheet
{
    public static ExcelSheet<QuestDialogueText> GuildLeveAssignment => Svc.Data.GetExcelSheet<QuestDialogueText>(name: "leve/GuildLeveAssignment");
    public static ExcelSheet<QuestDialogueText> LeveDirector => Svc.Data.GetExcelSheet<QuestDialogueText>(name: "leve/LeveDirector");
}
