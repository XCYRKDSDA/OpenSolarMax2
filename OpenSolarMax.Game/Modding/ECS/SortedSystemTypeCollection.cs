using System.Collections.Immutable;

namespace OpenSolarMax.Game.Modding.ECS;

internal record ImmutableSortedSystemTypeCollection(
    ImmutableArray<Type> Input,
    ImmutableArray<Type> Ai,
    ImmutableArray<Type> Simulate,
    ImmutableArray<Type> Render
);
