import os
import json
import argparse
import subprocess

import numpy as np
from scipy.stats import linregress
from scipy.signal import find_peaks
from jinja2 import Environment, FileSystemLoader
from jinja2 import Template


def run_unity_app(population_size, generations):
    """
    Run the Unity app with the specified population size and generations.
    """
    unity_app_path = os.path.join("CartPole", "CartPole.exe")
    if not os.path.exists(unity_app_path):
        print(f"Unity app not found at {unity_app_path}.")
        return

    try:
        proc = subprocess.Popen(
            [unity_app_path, f"-populationSize={population_size}", f"-generations={generations}"])
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
    training_history_path = os.path.join(
        "SavedSpecimen", "training_history.json")
    if not os.path.exists(training_history_path):
        print(f"Best specimen JSON file not found at {training_history_path}.")
        return
    report_data = {}
    # find top fitness
    with open(training_history_path, "r") as f:
        # [[0, 1, 2], [3, 4, 5], ...]
        training_history_data = json.load(f)
        # analyze best specimen
        best_fitness_list = get_best_fitness_list(training_history_data)
        report_data["fitness_data"] = best_fitness_list
        report_data["generation_labels"] = list(range(len(best_fitness_list)))
        report_best = analyze_data(best_fitness_list)
        report_data.update(report_best)
        # analyze per generation
        generations_report = []  # [{"generation": 0, "mean": 0, "std": 0}]
        for generation_index, generation in enumerate(training_history_data):
            # collect fitnesses to a list

            generation_report = analyze_data(generation)
            data = {
                "generation": generation_index,
                "mean": generation_report["mean"],
                "median": generation_report["median"],
                "std": generation_report["std"],
                "min": generation_report["min"],
                "max": generation_report["max"],
                "range": generation_report["range"]
            }
            generations_report.append(data)
        report_data["generations_report"] = generations_report
        # analyze trend
        trend_report = analyze_trend(best_fitness_list)
        report_data.update(trend_report)
        # analyze peaks
        peaks_report = analyze_peaks(best_fitness_list)
        report_data["peaks"] = peaks_report
        # analyze improvement rate
        improvement_rate_report = analyze_improvement_rate(
            best_fitness_list)
        report_data.update(improvement_rate_report)
        # dump report data to JSON file
        report_json_path = os.path.join("SavedSpecimen", "report.json")
        with open(report_json_path, "w") as f:
            json.dump(report_data, f, indent=4)
        print(f"Report data saved to {report_json_path}.")
        return report_data


def analyze_trend(fitness_data: list) -> dict:
    generations = range(len(fitness_data))
    slope, intercept, r_value, p_value, std_err = linregress(
        generations, fitness_data)
    trend = slope * np.array(generations) + intercept
    trend_string = ""
    if slope > 0:
        trend_string = "increasing"
    elif slope < 0:
        trend_string = "decreasing"
    else:
        trend_string = "constant"
    trend_data = {
        "slope": float(slope),
        "intercept": float(intercept),
        "r_squared": float(r_value ** 2),
        "p_value": float(p_value),
        "std_err": float(std_err),
        "trend": trend_string,
    }
    return trend_data


def analyze_peaks(fitness_data: list) -> list:
    """
    Analyze peaks in the fitness data.

    Returns a list of peak indices and their corresponding values.
    """
    peaks, _ = find_peaks(fitness_data, height=0)
    formatted_peaks = []
    for peak in peaks:
        formatted_peaks.append({
            "index": int(peak),
            "value": fitness_data[peak]
        })
    return formatted_peaks


def analyze_improvement_rate(fitness_data: list) -> dict:
    improvement_rate = np.diff(fitness_data)
    improvement_rate_mean = np.mean(improvement_rate)
    converted_improvement_rate = [float(i) for i in improvement_rate]
    improvement_rate_labels = list(range(1, len(improvement_rate)+1))
    return {
        "improvement_rate": converted_improvement_rate,
        "improvement_rate_labels": improvement_rate_labels,
        "improvement_rate_mean": float(improvement_rate_mean)
    }


def get_best_fitness_list(training_history_data) -> list:
    best_fitness_list = []
    for generation in training_history_data:
        best_fitness_list.append(max(generation))
    return best_fitness_list


def analyze_data(fitness_data: list) -> dict:
    best_mean = np.mean(fitness_data)
    best_median = np.median(fitness_data)
    best_std = np.std(fitness_data)
    best_min = np.min(fitness_data)
    best_max = np.max(fitness_data)
    best_range = best_max - best_min
    return {
        "mean": float(best_mean),
        "median": float(best_median),
        "std": float(best_std),
        "min": float(best_min),
        "max": float(best_max),
        "range": float(best_range)
    }


def parse_args():
    parser = argparse.ArgumentParser(
        description="Run Unity app and test saved specimens.")
    parser.add_argument("-p", "--population_size", type=int, default=50,
                        help="Population size for the Unity app.")
    parser.add_argument("-g", "--generations", type=int, default=50,
                        help="Number of generations for the Unity app.")
    return parser.parse_args()


def report_html(report_data, report_filename):
    # create a simple HTML report from report template using Jinja2
    env = Environment(loader=FileSystemLoader('.'))
    template = env.get_template('report_template.html')
    html_content = template.render(report_data=report_data)
    with open(report_filename, 'w') as f:
        f.write(html_content)
    print(f"Report generated: {report_filename}")


if __name__ == "__main__":

    args = parse_args()
    population_size = args.population_size
    generations = args.generations

    # run the unity app
    run_unity_app(population_size, generations)
    # run the test script
    report_data = run_test()
    report_html(report_data, "report.html")
