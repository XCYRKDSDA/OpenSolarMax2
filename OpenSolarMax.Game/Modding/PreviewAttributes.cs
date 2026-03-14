namespace OpenSolarMax.Game.Modding;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class OnlyForPreviewAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class BothForGameplayAndPreviewAttribute : Attribute;

[Flags]
internal enum GameplayOrPreview
{
    Gameplay = 1 << 0,
    Preview = 1 << 1,
}
