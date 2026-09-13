namespace OpenSolarMax.Game.Modding.Declaration;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class TranslateAttribute(string schemaName, string conceptName) : Attribute
{
    public string SchemaName => schemaName;

    public string ConceptName => conceptName;
}
