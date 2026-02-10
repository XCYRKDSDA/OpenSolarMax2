using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Arch.Buffer;
using Arch.Core;
using OneOf;
using OpenSolarMax.Game.Utils;

namespace OpenSolarMax.Game.Modding.Concept;

internal class ConceptFactory : IConceptFactory
{
    private static Signature GetSignature(Type definitionType)
    {
        const BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy;
        return (Signature)definitionType.GetProperty("Signature", bindingFlags)!.GetValue(null)!;
    }

    private static OneOf<IApplier, IDescriptionApplier> CreateApplier(Type applierType, Type? descriptionType,
                                                                      IReadOnlyDictionary<Type, object> @params)
    {
        var constructorInfos = applierType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        if (constructorInfos.Length > 1)
            throw new Exception($"{applierType} has more than one public constructors!");
        if (constructorInfos.Length == 0)
            throw new Exception($"{applierType} has no public constructor!");
        var constructor = constructorInfos[0];

        var parameterInfos = constructor.GetParameters();
        var parameters = parameterInfos.Select(i => @params[i.ParameterType]).ToArray();

        var applier = constructor.Invoke(parameters);

        return descriptionType is null
                   ? OneOf<IApplier, IDescriptionApplier>.FromT0((IApplier)applier)
                   : OneOf<IApplier, IDescriptionApplier>.FromT1((IDescriptionApplier)applier);
    }

    private static Concept BakeConcept(ConceptInfo info, IReadOnlyDictionary<Type, object> @params)
    {
        return new Concept(
            info.Name,
            info.DefinitionTypes.Select(GetSignature).Aggregate((s1, s2) => s1 + s2),
            info.DescriptionType,
            [..info.ApplierTypes.Select(t => CreateApplier(t, info.DescriptionType, @params))]
        );
    }

    private readonly Dictionary<string, Concept> _concepts;

    private readonly Lookup<Type, string> _conceptNamesByDescriptionType;

    public ConceptFactory(IEnumerable<ConceptInfo> conceptInfos, IReadOnlyDictionary<Type, object> @params)
    {
        var params2 = new Dictionary<Type, object>(@params) { { typeof(IConceptFactory), this } }; // 添加自己
        _concepts = conceptInfos.Select(i => BakeConcept(i, params2)).ToDictionary(c => c.Name);
        _conceptNamesByDescriptionType = (Lookup<Type, string>)
            _concepts.Where(p => p.Value.DescriptionType is not null)
                     .ToLookup(p => p.Value.DescriptionType!, p => p.Value.Name);
    }

    public IReadOnlyDictionary<string, Concept> Concepts => _concepts;

    public Entity Make<T>(World world, CommandBuffer commandBuffer, string key, T description) where T : IDescription
    {
        var conceptTemplate = _concepts[key];
        Debug.Assert(typeof(T) == conceptTemplate.DescriptionType);

        var entity = world.Construct(commandBuffer, conceptTemplate.Signature);
        foreach (var applier in conceptTemplate.Appliers)
        {
            applier.Switch(
                a => a.Apply(commandBuffer, entity),
                a => ((IApplier<T>)a).Apply(commandBuffer, entity, description)
            );
        }

        return entity;
    }

    public Entity Make<T>(World world, CommandBuffer commandBuffer, T description) where T : IDescription
    {
        var matchedConceptNames = _conceptNamesByDescriptionType[typeof(T)].ToImmutableArray();
        if (matchedConceptNames.Length is 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(description));
        return Make(world, commandBuffer, matchedConceptNames[0], description);
    }

    public Entity Make(World world, CommandBuffer commandBuffer, string key, IDescription description)
    {
        var conceptTemplate = _concepts[key];
        Debug.Assert(description.GetType() == conceptTemplate.DescriptionType);

        var entity = world.Construct(commandBuffer, conceptTemplate.Signature);
        foreach (var applier in conceptTemplate.Appliers)
        {
            applier.Switch(
                a => a.Apply(commandBuffer, entity),
                a => a.Apply(commandBuffer, entity, description)
            );
        }

        return entity;
    }

    public Entity Make(World world, CommandBuffer commandBuffer, string key)
    {
        var conceptTemplate = _concepts[key];
        Debug.Assert(conceptTemplate.DescriptionType is null);

        var entity = world.Construct(commandBuffer, conceptTemplate.Signature);
        foreach (var applier in conceptTemplate.Appliers)
        {
            applier.Switch(
                a => a.Apply(commandBuffer, entity),
                _ => throw new Exception()
            );
        }

        return entity;
    }
}
