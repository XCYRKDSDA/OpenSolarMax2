namespace OpenSolarMax.Game.Modding.UI;

internal class WidgetTypeCollection
{
    public HashSet<KeyValuePair<int, Type>> TopWidgetTypes { get; set; } = [];

    public HashSet<KeyValuePair<int, Type>> BottomWidgetTypes { get; set; } = [];

    public HashSet<KeyValuePair<int, Type>> LeftWidgetTypes { get; set; } = [];

    public HashSet<KeyValuePair<int, Type>> RightWidgetTypes { get; set; } = [];

    public void UnionWith(WidgetTypeCollection theOther)
    {
        TopWidgetTypes.UnionWith(theOther.TopWidgetTypes);
        BottomWidgetTypes.UnionWith(theOther.BottomWidgetTypes);
        LeftWidgetTypes.UnionWith(theOther.LeftWidgetTypes);
        RightWidgetTypes.UnionWith(theOther.RightWidgetTypes);
    }
}
