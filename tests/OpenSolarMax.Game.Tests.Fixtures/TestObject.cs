using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Declaration;

namespace OpenSolarMax.Game.Tests.Fixtures;

/// <summary>测试用组件：记录创建实体时由声明传入的名称。</summary>
public struct TestObjectComponent
{
    public string? Name;
}

[SchemaName("test-object")]
public class TestObjectDeclaration : IDeclaration<TestObjectDeclaration>
{
    public string? Name { get; set; }

    public TestObjectDeclaration Aggregate(TestObjectDeclaration newCfg)
    {
        return new TestObjectDeclaration() { Name = newCfg.Name ?? Name };
    }
}

[Translate("test-object", TestObjectConcept.Name)]
public class TestObjectDeclarationTranslator
    : ITranslator<TestObjectDeclaration, TestObjectDescription>
{
    public TestObjectDescription ToDescription(
        TestObjectDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        return new TestObjectDescription() { Name = declaration.Name };
    }
}

public static class TestObjectConcept
{
    public const string Name = "TestObject";
}

[Define(TestObjectConcept.Name)]
public class TestObjectDefinition : IDefinition
{
    public static Signature Signature { get; } = new(typeof(TestObjectComponent));
}

[Describe(TestObjectConcept.Name)]
public class TestObjectDescription : IDescription
{
    public string? Name { get; set; }
}

[Apply(TestObjectConcept.Name)]
public class TestObjectApplier : IApplier<TestObjectDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, TestObjectDescription desc)
    {
        commandBuffer.Set(entity, new TestObjectComponent { Name = desc.Name });
    }
}
