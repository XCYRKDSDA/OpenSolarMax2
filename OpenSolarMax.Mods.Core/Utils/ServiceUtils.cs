using Arch.Core;
using Arch.Core.Extensions;

namespace OpenSolarMax.Mods.Core.Utils;

public static class ServiceUtils
{
    public static void Call<RequestT>(this World world, in RequestT request)
    {
        var requestEntity = world.Create([typeof(RequestT)]);
        requestEntity.Set(request);
    }
}
