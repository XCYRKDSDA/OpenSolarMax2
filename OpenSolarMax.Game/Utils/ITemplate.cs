using Arch.Core;
using Nine.Assets;

namespace OpenSolarMax.Game.Utils;

/// <summary>
/// 实体模板接口。
/// 提供了初始化实体的方法
/// </summary>
public interface ITemplate
{
    Archetype Archetype { get; }

    void Apply(Entity entity);
}

public static class TemplateExtensions
{
    public static Entity Make(this World world, ITemplate template)
    {
        var entity = world.Construct(template.Archetype);
        template.Apply(entity);
        return entity;
    }
}
