namespace OpenSolarMax.Ecs;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class OnlyForPreviewAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class BothForGameplayAndPreviewAttribute : Attribute;
