#!/usr/bin/env python3
"""检测 Graphviz DOT 格式有向图中的所有环。

依赖：pydot（解析 DOT）、networkx（环检测 - Johnson 算法）
用法：python detect_cycles <graph.dot>
"""

import sys
from pathlib import Path

import pydot
import networkx as nx


def parse_dot(filepath: str) -> nx.DiGraph:
    graphs = pydot.graph_from_dot_file(filepath)
    if not graphs:
        raise ValueError(f"无法从 {filepath} 解析出图")

    dot_graph = graphs[0]
    if dot_graph.get_type() != "digraph":
        raise ValueError("仅支持有向图（digraph），当前文件为无向图（graph）")

    G = nx.DiGraph()
    for edge in dot_graph.get_edges():
        src = edge.get_source()
        dst = edge.get_destination()
        G.add_edge(src, dst)

    return G


def find_cycles(G: nx.DiGraph) -> list[list[str]]:
    cycles = list(nx.simple_cycles(G))
    return [list(cycle) for cycle in cycles]


def main() -> None:
    if len(sys.argv) < 2:
        print(f"用法: {sys.argv[0]} <graph.dot>", file=sys.stderr)
        sys.exit(1)

    filepath = sys.argv[1]
    if not Path(filepath).exists():
        print(f"文件不存在: {filepath}", file=sys.stderr)
        sys.exit(1)

    G = parse_dot(filepath)

    print(f"节点数: {G.number_of_nodes()}")
    print(f"边数:   {G.number_of_edges()}\n")

    cycles = find_cycles(G)

    if not cycles:
        print("未检测到环。该图为有向无环图（DAG）。")
    else:
        print(f"检测到 {len(cycles)} 个环：")
        for i, cycle in enumerate(cycles, 1):
            path = " -> ".join(cycle + [cycle[0]])
            print(f"  [{i}] {path}")


if __name__ == "__main__":
    main()
