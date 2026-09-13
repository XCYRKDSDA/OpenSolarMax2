using System.Reflection;
using Microsoft.Extensions.Configuration;
using OpenSolarMax.Game.Modding.Configuration;

namespace OpenSolarMax.Game.Modding;

public static class PluginFactory
{
    /// <summary>
    /// 根据给定参数和选项，反射地创建指定插件类型
    /// </summary>
    /// <param name="pluginType">待实例化的插件的类型</param>
    /// <param name="fixedArguments">插件构造函数中固定的、必需的参数</param>
    /// <param name="dynamicArguments">插件构造函数中动态的、可选的参数</param>
    /// <returns></returns>
    public static object Instantiate(
        Type pluginType,
        IReadOnlyList<(Type Type, object Value)> fixedArguments,
        IReadOnlyDictionary<Type, object> dynamicArguments
    )
    {
        var constructorInfos = pluginType.GetConstructors(
            BindingFlags.Public | BindingFlags.Instance
        );
        if (constructorInfos.Length > 1)
            throw new Exception($"{pluginType} has more than one public constructors!");
        if (constructorInfos.Length == 0)
            throw new Exception($"{pluginType} has no public constructor!");
        var constructor = constructorInfos[0];
        var parameterInfos = constructor.GetParameters();

        var arguments = new object[parameterInfos.Length];
        var i = 0;

        // 填写固定参数
        if (parameterInfos.Length < fixedArguments.Count)
            throw new Exception(
                $"{pluginType}'s constructor lacks sufficient parameters for its fixed arguments"
            );
        for (; i < fixedArguments.Count; i++)
        {
            if (parameterInfos[i].ParameterType != fixedArguments[i].Type)
                throw new Exception(
                    $"{pluginType}'s constructor's parameters does not match its fixed arguments"
                );
            arguments[i] = fixedArguments[i].Value;
        }

        // 填写动态参数
        for (; i < parameterInfos.Length; i++)
        {
            if (parameterInfos[i].ParameterType == typeof(IConfiguration))
            {
                // 特殊处理 IConfiguration
                if (
                    parameterInfos[i].GetCustomAttribute<SectionAttribute>()
                    is not { } sectionAttribute
                )
                    throw new Exception(
                        "IConfiguration parameter must be declared with a Section attribute"
                    );
                var configurationRoot = (IConfigurationRoot)
                    dynamicArguments[typeof(IConfigurationRoot)];
                var configurationAggregator = new ConfigurationBuilder();
                foreach (var section in sectionAttribute.Section)
                    configurationAggregator.AddConfiguration(configurationRoot.GetSection(section));
                arguments[i] = configurationAggregator.Build();
            }
            else
                arguments[i] = dynamicArguments[parameterInfos[i].ParameterType];
        }

        return constructor.Invoke(arguments);
    }
}
