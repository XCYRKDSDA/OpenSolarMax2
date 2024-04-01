using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Utils;

public static class AnchorageUtils
{
    private const float _defaultRevolutionOffsetRange = 0.3f;

    public static void AnchorTo(this Entity ship, Entity planet, Random? random = null,
                                float revolutionOffsetRange = _defaultRevolutionOffsetRange)
    {
        random ??= new();

        // 设置停靠关系
        ship.SetParent<Components.Anchorage>(planet);

        // 设置变换关系
        ship.SetParent<RelativeTransform>(planet);

        // 随机生成并泊入轨道
        ship.Get<RevolutionOrbit>() = RevolutionUtils.CreateRandomRevolutionOrbit(
            in planet.Get<PlanetGeostationaryOrbit>(), random, revolutionOffsetRange);
        ship.Get<RevolutionState>() = RevolutionUtils.CreateRandomState(random);
    }
}
