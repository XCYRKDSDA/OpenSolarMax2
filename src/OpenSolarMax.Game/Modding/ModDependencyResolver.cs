using System.Collections.Immutable;

namespace OpenSolarMax.Game.Modding;

internal static class ModDependencyResolver
{
    /// <summary>
    /// 把入口模组按依赖关系展开为「被依赖者在前」的加载顺序；无依赖关系的模组之间保持入口次序。
    /// </summary>
    /// <exception cref="InvalidOperationException">存在缺失的依赖或循环依赖</exception>
    public static ImmutableArray<BehaviorModInfo> Resolve(
        IEnumerable<string> entryPoints,
        IReadOnlyDictionary<string, BehaviorModInfo> available
    )
    {
        var done = new HashSet<string>(); // 已确定顺序的模组。同一模组只进结果一次，重复的依赖声明在此被消掉
        var onPath = new List<string>(); // 当前正在展开的依赖链。链上再次出现同一个名字即为环
        var order = ImmutableArray.CreateBuilder<BehaviorModInfo>(); // 结果，按「依赖在前」的顺序追加
        var problems = new List<string>(); // 解析过程中发现的问题。全部收集完再一次性抛出，避免只报出第一个
        var reported = new HashSet<string>(); // 问题文本去重，同一条问题只报一次

        // 后序遍历的显式栈。每个节点压两次：一次标记「待展开」，
        // 一次标记「子节点已处理完」（Expanded 为 true），后者出栈时把自身追加到结果
        var pending = new Stack<(string Name, bool Expanded)>();

        // 按入口顺序逐个展开，使无依赖关系的模组之间保持关卡声明的原有次序
        foreach (var entryPoint in entryPoints)
        {
            pending.Push((entryPoint, false));

            while (pending.Count > 0)
            {
                var (name, expanded) = pending.Pop();

                if (expanded)
                {
                    // 依赖都已入结果，此时 onPath 末尾正是自己
                    onPath.RemoveAt(onPath.Count - 1);
                    done.Add(name);
                    order.Add(available[name]);
                    continue;
                }

                if (done.Contains(name))
                    continue;

                // 依赖链上再次遇到自己：环
                var cycleStart = onPath.IndexOf(name);
                if (cycleStart >= 0)
                {
                    var cycle = $"循环依赖：{string.Join(" → ", onPath.Skip(cycleStart))} → {name}";
                    if (reported.Add(cycle))
                        problems.Add(cycle);
                    continue;
                }

                if (!available.TryGetValue(name, out var mod))
                {
                    var missing =
                        onPath.Count > 0
                            ? $"{onPath[^1]} 依赖的模组 {name} 不存在"
                            : $"入口声明的模组 {name} 不存在";
                    if (reported.Add(missing))
                        problems.Add(missing);
                    continue;
                }

                // 先把自己标记为「待收尾」，再逆序压入依赖：
                // 栈后进先出，逆序压入后弹出的顺序即依赖的声明顺序
                onPath.Add(name);
                pending.Push((name, true));
                foreach (var dependency in mod.Dependencies.Reverse())
                    pending.Push((dependency, false));
            }
        }

        if (problems.Count > 0)
            throw new InvalidOperationException(
                $"模组依赖解析失败：{Environment.NewLine}{string.Join(Environment.NewLine, problems)}"
            );

        return order.ToImmutable();
    }
}
