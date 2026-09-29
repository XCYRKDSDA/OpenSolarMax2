using System.Collections.Immutable;

namespace OpenSolarMax.Game.Modding.ECS;

public record ImmutableSortedSystemTypesCollection(
    ImmutableArray<Type> UpdateSystems,
    ImmutableArray<Type> LateUpdate1Systems,
    ImmutableArray<Type> LateUpdate2Systems,
    ImmutableArray<Type> ReactiveSystems
);

public record StageSystemTypesCollection(
    ImmutableSortedSystemTypesCollection Input,
    ImmutableSortedSystemTypesCollection Ai,
    ImmutableSortedSystemTypesCollection Simulate,
    ImmutableSortedSystemTypesCollection Render
);
