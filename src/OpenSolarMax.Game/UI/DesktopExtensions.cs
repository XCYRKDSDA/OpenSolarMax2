using Myra.Graphics2D.UI;

namespace OpenSolarMax.Game.UI;

internal static class DesktopExtensions
{
    /// <summary>
    /// 把 Desktop 的输入快照对齐到当前状态，不向任何控件派发事件。
    /// 界面被压入界面栈闲置期间，其 Desktop 不处理输入，空窗期积攒的滚轮累计量与按键边沿
    /// 会在恢复处理的首帧被算成本次输入；在界面恢复参与合成之前调用本方法，可以把快照
    /// 推进到当前状态、丢弃积压。调用期间键盘焦点会被临时摘除，避免按键边沿落到焦点控件上
    /// </summary>
    public static void RefreshInputSnapshot(this Desktop desktop)
    {
        var focus = desktop.FocusedKeyboardWidget;
        desktop.FocusedKeyboardWidget = null;
        desktop.UpdateInput();
        desktop.FocusedKeyboardWidget = focus;
    }
}
