using Arch.Buffer;
using Arch.Core;

namespace OpenSolarMax.Game.Modding.Concept;

public interface IApplier
{
    void Apply(CommandBuffer commandBuffer, Entity entity);
}

public interface IDescriptionApplier;

public interface IApplier<in T> : IDescriptionApplier where T : IDescription
{
    void Apply(CommandBuffer commandBuffer, Entity entity, T desc);
}
