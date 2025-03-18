import os
import json


def generate_dot_file(saved_specimen_directory, file):
    sepcimen_graph = parse_specimen_data_json(saved_specimen_directory, file)
    dot_file_text = "digraph G {\n"
    dot_file_text += "rankdir=LR\n"
    for node_id in sepcimen_graph["nodes"]:
        node = sepcimen_graph["nodes"][node_id]
        node_type = node["type"]
        node_function = node["function"]
        if node_type == "Input":
            dot_file_text += f"{node_id} [label=\"{node_id} {node_type}\"]\n"
        else:
            dot_file_text += f"{node_id} [label=\"{node_id} {node_type} {node_function}\"]\n"
    for connection in sepcimen_graph["connections"]:
        connection_in_node = connection[0]
        connection_out_node = connection[1]
        connection_weight = sepcimen_graph["connections"][connection]["weight"]
        connection_enabled = sepcimen_graph["connections"][connection]["enabled"]
        style = "solid" if connection_enabled else "dotted"
        dot_file_text += f"{connection_in_node} -> {connection_out_node} [label=\"{connection_weight}\"] [style=\"{style}\"]\n"
    dot_file_text += "}"
    with open(saved_specimen_directory + "/" + file.replace(".json", ".dot"), "w") as f:
        f.write(dot_file_text)


def parse_specimen_data_json(saved_specimen_directory, file):
    sepcimen_graph = {"nodes": {}, "connections": {}}
    with open(saved_specimen_directory + "/" + file, "r") as f:
        specimen_data = json.load(f)
        for node in specimen_data["nodes"]:
            node_id = node["id"]
            node_type = node["type"]
            node_function = node["function"]
            sepcimen_graph["nodes"][node_id] = {
                "type": node_type, "function": node_function}
        for connection in specimen_data["connections"]:
            connection_in_node = connection["from"]
            connection_out_node = connection["to"]
            connection_weight = connection["weight"]
            connection_enabled = connection["enabled"]
            sepcimen_graph["connections"][(connection_in_node, connection_out_node)] = {
                "weight": connection_weight, "enabled": connection_enabled}
    return sepcimen_graph


def generate_dot_files(saved_specimen_directory):
    for file in os.listdir(saved_specimen_directory):
        if file.endswith(".json"):
            generate_dot_file(saved_specimen_directory, file)


if __name__ == "__main__":
    saved_specimen_directory = "SavedSpecimen"
    if not os.path.exists(saved_specimen_directory):
        print("Error: SavedSpecimens directory does not exist.")
        print("Run the CartPole experiment first.")
        exit(1)
    else:
        generate_dot_files(saved_specimen_directory)
