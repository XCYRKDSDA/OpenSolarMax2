using Arch.Buffer;
using Arch.Core;

namespace OpenSolarMax.Game.Utils;

/// <summary>
/// 实体模板接口。
/// 提供了初始化实体的方法
/// </summary>
public interface ITemplate
{
    Signature Signature { get; }

    void Apply(CommandBuffer commandBuffer, Entity entity);
}

public static class TemplateExtensions
{
    public static Entity Make(this World world, CommandBuffer commandBuffer, ITemplate template)
    {
        var entity = world.Construct(commandBuffer, template.Signature);
        template.Apply(commandBuffer, entity);
        return entity;
    }
}
