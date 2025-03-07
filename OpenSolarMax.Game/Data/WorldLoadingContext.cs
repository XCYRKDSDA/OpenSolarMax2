﻿using Arch.Core;
using OpenSolarMax.Game.Utils;

namespace OpenSolarMax.Game.Data;

/// <summary>
/// 世界加载过程中的上下文。记录着当前的临时进展
/// </summary>
/// <param name="OtherEntities"></param>
public record class WorldLoadingContext(
    IReadOnlyDictionary<string, ITemplate[]> OtherTemplates,
    IReadOnlyDictionary<string, EntityReference> OtherEntities
);
