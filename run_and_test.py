import os
import json
import subprocess


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
    saved_specimen_dir = "SavedSpecimen"
    if not os.path.exists(saved_specimen_dir):
        print(f"Saved specimen directory not found at {saved_specimen_dir}.")
        return
    best_fintess = 0
    for file in os.listdir(saved_specimen_dir):
        if file.endswith(".json"):
            with open(os.path.join(saved_specimen_dir, file), 'r') as f:
                data = json.load(f)
                fitness = data.get('fitness', 0)
                if fitness > best_fintess:
                    best_fintess = fitness
    print(f"Best fitness from saved specimens: {best_fintess}")


if __name__ == "__main__":

    population_size = 50
    generations = 50

    # run the unity app
    run_unity_app(population_size, generations)
    # run the test script
    run_test()
