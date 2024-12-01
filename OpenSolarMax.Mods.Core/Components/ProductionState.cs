﻿using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 生产状态组件。描述星球当前的生产进度
/// </summary>
[Component]
public struct ProductionState
{
    /// <summary>
    /// 当前单位的生产进度
    /// </summary>
    public float Progress;
}