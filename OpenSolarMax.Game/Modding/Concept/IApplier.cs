using Arch.Buffer;
using Arch.Core;

namespace OpenSolarMax.Game.Modding.Concept;

public interface IApplier
{
    void Apply(CommandBuffer commandBuffer, Entity entity);
}

public interface IDescriptionApplier
{
    void Apply(CommandBuffer commandBuffer, Entity entity, IDescription desc);
}

public interface IApplier<in T> : IDescriptionApplier where T : IDescription
{
    void Apply(CommandBuffer commandBuffer, Entity entity, T desc);

    void IDescriptionApplier.Apply(CommandBuffer commandBuffer, Entity entity, IDescription desc) =>
        Apply(commandBuffer, entity, (T)desc);
}
