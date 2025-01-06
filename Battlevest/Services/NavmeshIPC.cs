using ECommons.EzIpcManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlevest.Services;
public class NavmeshIPC
{
    [EzIPC("Nav.IsReady")] public readonly Func<bool> IsReady;
    [EzIPC("Nav.BuildProgress")] public readonly Func<float> BuildProgress;
    [EzIPC("Nav.Reload")] public readonly Func<bool> Reload;
    [EzIPC("Nav.Rebuild")] public readonly Func<bool> Rebuild;
    /// <summary>
    /// Vector3 from, Vector3 to, bool fly
    /// </summary>
    [EzIPC("Nav.Pathfind")] public readonly Func<Vector3, Vector3, bool, Task<List<Vector3>>> Pathfind;
    [EzIPC("Nav.PathfindCancelAll")] public readonly Action PathfindCancelAll;

    [EzIPC("SimpleMove.PathfindAndMoveTo")] public readonly Func<Vector3, bool, bool> PathfindAndMoveTo;
    [EzIPC("SimpleMove.PathfindInProgress")] public readonly Func<bool> PathfindInProgress;

    [EzIPC("Path.Stop")] public readonly Action Stop;
    [EzIPC("Path.IsRunning")] public readonly Func<bool> IsRunning;

    /// <summary>
    /// Vector3 p, float halfExtentXZ, float halfExtentY
    /// </summary>
    [EzIPC("Query.Mesh.NearestPoint")] public readonly Func<Vector3, float, float, Vector3?> NearestPoint;
    /// <summary>
    /// Vector3 p, bool allowUnlandable, float halfExtentXZ
    /// </summary>
    [EzIPC("Query.Mesh.PointOnFloor")] public readonly Func<Vector3, bool, float, Vector3?> PointOnFloor;

    private NavmeshIPC()
    {
        EzIPC.Init(this, "vnavmesh", SafeWrapper.AnyException);
    }
}