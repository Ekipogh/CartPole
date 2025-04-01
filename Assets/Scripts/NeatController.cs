using System.Collections.Generic;
using UnityEngine;

public class NeatController : MonoBehaviour
{
    // NEAT settings
    private float _randomBias;

    private const int _inputSize = 5;
    private const int _outputSize = 1;

    // genetic algorithm settings
    private Dictionary<Neat, CartAndPole> _currentGeneration;
    private List<Neat> _deadSpecimens = new();
    private const int _maxGenerations = 50;
    private int _currentGenerationIndex = 0;
    private const int _populationSize = 50; // number of specimens in the current generation
    //private int _currentSpecimenIndex = 0;
    //private bool _currentSpecimenIsDead = false;
    private bool _currentGenerationIsFinished = false;

    private const int _championSize = 5; // number of specimens that will be preserved in the next generation
    private const int _antichampionSize = 1; // number of worst specimens that will be saved in the next generation

    // statistics
    private List<float> _averageFitness = new();
    private List<float> _maxFitness = new();

    public StatisticsSO statisticsSO;
    public NodeSO nodeSO;

    public CartAndPole cartAndPolePrefab;

    void Start()
    {
        InitGeneration();
        ResetStatistics();
    }

    // Update is called once per frame
    void Update()
    {
        ManageTraining();
        NeatThink();
    }

    private void InitGeneration()
    {
        _currentGeneration = new Dictionary<Neat, CartAndPole>();
        _randomBias = Random.Range(-1.0f, 1.0f);

        // Attempt to load the best specimen from a saved file
        var bestSpecimenLoaded = LoadBest();

        // Initialize the population
        for (int i = bestSpecimenLoaded ? 1 : 0; i < _populationSize; i++)
        {
            // Create a new NEAT specimen
            var newSpecimen = new Neat(_inputSize, _outputSize);

            // Instantiate a new CartAndPole prefab
            var cartAndPole = Instantiate(cartAndPolePrefab, Vector3.zero, Quaternion.identity);

            // Randomize the initial rotation of the pole
            var poleRotation = Quaternion.Euler(0, 0, Random.Range(-180f, 180f));
            cartAndPole.pole.transform.rotation = poleRotation;

            // Assign a unique number to the cart
            cartAndPole.cart.SetNumber(i);

            // Add the specimen and its associated GameObject to the current generation
            _currentGeneration.Add(newSpecimen, cartAndPole);
        }
    }

    private void ManageTraining()
    {
        if (_currentGenerationIndex < _maxGenerations)
        {
            // evolve the generation if the current generation is finished
            if (_currentGenerationIsFinished)
            {
                Evolution();
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
                        Destroy(cartAndPole);
                        _deadSpecimens.Add(specimen);
                        _currentGeneration.Remove(specimen);
                    }
                }
            }
        }
    }

    void NeatThink()
    {
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

                nodeSO.SetInputs(inputs);
                nodeSO.SetMove(move);
                cart.moveAmount += Mathf.Abs(move);
                cart.Move(new Vector2(move, 0));
                specimen.SetPoleAngle(angle);
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
        // sort dead specimens by fitness
        _deadSpecimens.Sort((x, y) => y.Fitness.CompareTo(x.Fitness));
        var newGeneration = new Dictionary<Neat, CartAndPole>();
        List<Neat> champions = _deadSpecimens.GetRange(0, _championSize); // first _championSize specimens are the best ones
        List<Neat> antichampions = _deadSpecimens.GetRange(_deadSpecimens.Count - _antichampionSize, _antichampionSize); // last _antichampionSize specimens are the worst ones
        for (int i = 0; i < _championSize; i++)
        {
            var cartAndPole = Instantiate(cartAndPolePrefab, Vector3.zero, Quaternion.identity);
            // Randomize the initial rotation of the pole
            var poleRotation = Quaternion.Euler(0, 0, Random.Range(-180f, 180f));
            cartAndPole.pole.transform.rotation = poleRotation;
            // set the cart number to the best specimen index
            cartAndPole.cart.SetNumber(i);
            newGeneration.Add(champions[i], cartAndPole);
        }
        for (int i = _championSize; i < _championSize + _antichampionSize; i++)
        {
            var cartAndPole = Instantiate(cartAndPolePrefab, Vector3.zero, Quaternion.identity);
            // Randomize the initial rotation of the pole
            var poleRotation = Quaternion.Euler(0, 0, Random.Range(-180f, 180f));
            cartAndPole.pole.transform.rotation = poleRotation;
            // set the cart number to the best specimen index
            cartAndPole.cart.SetNumber(i);
            newGeneration.Add(antichampions[i - _championSize], cartAndPole);
        }
        for (int i = _championSize + _antichampionSize; i < _populationSize; i++)
        {
            // Crossover between the best specimens
            var parent1 = champions[i % _championSize];
            var parent2 = champions[(i + 1) % _championSize];
            var child = parent1.Crossover(parent2);
            var cartAndPole = Instantiate(cartAndPolePrefab, Vector3.zero, Quaternion.identity);
            // Randomize the initial rotation of the pole
            cartAndPole.pole.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-180f, 180f));
            // set the cart number to the best specimen index
            cartAndPole.cart.SetNumber(i);
            newGeneration.Add(child, cartAndPole);
        }
        _deadSpecimens.Clear();
        _currentGeneration = newGeneration;
    }

    // private void Statistics()
    // {

    //     // Calculate continuous statistics
    //     float totalFitness = 0;
    //     float maxFitness = float.MinValue;
    //     foreach (var specimen in _currentGeneration.GetRange(0, _currentSpecimenIndex))
    //     {
    //         totalFitness += specimen.Fitness;
    //         if (specimen.Fitness > maxFitness)
    //         {
    //             maxFitness = specimen.Fitness;
    //         }
    //     }

    //     float averageFitness = totalFitness / _currentSpecimenIndex;

    //     statisticsSO.averageFitness = averageFitness;
    //     statisticsSO.bestFitness = maxFitness;
    //     statisticsSO.lastSpecimenFitness = _currentGeneration[_currentSpecimenIndex - 1].Fitness;

    // }

    private void ResetStatistics()
    {
        statisticsSO.averageFitness = 0;
        statisticsSO.bestFitness = 0;
        statisticsSO.lastSpecimenFitness = 0;
        statisticsSO.generation = 0;
    }

    private bool LoadBest()
    {
        const string bestFilePath = "SavedSpecimen/best.json";
        if (System.IO.File.Exists(bestFilePath))
        {
            var best = Neat.Load(bestFilePath);
            var cartAndPole = Instantiate(cartAndPolePrefab, Vector3.zero, Quaternion.identity);
            // Randomize the initial rotation of the pole
            var poleRotation = Quaternion.Euler(0, 0, Random.Range(-180f, 180f));
            cartAndPole.pole.transform.rotation = poleRotation;
            // set the cart number to the best specimen index
            cartAndPole.cart.SetNumber(0);
            _currentGeneration.Add(best, cartAndPole);
            return true;
        }
        return false;
    }
}
