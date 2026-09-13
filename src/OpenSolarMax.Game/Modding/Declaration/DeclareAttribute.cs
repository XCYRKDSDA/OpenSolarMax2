namespace OpenSolarMax.Game.Modding.Declaration;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class DeclareAttribute(string target) : Attribute
{
    public string Target => target;
}
