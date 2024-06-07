using System.Runtime.CompilerServices;
using Arch.Core;

namespace OpenSolarMax.Mods.Core;

public sealed class EntityReferenceComparer : IComparer<EntityReference>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(EntityReference x, EntityReference y) => x.Entity.CompareTo(y.Entity);
}
