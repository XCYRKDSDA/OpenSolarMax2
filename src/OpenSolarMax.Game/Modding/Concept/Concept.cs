using System.Collections.Immutable;
using Arch.Core;
using OneOf;

namespace OpenSolarMax.Game.Modding.Concept;

public record Concept(
    string Name,
    Signature Signature,
    Type? DescriptionType,
    ImmutableArray<OneOf<IApplier, IDescriptionApplier>> Appliers
);
