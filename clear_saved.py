import os

if __name__ == "__main__":
    saved_specimen_directory = "SavedSpecimen"
    if not os.path.exists(saved_specimen_directory):
        print("Nothing to clear.")
        exit(0)
    for file in os.listdir(saved_specimen_directory):
        if file != "best.json":
            os.remove(saved_specimen_directory + "/" + file)
