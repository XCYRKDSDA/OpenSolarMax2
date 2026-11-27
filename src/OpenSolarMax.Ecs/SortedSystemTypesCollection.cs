using System.Collections.Immutable;

namespace OpenSolarMax.Ecs;

public record ImmutableSortedSystemTypesCollection(
    ImmutableArray<Type> UpdateSystems,
    ImmutableArray<Type> LateUpdate1Systems,
    ImmutableArray<Type> LateUpdate2Systems,
    ImmutableArray<Type> ReactiveSystems
);

public record StageSystemTypesCollection(
    ImmutableSortedSystemTypesCollection Simulate,
    ImmutableSortedSystemTypesCollection Render
);
