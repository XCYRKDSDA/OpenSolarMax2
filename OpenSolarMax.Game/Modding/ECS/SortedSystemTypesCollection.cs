using System.Collections.Immutable;

namespace OpenSolarMax.Game.Modding.ECS;

internal record ImmutableSortedSystemTypesCollection(
    ImmutableArray<Type> UpdateSystems,
    ImmutableArray<Type> LateUpdate1Systems,
    ImmutableArray<Type> LateUpdate2Systems
);

internal record StageSystemTypesCollection(
    ImmutableSortedSystemTypesCollection Input,
    ImmutableSortedSystemTypesCollection Ai,
    ImmutableSortedSystemTypesCollection Simulate,
    ImmutableSortedSystemTypesCollection Render
);
