import os
import json
import argparse
import subprocess

import numpy as np
from scipy.stats import linregress
from scipy.signal import find_peaks


def run_unity_app(population_size, generations):
    """
    Run the Unity app with the specified population size and generations.
    """
    unity_app_path = os.path.join("CartPole", "CartPole.exe")
    if not os.path.exists(unity_app_path):
        print(f"Unity app not found at {unity_app_path}.")
        return

    try:
        proc = subprocess.Popen([unity_app_path, "-populationSize",
                                str(population_size), "-generations", str(generations)])
        proc.wait()  # Wait for the process to complete
        if proc.returncode != 0:
            print(f"Unity app exited with code {proc.returncode}.")
        else:
            print("Unity app ran successfully.")
    except FileNotFoundError:
        print(f"Unity app executable not found at {unity_app_path}.")
    except subprocess.CalledProcessError as e:
        print(f"Error running Unity app: {e}")
    except Exception as e:
        print(f"Failed to start Unity app: {e}")


def run_test():
    best_json_path = os.path.join("SavedSpecimen", "best.json")
    if not os.path.exists(best_json_path):
        print(f"Best specimen JSON file not found at {best_json_path}.")
        return
    best_fitness = json.load(open(best_json_path, "r"))["fitness"]
    print(f"Best fitness from JSON file: {best_fitness}")
    # collect fiteness of all specimens
    fitness_data = []
    for filename in os.listdir("SavedSpecimen"):
        if filename.endswith("_best.json") and filename != "best.json":
            with open(os.path.join("SavedSpecimen", filename), "r") as f:
                data = json.load(f)
                fitness_data.append(data["fitness"])
    analyze_fitness(fitness_data)
    analyze_trend(fitness_data)
    detect_peaks(fitness_data)


def analyze_fitness(fitness_data):
    """
    Analyze the fitness data of the specimens.
    """
    fitness_data = np.array(fitness_data)
    mean_fitness = np.mean(fitness_data)
    median_fitness = np.median(fitness_data)
    std_fitness = np.std(fitness_data)
    min_fitness = np.min(fitness_data)
    max_fitness = np.max(fitness_data)

    print("Fitness Analysis:")
    print(f"Mean: {mean_fitness}")
    print(f"Median: {median_fitness}")
    print(f"Standard Deviation: {std_fitness}")
    print(f"Minimum: {min_fitness}")
    print(f"Maximum: {max_fitness}")


def analyze_trend(fitness_data):
    generations = range(len(fitness_data))
    slope, intercept, r_value, p_value, std_err = linregress(
        generations, fitness_data)
    print(f"Slope: {slope}, R-squared: {r_value**2}")
    if slope > 0:
        print("Fitness is improving over generations.")
    elif slope < 0:
        print("Fitness is declining over generations.")
    else:
        print("No significant trend.")


def detect_peaks(fitness_data):
    peaks, _ = find_peaks(fitness_data, height=0)
    print(f"Detected peaks at generations: {peaks}")
    for peak in peaks:
        print(f"Peak fitness at generation {peak}: {fitness_data[peak]}")


def parse_args():
    parser = argparse.ArgumentParser(
        description="Run Unity app and test saved specimens.")
    parser.add_argument("-p", "--population_size", type=int, default=50,
                        help="Population size for the Unity app.")
    parser.add_argument("-g", "--generations", type=int, default=50,
                        help="Number of generations for the Unity app.")
    return parser.parse_args()


if __name__ == "__main__":

    args = parse_args()
    population_size = args.population_size
    generations = args.generations

    # run the unity app
    run_unity_app(population_size, generations)
    # run the test script
    run_test()
