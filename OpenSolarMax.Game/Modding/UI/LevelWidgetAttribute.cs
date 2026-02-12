namespace OpenSolarMax.Game.Modding.UI;

public enum LevelWidgetPosition
{
    Top,
    Bottom,
    Left,
    Right,
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class LevelWidgetAttribute(LevelWidgetPosition position, int order) : Attribute
{
    public LevelWidgetPosition Position => position;

    public int Order => order;
}
