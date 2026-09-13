using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 标记胜利特效已触发。由 <see cref="GameOverSystem"/> 在首次触发胜利特效时创建，
/// 用于防止特效重复播放。实体常驻至关卡 World 销毁。
/// </summary>
[Component]
public struct VictoryEffectMarker;
