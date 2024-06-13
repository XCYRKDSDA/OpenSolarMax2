using Arch.Core;
using OpenSolarMax.Game.Utils;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

public class RuntimeTemplate(params Type[] types) : ITemplate
{
    public Archetype Archetype { get; } = new(types);
    
    public void Apply(Entity entity) { }
}