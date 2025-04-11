using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using System;
using Random = UnityEngine.Random;

public class CartNeatController : MonoBehaviour
{
    // NEAT settings
    private float _randomBias;

    private const int _inputSize = 5;
    private const int _outputSize = 1;

    // genetic algorithm settings
    private Dictionary<CartNeat, CartAndPole> _currentGeneration;
    private List<CartNeat> _deadSpecimens = new();
    private int _maxGenerations = 50;
    private int _currentGenerationIndex = 0;
    private int _populationSize = 50; // number of specimens in the current generation
    private bool _currentGenerationIsFinished = false;

    private const int _championSize = 5; // number of specimens that will be preserved in the next generation
    private const int _antichampionSize = 1; // number of worst specimens that will be saved in the next generation
    private const int _newSpeciesSize = 5; // number of new species re-introduced in the next generation

    private CartNeat absoluteBestSpecimen = null; // the best specimen of all generations

    public StatisticsSO statisticsSO;
    public NodeSO nodeSO;

    public CartAndPole cartAndPolePrefab;

    public FollowCamera mainCamera;

    // history
    private List<List<float>> _trainingHistory = new();

    void Start()
    {
        ParseArgs();
        InitGeneration();
        ResetStatistics();
    }

    // Update is called once per frame
    void Update()
    {
        ManageTraining();
        CartNeatThink();
        UpdateCamera();
        Statistics();
    }

    private void ParseArgs()
    {
        // Parse command line arguments
        var args = System.Environment.GetCommandLineArgs();
        foreach (var arg in args)
        {
            if (arg.StartsWith("-populationSize="))
            {
                if (int.TryParse(arg[16..], out int populationSize))
                {
                    _populationSize = populationSize;
                }
            }
            else if (arg.StartsWith("-maxGenerations="))
            {
                if (int.TryParse(arg[16..], out int maxGenerations))
                {
                    _maxGenerations = maxGenerations;
                }
            }
        }
    }

    private void UpdateCamera()
    {
        if (_currentGeneration.Count > 0)
        {
            var cartToFollow = _currentGeneration.First().Value.cart;
            mainCamera.Target = cartToFollow.transform;
        }
    }

    private void InitGeneration()
    {
        Debug.Log($"Population size: {_populationSize}");
        Debug.Log($"Max generations: {_maxGenerations}");
        _currentGeneration = new Dictionary<CartNeat, CartAndPole>();
        _randomBias = Random.Range(-1.0f, 1.0f);

        // Attempt to load the best specimen from a saved file
        var bestSpecimenLoaded = LoadBest();

        // Initialize the population
        for (int i = bestSpecimenLoaded ? 1 : 0; i < _populationSize; i++)
        {
            // Create a new NEAT specimen
            var newSpecimen = new CartNeat(_inputSize, _outputSize)
            {
                Id = i // Assign a unique ID to the specimen
            };

            var cartAndPole = InstantiateCartAndPole(i);

            // Randomize the initial rotation of the pole
            var poleRotation = RandomizeRotation();
            cartAndPole.pole.transform.rotation = poleRotation;

            // Assign a unique number to the cart
            cartAndPole.cart.SetNumber(i);

            // Add the specimen and its associated GameObject to the current generation
            _currentGeneration.Add(newSpecimen, cartAndPole);
        }
        EnableCartPolePhysics();
        UpdateCamera();
    }

    private void ManageTraining()
    {
        List<CartNeat> specimensToRemove = new();
        if (_currentGenerationIndex < _maxGenerations)
        {
            // evolve the generation if the current generation is finished
            if (_currentGenerationIsFinished)
            {
                UpdateHistory();
                Evolution();
                EnableCartPolePhysics();
            }
            else
            {
                if (_currentGeneration.Count == 0)
                {
                    _currentGenerationIsFinished = true;
                    return;
                }
                foreach (var kvp in _currentGeneration)
                {
                    var specimen = kvp.Key;
                    var cartAndPole = kvp.Value;
                    if (specimen.IsDead)
                    {
                        specimensToRemove.Add(specimen);
                    }
                }
            }
        }
        else
        {
            // Training is finished
            Debug.Log($"Training finished in {_maxGenerations} generations.");
            Debug.Log($"Best specimen fitness: {absoluteBestSpecimen.Fitness}");
            // Save training history
            SaveTrainingHistory();
            // Quit the application
#if UNITY_EDITOR
            EditorApplication.isPlaying = false; // Stops play mode in the Unity Editor
#else
            Application.Quit(); // Quits the application in a built version
#endif
            return;
        }
        if (specimensToRemove.Count > 0)
        {
            _deadSpecimens.AddRange(specimensToRemove);
            foreach (var specimen in specimensToRemove)
            {
                var cartAndPole = _currentGeneration[specimen];
                Destroy(cartAndPole.gameObject);
                _currentGeneration.Remove(specimen);
            }
        }
    }

    void CartNeatThink()
    {
        var first = true;
        foreach (var kvp in _currentGeneration)
        {
            var specimen = kvp.Key;
            var cartAndPole = kvp.Value;
            if (!CheckForDeath(cartAndPole))
            {
                var cart = cartAndPole.cart;
                var pole = cartAndPole.pole;
                var inputs = new float[_inputSize];
                // Calculate the angle of the pole relative to the vertical axis
                var angle = pole.transform.rotation.eulerAngles.z;
                // translate the angle to the range [-180, 180]
                if (angle > 180)
                {
                    angle -= 360;
                }

                inputs[0] = angle;
                // Calculate the relative x position of the pole's bottom point to the cart
                var poleSlide = pole.poleBottomPoint.position.x - cart.transform.position.x;
                inputs[1] = poleSlide;
                // Cart x position
                var cartX = cart.transform.position.x;
                inputs[2] = cartX;
                // Pole height
                var poleHeight = pole.poleTopPoint.position.y;
                inputs[3] = poleHeight;
                // Include a random bias in the inputs
                inputs[4] = _randomBias;

                // output[0] sigmoid value between 0 and 1
                var outputs = specimen.Evaluate(inputs);
                // move value between -1 and 1
                var move = outputs[0] * 2 - 1;

                if (first)
                {
                    nodeSO.SetInputs(inputs);
                    nodeSO.SetMove(move);
                    first = false;
                }

                cart.moveAmount += Mathf.Abs(move);
                cart.Move(new Vector2(move, 0));
                specimen.SetPoleAngle(angle);
                specimen.UpdateDistance(cartX);
            }
            else
            {
                specimen.Dead();
            }
        }
    }

    private bool CheckForDeath(CartAndPole cartAndPole)
    {
        var pole = cartAndPole.pole;
        if (pole.poleTopPoint.position.y < pole.poleMiddlePoint.position.y)
        {
            return true;
        }
        if (pole.IsFallen())
        {
            return true;
        }
        return false;
    }

    public void Evolution()
    {
        // Sort dead specimens by fitness (descending order)
        _deadSpecimens.Sort((x, y) => y.Fitness.CompareTo(x.Fitness));

        // Update the absolute best specimen
        UpdateAbsoluteBestSpecimen();

        // Save the best specimen of the current generation
        SaveBestSpecimen();

        // Create a new generation
        var newGeneration = new Dictionary<CartNeat, CartAndPole>();

        // Add champions to the new generation
        AddSpecimensToNewGeneration(newGeneration, _deadSpecimens.Take(_championSize).ToList(), 0);

        // Add antichampions to the new generation
        AddSpecimensToNewGeneration(newGeneration, _deadSpecimens.Skip(_deadSpecimens.Count - _antichampionSize).ToList(), _championSize);

        // Add new species to the new generation
        AddNewSpeciesToGeneration(newGeneration);

        // Add offspring from crossover to the new generation
        AddOffspringToGeneration(newGeneration);

        // Reset champions and antichampions
        ResetSpecimens(_deadSpecimens.Take(_championSize).ToList());
        ResetSpecimens(_deadSpecimens.Skip(_deadSpecimens.Count - _antichampionSize).ToList());

        // Update the current generation
        _deadSpecimens.Clear();
        _currentGeneration = newGeneration;
        _currentGenerationIsFinished = false;
        _currentGenerationIndex++;
    }

    private void UpdateAbsoluteBestSpecimen()
    {
        if (absoluteBestSpecimen == null || _deadSpecimens.First().Fitness > absoluteBestSpecimen.Fitness)
        {
            absoluteBestSpecimen = _deadSpecimens.First();
        }
    }

    private void SaveBestSpecimen()
    {
        if (_deadSpecimens.Count > 0)
        {
            var bestFileName = $"generation_{_currentGenerationIndex}_best";
            _deadSpecimens.First().Save(bestFileName);
        }
    }

    private void AddSpecimensToNewGeneration(Dictionary<CartNeat, CartAndPole> newGeneration, List<CartNeat> specimens, int startIndex)
    {
        for (int i = 0; i < specimens.Count; i++)
        {
            var specimen = specimens[i];
            var cartAndPole = InstantiateCartAndPole(startIndex + i);

            // Randomize the initial rotation of the pole
            cartAndPole.pole.transform.rotation = RandomizeRotation();

            // Set the cart number and reset the specimen
            cartAndPole.cart.SetNumber(startIndex + i);
            specimen.IsDead = false;
            specimen.Id = startIndex + i;

            newGeneration.Add(specimen, cartAndPole);
        }
    }

    private void AddNewSpeciesToGeneration(Dictionary<CartNeat, CartAndPole> newGeneration)
    {
        for (int i = 0; i < _newSpeciesSize; i++)
        {
            var newSpecimen = new CartNeat(_inputSize, _outputSize)
            {
                Id = _championSize + _antichampionSize + i
            };

            var cartAndPole = InstantiateCartAndPole(newSpecimen.Id);

            // Randomize the initial rotation of the pole
            cartAndPole.pole.transform.rotation = RandomizeRotation();

            // Set the cart number
            cartAndPole.cart.SetNumber(newSpecimen.Id);

            newGeneration.Add(newSpecimen, cartAndPole);
        }
    }

    private void AddOffspringToGeneration(Dictionary<CartNeat, CartAndPole> newGeneration)
    {
        int j = 0;
        for (int i = _championSize + _antichampionSize + _newSpeciesSize; i < _populationSize; i++)
        {
            // Select parents for crossover
            var parent1 = _deadSpecimens[j % _championSize];
            var parent2 = _deadSpecimens[(j + 1) % _championSize];

            // Perform crossover to create a child
            var child = parent1.Crossover<CartNeat>(parent2);
            child.Id = i;

            var cartAndPole = InstantiateCartAndPole(i);

            // Randomize the initial rotation of the pole
            cartAndPole.pole.transform.rotation = RandomizeRotation();

            // Set the cart number
            cartAndPole.cart.SetNumber(i);

            newGeneration.Add(child, cartAndPole);
            j++;
        }
    }

    private void ResetSpecimens(List<CartNeat> specimens)
    {
        foreach (var specimen in specimens)
        {
            specimen.Reset();
        }
    }

    private CartAndPole InstantiateCartAndPole(int i)
    {
        var position = new Vector3(0, 0, i);
        var cartAndPole = Instantiate(cartAndPolePrefab, position, Quaternion.identity);
        cartAndPole.name = "CartAndPole" + i;
        var cart = cartAndPole.cart;
        var pole = cartAndPole.pole;
        cart.ownPole = pole;
        pole.ownCart = cart;
        cart.name = "Cart" + i;
        pole.name = "Pole" + i;
        return cartAndPole;
    }

    private void Statistics()
    {
        statisticsSO.SetGeneration(_currentGenerationIndex);
        if (_deadSpecimens.Count == 0) return;
        // Calculate continuous statistics
        float totalFitness = 0;
        float maxFitness = 0;
        foreach (var specimen in _deadSpecimens)
        {
            totalFitness += specimen.Fitness;
            if (specimen.Fitness > maxFitness)
            {
                maxFitness = specimen.Fitness;
            }
        }

        float averageFitness = totalFitness / _deadSpecimens.Count;

        statisticsSO.SetAverageFitness(averageFitness);
        statisticsSO.AttemptToSetMaxFitness(maxFitness);
        statisticsSO.SetLastSpecimenFitness(_deadSpecimens.Last().Fitness);
        statisticsSO.SetCurrentPopulation(_currentGeneration.Count);
    }

    private void ResetStatistics()
    {
        statisticsSO.ResetStatistics();
    }

    private bool LoadBest()
    {
        const string bestFilePath = "SavedSpecimen/best.json";
        if (System.IO.File.Exists(bestFilePath))
        {
            CartNeat best = Neat.Load<CartNeat>(bestFilePath);
            best.Id = 0; // Set the ID to 0 for the loaded specimen
            var cartAndPole = InstantiateCartAndPole(0);
            // Randomize the initial rotation of the pole
            var poleRotation = RandomizeRotation();
            cartAndPole.pole.transform.rotation = poleRotation;
            // set the cart number to the best specimen index
            cartAndPole.cart.SetNumber(0);
            _currentGeneration.Add(best, cartAndPole);
            return true;
        }
        return false;
    }

    private Quaternion RandomizeRotation()
    {
        return Quaternion.Euler(0, 0, Random.Range(-10f, 10f));
    }

    private void EnableCartPolePhysics()
    {
        // cart and pole spawned disabled
        // enable every cart and pole to setup collisions
        foreach (var kvp in _currentGeneration)
        {
            var cartAndPole = kvp.Value;
            var pole = cartAndPole.pole;
            cartAndPole.gameObject.SetActive(true);
            pole.GetRigidbody().simulated = true;
        }
    }

    void OnApplicationQuit()
    {
        // Save the best specimen to a file
        var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string bestFileName = $"quit_{timestamp}";
        if (absoluteBestSpecimen != null)
        {
            absoluteBestSpecimen.Save(bestFileName);
        }
        else
        {
            Debug.LogWarning("No best specimen to save on quit.");
        }
    }

    private void UpdateHistory()
    {
        // update the training history with the current generation fitnesses
        var specimenSortedById = _deadSpecimens.OrderBy(x => x.Id).ToList();
        var fitnesses = specimenSortedById.Select(x => x.Fitness).ToList();
        // save the best fitness of the current generation
        var bestFitness = fitnesses.Max();
        Console.WriteLine($"Best fitness of generation {_currentGenerationIndex}: {bestFitness}");
        _trainingHistory.Add(fitnesses);
    }

    private void SaveTrainingHistory()
    {
        var savedSpecimenDirectory = "SavedSpecimen";
        // Check if the directory exists, if not create it
        if (!System.IO.Directory.Exists(savedSpecimenDirectory))
        {
            System.IO.Directory.CreateDirectory(savedSpecimenDirectory);
        }
        // Save the training history to a file
        var json = JsonConvert.SerializeObject(_trainingHistory, Formatting.Indented);
        var filePath = System.IO.Path.Combine(savedSpecimenDirectory, "training_history.json");
        System.IO.File.WriteAllText(filePath, json);
        Debug.Log($"Training history saved to {filePath}");
    }
}
