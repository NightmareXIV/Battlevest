using ECommons.EzIpcManager;

namespace Battlevest.Services;
public class BossModIPC
{
    public BossModIPC() => EzIPC.Init(this, "BossMod", SafeWrapper.AnyException);

    [EzIPC("Presets.%m", true)] public readonly Func<string, bool> SetActive;
    [EzIPC("Presets.%m", true)] public readonly Func<bool> ClearActive;
}
