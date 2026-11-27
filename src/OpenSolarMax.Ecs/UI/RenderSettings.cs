namespace OpenSolarMax.Ecs.UI;

public struct RenderSettings()
{
    /// <summary>
    /// 每个纹理的缩放. 主要用于预览时的过渡
    /// </summary>
    public float SpriteScaling = 1;

    /// <summary>
    /// 整体不透明度
    /// </summary>
    public float Opacity = 1;

    /// <summary>
    /// 是否启用裁剪测试, 把绘制限制在设备裁剪矩形内
    /// </summary>
    public bool ScissorTestEnable = false;
}
