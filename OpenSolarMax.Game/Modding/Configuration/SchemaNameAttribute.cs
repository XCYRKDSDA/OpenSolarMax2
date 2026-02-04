namespace OpenSolarMax.Game.Modding.Configuration;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SchemaNameAttribute(string name) : Attribute
{
    public string Name => name;
}
