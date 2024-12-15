using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Utils;

public static class RevolutionUtils
{
    /// <summary>
    /// 根据星球同步轨道随机生成单位公转轨道
    /// </summary>
    /// <param name="planetOrbit">星球同步轨道组件</param>
    /// <param name="random">随机引擎</param>
    /// <param name="orbitOffsetRange">单位半径偏差范围</param>
    /// <returns>单位公转轨道组件</returns>
    public static RevolutionOrbit CreateRandomRevolutionOrbit(in PlanetGeostationaryOrbit planetOrbit,
                                                              Random random, float orbitOffsetRange)
    {
        float offset = ((float)random.NextDouble() - 0.5f) * orbitOffsetRange + 1;

        return new RevolutionOrbit
        {
            Rotation = planetOrbit.Rotation,
            Shape = new(planetOrbit.Radius * offset * 2, planetOrbit.Radius * offset * 2),
            Period = planetOrbit.Period * MathF.Pow(offset, 1.5f)
        };
    }

    /// <summary>
    /// 随机生成单位公转状态。
    /// 注意，因为认为调用该函数者希望操作的对象进行公转，因此状态中的<see cref="RevolutionState.Revolving"/>设置为了<c>true</c>
    /// </summary>
    /// <param name="random">随机引擎</param>
    /// <returns>单位公转状态组件</returns>
    public static RevolutionState CreateRandomState(Random random)
    {
        float phase = (float)random.NextDouble() * 2 * MathF.PI;

        return new RevolutionState { Phase = phase };
    }

    private const float _defaultOrbitOffsetRange = 0.3f;

    public static void RandomlySetShipOrbitAroundPlanet(Entity relationship, Entity planet,
                                                        Random? random = null,
                                                        float orbitOffsetRange = _defaultOrbitOffsetRange)
    {
        random ??= new();

        ref readonly var geostationaryOrbit = ref planet.Get<PlanetGeostationaryOrbit>();
        relationship.Get<RevolutionOrbit>() =
            CreateRandomRevolutionOrbit(in geostationaryOrbit, random, _defaultOrbitOffsetRange);
        relationship.Get<RevolutionState>() = CreateRandomState(random);
    }

    /// <summary>
    /// 计算实体绕其所在轨道公转的相对位姿
    /// </summary>
    /// <param name="orbit">实体所在轨道</param>
    /// <param name="state">实体当前公转状态</param>
    /// <returns>单位相对轨道所在实体的相对变换</returns>
    public static RelativeTransform CalculateTransform(in RevolutionOrbit orbit, in RevolutionState state)
        // 以+Z轴为轴, 逆时针旋转
        => new(Matrix.CreateTranslation(orbit.Shape.Width / 2, 0, 0)
               * Matrix.CreateRotationZ(state.Phase)
               * Matrix.CreateScale(1, orbit.Shape.Height / orbit.Shape.Width, 1)
               * Matrix.CreateFromQuaternion(orbit.Rotation));
}
