namespace OpenSolarMax.Game.Modding.ECS;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class HookAttribute(string name) : Attribute
{
    public string Name => name;
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class HookOnAttribute(string hook) : Attribute
{
    public string Hook => hook;
}
