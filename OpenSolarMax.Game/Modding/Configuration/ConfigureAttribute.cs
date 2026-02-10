namespace OpenSolarMax.Game.Modding.Configuration;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ConfigureAttribute(string target) : Attribute
{
    public string Target => target;
}
