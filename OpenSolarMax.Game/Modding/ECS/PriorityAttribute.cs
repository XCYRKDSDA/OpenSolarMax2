namespace OpenSolarMax.Game.Modding.ECS;

/// <summary>
/// 系统优先级属性。优先级高的系统将更靠后执行
/// </summary>
/// <param name="priority"></param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class PriorityAttribute(int priority) : Attribute
{
    public int Value => priority;
}
