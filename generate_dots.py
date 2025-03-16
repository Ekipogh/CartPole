import os

def generate_dot_file(saved_specimen_directory, file):
    sepcimen_graph = parse_specimen_data(saved_specimen_directory, file)
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
    with open(saved_specimen_directory + "/" + file.replace(".txt", ".dot"), "w") as f:
        f.write(dot_file_text)

def parse_specimen_data(saved_specimen_directory, file):
    sepcimen_graph = {"nodes": {}, "connections": {}}
    with open(saved_specimen_directory + "/" + file, "r") as f:
        lines = f.readlines()
        for line in lines:
            line = line.strip()
            if line.startswith("Node:"):
                # Node: 0 Input PassThrough
                node_id = int(line.split(" ")[1])
                node_type = line.split(" ")[2]
                node_function = line.split(" ")[3]
                sepcimen_graph["nodes"][node_id] = {"type": node_type, "function": node_function}
            elif line.startswith("Connection:"):
                # Connection: 0 5 -0.2858697 True
                connection_in_node = int(line.split(" ")[1])
                connection_out_node = int(line.split(" ")[2])
                connection_weight = float(line.split(" ")[3])
                connection_enabled = line.split(" ")[4] == "True"
                sepcimen_graph["connections"][(connection_in_node, connection_out_node)] = {"weight": connection_weight, "enabled": connection_enabled}
    return sepcimen_graph




def generate_dot_files(saved_specimen_directory):
    for file in os.listdir(saved_specimen_directory):
        if file.endswith(".txt"):
            generate_dot_file(saved_specimen_directory, file)

if __name__ == "__main__":
    saved_specimen_directory = "SavedSpecimen"
    if not os.path.exists(saved_specimen_directory):
        print("Error: SavedSpecimens directory does not exist.")
        print("Run the CartPole experiment first.")
        exit(1)
    else:
        generate_dot_files(saved_specimen_directory)