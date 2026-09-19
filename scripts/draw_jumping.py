#!/usr/bin/env python3
"""绘制舰船跳跃的起点到目标点的向量。

输入 CSV 每行依次包含 8 列：id, x, y, z, tx, ty, tz, dt，其中 (x, y, z) 为起点，
(tx, ty, tz) 为目标点，dt 为该舰的抵达时间偏移（秒）。首行若为表头会被自动跳过。

绘图使用 3D 箭头（quiver），箭头按 dt 着色并附色标；色标以 0 为中心，用发散
色图区分早到（负）与晚到（正）。同时按三个方向的数据跨度设置显示盒的长宽高
比例，使 x/y/z 的单位长度在图上一致，避免坐标轴被拉伸。图内文字使用英文，
避免默认字体缺少中文字形而渲染成方块。

依赖：matplotlib（numpy 随 matplotlib 一并安装）
用法：python draw_jumping.py <input.csv> [output.png]
      提供 output.png 时保存图像，否则交互式显示。
"""

import csv
import sys
from pathlib import Path

import matplotlib.pyplot as plt
import numpy as np
from matplotlib.colors import TwoSlopeNorm

COLUMNS = ("id", "x", "y", "z", "tx", "ty", "tz", "dt")


def parse_csv(filepath: str) -> tuple[list[str], np.ndarray, np.ndarray, np.ndarray]:
    ids: list[str] = []
    starts: list[list[float]] = []
    targets: list[list[float]] = []
    offsets: list[float] = []
    seen_data = False

    with open(filepath, newline="", encoding="utf-8") as f:
        for line_no, raw in enumerate(csv.reader(f), 1):
            cells = [c.strip() for c in raw if c.strip() != ""]
            if not cells:
                continue

            if len(cells) != len(COLUMNS):
                raise ValueError(
                    f"第 {line_no} 行有 {len(cells)} 列，期望 {len(COLUMNS)} 列"
                )

            try:
                values = [float(c) for c in cells]
            except ValueError:
                # 首个非空行无法解析为数值时，视为表头
                if not seen_data:
                    continue
                raise ValueError(f"第 {line_no} 行含非数值内容：{raw}")

            seen_data = True
            ids.append(cells[0])
            starts.append(values[1:4])
            targets.append(values[4:7])
            offsets.append(values[7])

    if not starts:
        raise ValueError(f"{filepath} 中没有可用的数据行")

    return (
        ids,
        np.asarray(starts, dtype=float),
        np.asarray(targets, dtype=float),
        np.asarray(offsets, dtype=float),
    )


def draw_jumping(
    starts: np.ndarray, targets: np.ndarray, ids: list[str], offsets: np.ndarray
):
    vectors = targets - starts

    fig = plt.figure(figsize=(10, 8))
    ax = fig.add_subplot(111, projection="3d")

    # 以 0 为中心的对称取值范围，使 0 恰好落在发散色图的中点
    limit = max(float(np.max(np.abs(offsets))), 1e-6)
    norm = TwoSlopeNorm(vmin=-limit, vcenter=0.0, vmax=limit)

    arrows = ax.quiver(
        starts[:, 0],
        starts[:, 1],
        starts[:, 2],
        vectors[:, 0],
        vectors[:, 1],
        vectors[:, 2],
        linewidth=1.5,
        arrow_length_ratio=0.1,
    )
    arrows.set_array(offsets)
    arrows.set_cmap("coolwarm")
    arrows.set_norm(norm)

    ax.scatter(*starts.T, color="tab:green", s=18, label="departure")
    ax.scatter(*targets.T, color="tab:red", s=18, marker="x", label="target")

    for ship_id, point in zip(ids, starts):
        ax.text(point[0], point[1], point[2], f" {ship_id}", fontsize=7, color="0.4")

    # 等比例缩放：令显示盒三边长正比于数据跨度，则三个方向的单位长度一致
    spans = np.ptp(np.vstack([starts, targets]), axis=0)
    ax.set_box_aspect(tuple(float(s) for s in np.maximum(spans, 1e-6)))

    ax.set_xlabel("X")
    ax.set_ylabel("Y")
    ax.set_zlabel("Z")
    ax.legend()
    fig.colorbar(arrows, ax=ax, shrink=0.6, pad=0.1, label="dt (s)")

    return fig, ax


def main() -> None:
    if len(sys.argv) < 2:
        print(f"用法: {sys.argv[0]} <input.csv> [output.png]", file=sys.stderr)
        sys.exit(1)

    filepath = sys.argv[1]
    if not Path(filepath).exists():
        print(f"文件不存在: {filepath}", file=sys.stderr)
        sys.exit(1)

    ids, starts, targets, offsets = parse_csv(filepath)
    print(f"已解析 {len(ids)} 条跳跃记录")

    fig, _ = draw_jumping(starts, targets, ids, offsets)

    if len(sys.argv) >= 3:
        fig.savefig(sys.argv[2], dpi=150, bbox_inches="tight")
        print(f"图像已保存至: {sys.argv[2]}")
    else:
        plt.show()


if __name__ == "__main__":
    main()
