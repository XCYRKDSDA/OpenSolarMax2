using System.Collections.Immutable;

namespace OpenSolarMax.Game.Modding.ECS;

internal record ImmutableSortedSystemTypesCollection(
    ImmutableArray<Type> UpdateSystems,
    ImmutableArray<Type> PreStructuralChangeSystems,
    ImmutableArray<Type> StructuralChangeSystems,
    ImmutableArray<Type> PostStructuralChangeSystems
);

internal record StageSystemTypesCollection(
    ImmutableSortedSystemTypesCollection Input,
    ImmutableSortedSystemTypesCollection Ai,
    ImmutableSortedSystemTypesCollection Simulate,
    ImmutableSortedSystemTypesCollection Render
);
