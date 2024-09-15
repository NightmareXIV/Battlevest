using Battlevest.Data;
using ECommons.Configuration;
using ECommons.LazyDataHelpers;
using ECommons.Singletons;

namespace Battlevest;

[Purgeable]
public class Battlevest : IDalamudPlugin
{
    public static Battlevest P;
    public static Config C;
    public Battlevest(IDalamudPluginInterface pi)
    {
        P = this;
        ECommonsMain.Init(pi, this);
        C = EzConfig.Init<Config>();
        SingletonServiceManager.Initialize(typeof(S));
    }

    public void Dispose()
    {
        S.Core.RelinquishExternalControl();
        ECommonsMain.Dispose();
    }
}
