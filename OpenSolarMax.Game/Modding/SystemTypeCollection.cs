namespace OpenSolarMax.Game.Modding;

internal class SystemTypeCollection
{
    public HashSet<Type> InputSystemTypes { get; set; } = [];

    public HashSet<Type> AiSystemTypes { get; set; } = [];

    public HashSet<Type> SimulateSystemTypes { get; set; } = [];

    public HashSet<Type> RenderSystemTypes { get; set; } = [];

    public HashSet<Type> PreviewSystemTypes { get; set; } = [];

    public void UnionWith(SystemTypeCollection theOther)
    {
        InputSystemTypes.UnionWith(theOther.InputSystemTypes);
        AiSystemTypes.UnionWith(theOther.AiSystemTypes);
        SimulateSystemTypes.UnionWith(theOther.SimulateSystemTypes);
        RenderSystemTypes.UnionWith(theOther.RenderSystemTypes);
        PreviewSystemTypes.UnionWith(theOther.PreviewSystemTypes);
    }
}
