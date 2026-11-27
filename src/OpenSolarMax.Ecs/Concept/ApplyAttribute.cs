namespace OpenSolarMax.Ecs.Concept;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ApplyAttribute(string key) : Attribute
{
    public string Key => key;
}
