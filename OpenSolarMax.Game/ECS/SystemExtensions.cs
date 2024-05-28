using Arch.System;

namespace OpenSolarMax.Game;

public static class SystemExtensions
{
    public static void JustUpdate<T>(this ISystem<T> system, in T t)
    {
        system.BeforeUpdate(in t);
        system.Update(in t);
        system.AfterUpdate(in t);
    }
}
