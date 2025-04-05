import os
import json
import shutil

if __name__ == "__main__":
    saved_specimen_directory = "SavedSpecimen"
    best_specimenfile = None
    if not os.path.exists(saved_specimen_directory):
        print("Error: SavedSpecimens directory does not exist.")
        print("Run the CartPole experiment first.")
        exit(1)
    best_fitness = -1
    for file in os.listdir(saved_specimen_directory):
        if file.endswith("_best.json") and file != "best.json":
            with open(saved_specimen_directory + "/" + file, "r") as f:
                specimen_data = json.load(f)
                if float(specimen_data["fitness"]) > best_fitness:
                    best_fitness = specimen_data["fitness"]
                    best_specimenfile = file
    print(f"Best specimen file: {best_specimenfile}")
    if best_specimenfile is None:
        print("Error: No valid specimen file found.")
        exit(1)
    if os.path.exists(saved_specimen_directory + "/best.json"):
        with open(saved_specimen_directory + "/best.json", "r") as f:
            best_json_data = json.load(f)
            if best_json_data["fitness"] >= best_fitness:
                print("Best specimen file already exists.")
                exit(0)
    # copy best specimen file as best.json
    shutil.copyfile(saved_specimen_directory + "/" +
                    best_specimenfile, saved_specimen_directory + "/" + "best.json")
