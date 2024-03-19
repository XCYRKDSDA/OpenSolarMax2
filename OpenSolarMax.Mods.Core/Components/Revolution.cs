﻿using Microsoft.Xna.Framework;
using Nine.Drawing;
using OpenSolarMax.Game.System;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 公转的模式
/// </summary>
public enum RevolutionMode
{
    TranslationAndRotation, //同时进行平动和转动
    TranslationOnly,        //只进行平动, 不进行转动
}

/// <summary>
/// 轨道组件。描述当前实体正在运行的轨道
/// </summary>
[Component]
public struct RevolutionOrbit()
{
    /// <summary>
    /// 轨道旋转。描述轨道圆所在坐标系相对星球坐标系的旋转
    /// </summary>
    public Quaternion Rotation = Quaternion.Identity;

    /// <summary>
    /// 轨道作为一个椭圆的横纵尺寸
    /// </summary>
    public SizeF Shape;

    /// <summary>
    /// 轨道上星球公转周期
    /// </summary>
    public float Period;

    /// <summary>
    /// 按当前轨道转动的模式
    /// </summary>
    public RevolutionMode Mode;
}

/// <summary>
/// 公转状态组件。描述当前实体绕其当前轨道公转的实时状态
/// </summary>
[Component]
public struct RevolutionState
{
    /// <summary>
    /// 当前相位
    /// </summary>
    public float Phase;
}
