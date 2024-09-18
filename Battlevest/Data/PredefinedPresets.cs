using ECommons.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlevest.Data;
public static class PredefinedPresets
{
    public static LevePlan[] GetList() => ((string[])[
        """
        {"NpcDataID":1004735,"Name":"Pre-expert GC seals farm - Moraby Drydocks","Territory":135,"LeveList":[809,808,806,797,794,796],"Difficulty":null,"IgnoredMobs":[117],"ForcedMobs":[1160,1117,1123,1165],"Favorite":[809,797]}
        """,
        """
        {"NpcDataID":1004739,"Name":"Pre-expert GC seals farm - Camp Drybone","Territory":145,"LeveList":[814,816,817,802,804,805],"Difficulty":null,"IgnoredMobs":[113],"ForcedMobs":[1108,1028,1109],"Favorite":[817,805]}
        """,
        """
        {"NpcDataID":1004737,"Name":"Pre-expert GC seals farm - Hawthorne Hut","Territory":152,"LeveList":[813,812,810,800,801,798],"Difficulty":null,"IgnoredMobs":[115],"ForcedMobs":[1123,1137,1143],"Favorite":[813,801]}
        """
        ]).Select(EzConfig.DefaultSerializationFactory.Deserialize<LevePlan>).ToArray();
}
