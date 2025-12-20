namespace OpenSolarMax.Game.Modding;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ConfigurationSectionAttribute(string section) : Attribute
{
    public string Section => section;
}
