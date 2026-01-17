namespace OpenSolarMax.Game.Modding;

internal record ImmutableSortedSystemTypeCollection(
    ImmutableSortedSystemTypes Input,
    ImmutableSortedSystemTypes Ai,
    ImmutableSortedSystemTypes Simulate,
    ImmutableSortedSystemTypes Render,
    ImmutableSortedSystemTypes Preview
);
