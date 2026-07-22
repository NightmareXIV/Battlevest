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
        {"NpcDataID":1004735,"Name":"GC seals farm - Moraby Drydocks","Territory":135,"LeveList":[809,808,806,797,794,796],"Difficulty":null,"IgnoredMobs":[117],"ForcedMobs":[1160,1117,1123,1165],"Favorite":[809,797],"StopOnGcCap":false}
        
        """,
        """
        {"NpcDataID":1004739,"Name":"GC seals farm - Camp Drybone","Territory":145,"LeveList":[814,816,817,802,804,805],"Difficulty":null,"IgnoredMobs":[113],"ForcedMobs":[1108,1028,1109],"Favorite":[817,805],"StopOnGcCap":false}
        """,
        """
        {"NpcDataID":1004737,"Name":"GC seals farm - Hawthorne Hut","Territory":152,"LeveList":[813,812,810,800,801,798],"Difficulty":null,"IgnoredMobs":[115],"ForcedMobs":[1123,1137,1143],"Favorite":[813,801],"StopOnGcCap":false}
        """,
        """
        {"NpcDataID":1004735,"Name":"[Stop on Expert Delivery Unlock] GC seals farm - Moraby Drydocks","Territory":135,"LeveList":[809,808,806,797,794,796],"Difficulty":null,"IgnoredMobs":[117],"ForcedMobs":[1160,1117,1123,1165],"Favorite":[809,797],"StopOnGcCap":true}
        """,
        """
        {"NpcDataID":1004739,"Name":"[Stop on Expert Delivery Unlock] GC seals farm - Camp Drybone","Territory":145,"LeveList":[814,816,817,802,804,805],"Difficulty":null,"IgnoredMobs":[113],"ForcedMobs":[1108,1028,1109],"Favorite":[817,805],"StopOnGcCap":true}
        """,
        """
        {"NpcDataID":1004737,"Name":"[Stop on Expert Delivery Unlock] GC seals farm - Hawthorne Hut","Territory":152,"LeveList":[813,812,810,800,801,798],"Difficulty":null,"IgnoredMobs":[115],"ForcedMobs":[1123,1137,1143],"Favorite":[813,801],"StopOnGcCap":true}
        """,
        """
        {"NpcDataID":1000105,"Name":"Leves of Bentbranch - Trial Leve","Territory":148,"LeveList":[546],"Difficulty":null,"IgnoredMobs":[],"ForcedMobs":[206],"Favorite":[546],"StopOnGcCap":false,"SingleUse":true}
        """,
        """
        {"NpcDataID":1001796,"Name":"Leves of Horizon - Trial Leve","Territory":140,"LeveList":[566],"Difficulty":null,"IgnoredMobs":[],"ForcedMobs":[1037],"Favorite":[566],"StopOnGcCap":false,"SingleUse":true}
        """,
        """
        {"NpcDataID":1001788,"Name":"Leves of Swiftperch - Trial Leve","Territory":138,"LeveList":[556],"Difficulty":null,"IgnoredMobs":[],"ForcedMobs":[1081],"Favorite":[556],"StopOnGcCap":false,"SingleUse":true}
        """,
        """
        {"NpcDataID":1004344,"Name":"Leves of Costa del sol - Trial Leve","Territory":137,"LeveList":[629],"Difficulty":null,"IgnoredMobs":[],"ForcedMobs":[],"Favorite":[629],"StopOnGcCap":false,"SingleUse":true}
        """
        ]).Select(EzConfig.DefaultSerializationFactory.Deserialize<LevePlan>).ToArray();
}
