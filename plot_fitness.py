import os
import json
import matplotlib.pyplot as plt

if __name__ == "__main__":
    saved_specimen_directory = "SavedSpecimen"
    if not os.path.exists(saved_specimen_directory):
        print("Error: SavedSpecimens directory does not exist.")
        print("Run the CartPole experiment first.")
        exit(1)
    best_fitness_values = []
    worst_fitness_values = []
    for file in os.listdir(saved_specimen_directory):
        if file.endswith(".json") and file != "best.json":
            with open(saved_specimen_directory + "/" + file, "r") as f:
                specimen_data = json.load(f)
                if "best" in file:
                    best_fitness_values.append(specimen_data["fitness"])
                if "worst" in file:
                    worst_fitness_values.append(specimen_data["fitness"])
    # Plot the fitness values
    plt.plot(best_fitness_values, label="Best Fitness")
    plt.plot(worst_fitness_values, label="Worst Fitness")
    plt.xlabel("Generation")
    plt.ylabel("Fitness")
    plt.legend()
    plt.show()
