namespace OpenSolarMax.Ecs.Configuration;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public class SectionAttribute(params string[] section) : Attribute
{
    public string[] Section => section;
}
