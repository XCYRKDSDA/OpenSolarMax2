namespace OpenSolarMax.Ecs.Concept;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DefineAttribute(string key) : Attribute
{
    public string Key => key;
}
