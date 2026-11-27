namespace OpenSolarMax.Ecs;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class UpdateAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class LateUpdateAttribute : Attribute { }
