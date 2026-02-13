namespace OpenSolarMax.Game.Modding.Configuration;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public class SectionAttribute(string section) : Attribute
{
    public string Section => section;
}
