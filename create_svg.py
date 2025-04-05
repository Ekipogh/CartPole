import os
import json
from graphviz import Digraph

# load SavedSpecimen/best.json
# covert to Graphviz dot format
# save as SVG file


def load_data(file_path) -> Digraph:
    with open(file_path, "r") as f:
        data = json.load(f)
    dot = Digraph(comment='Best Specimen')
    dot.attr(rankdir='LR', size='8,5')
    dot.attr('node', shape='box', style='filled', fillcolor='lightblue')
    for node in data["nodes"]:
        dot.node(
            str(node["id"]), label=f"{node['id']} {node['type']} {node['function']}")
    for connection in data["connections"]:
        in_node = str(connection["from"])
        out_node = str(connection["to"])
        weight = connection["weight"]
        enabled = connection["enabled"]
        style = "solid" if enabled else "dotted"
        dot.edge(in_node, out_node, label=str(weight), style=style)
    return dot


def save_to_svg(dot: Digraph, save_path: str) -> None:
    dot.render(save_path, format='svg', cleanup=True)
    print(f"SVG file saved to {save_path}")


if __name__ == "__main__":
    best_specimenfile = os.path.join("SavedSpecimen", "best.json")
    if not os.path.exists(best_specimenfile):
        print("Error: best.json file does not exist.")
        print("Run the CartPole experiment first.")
        exit(1)
    dot_data: Digraph = load_data(best_specimenfile)
    save_path = os.path.join("SavedSpecimen", "best_specimen")
    save_to_svg(dot_data, save_path)
