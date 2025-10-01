using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Game.Modding;

/// <summary>
/// 执行关卡加载的系统。将被额外注入到 SimulateSystem 中，在第一次执行时加载关卡中所有内容
/// </summary>
[SimulateSystem, Stage2, CreateEntities]
internal class LoadLevelSystem(World world, IAssetsManager assets, Level level) : IStructuralChangeSystem
{
    private readonly WorldLoader _worldLoader = new(assets);

    public void Initialize(CommandBuffer commandBuffer)
    {
        _worldLoader.Load(level, world, commandBuffer);
    }

    public void Update(GameTime gameTime, CommandBuffer commandBuffer) { }
}
