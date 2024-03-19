using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

public class UpdateTreeSystem<T>(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly QueryDescription _parentDesc = new QueryDescription().WithAll<Tree<T>.Parent>();
    private readonly QueryDescription _childDesc = new QueryDescription().WithAll<Tree<T>.Child>();

    public override void Update(in GameTime t)
    {
        // 清空父实体记录的子实体
        World.Query(in _parentDesc, (ref Tree<T>.Parent relationship)
            => relationship._children.Clear());

        // 将子实体添加到父实体的关系组件中
        World.Query(in _childDesc, (Entity child, ref Tree<T>.Child relationship) =>
        {
            var parent = relationship.Parent;
            if (parent == Entity.Null)
                return;

            parent.Get<Tree<T>.Parent>()._children.Add(child);
        });

        // 对父实体记录中的子实体进行排序
        World.Query(in _parentDesc, (ref Tree<T>.Parent relationship)
            => relationship._children.Sort());
    }
}
