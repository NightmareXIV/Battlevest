using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlevest.Data;
public class MoveData
{
    public Vector3 Position;
    public uint DataID;
    public bool NoInteract;
    public bool? Mount = null;
    public bool? Fly = null;
}
