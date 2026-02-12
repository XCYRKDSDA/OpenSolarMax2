namespace OpenSolarMax.Game.Modding.ECS;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class BeforeStructuralChangesAttribute : Attribute
{ }

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ReactToStructuralChangesAttribute : Attribute
{ }

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class AfterStructuralChangesAttribute : Attribute
{ }
