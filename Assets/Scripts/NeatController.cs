using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class NeatController : MonoBehaviour
{
    // NEAT settings
    private float _randomBias;

    private const int _inputSize = 5;
    private const int _outputSize = 1;

    // genetic algorithm settings
    private Dictionary<Neat, CartAndPole> _currentGeneration;
    private List<Neat> _deadSpecimens = new();
    private int _maxGenerations = 50;
    private int _currentGenerationIndex = 0;
    private int _populationSize = 50; // number of specimens in the current generation
    private bool _currentGenerationIsFinished = false;

    private const int _championSize = 5; // number of specimens that will be preserved in the next generation
    private const int _antichampionSize = 1; // number of worst specimens that will be saved in the next generation

    private Neat absoluteBestSpecimen = null; // the best specimen of all generations

    public StatisticsSO statisticsSO;
    public NodeSO nodeSO;

    public CartAndPole cartAndPolePrefab;

    public FollowCamera mainCamera;

    // statistics
    private float _maxFitness = 0; // max fitness of the whole session

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
        NeatThink();
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
        _currentGeneration = new Dictionary<Neat, CartAndPole>();
        _randomBias = Random.Range(-1.0f, 1.0f);

        // Attempt to load the best specimen from a saved file
        var bestSpecimenLoaded = LoadBest();

        // Initialize the population
        for (int i = bestSpecimenLoaded ? 1 : 0; i < _populationSize; i++)
        {
            // Create a new NEAT specimen
            var newSpecimen = new Neat(_inputSize, _outputSize);

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
        List<Neat> specimensToRemove = new();
        if (_currentGenerationIndex < _maxGenerations)
        {
            // evolve the generation if the current generation is finished
            if (_currentGenerationIsFinished)
            {
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
            // Quit the application
            Application.Quit();
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

    void NeatThink()
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
        // update the absolute best specimen
        if (absoluteBestSpecimen == null || _deadSpecimens.First().Fitness > absoluteBestSpecimen.Fitness)
        {
            absoluteBestSpecimen = _deadSpecimens.First();
        }
        // save the best and the worst specimens
        if (_deadSpecimens.Count > 0)
        {
            var bestFileName = $"generation_{_currentGenerationIndex}_best";
            var worstFileName = $"generation_{_currentGenerationIndex}_worst";
            _deadSpecimens.First().Save(bestFileName);
            _deadSpecimens.Last().Save(worstFileName);
        }
        var newGeneration = new Dictionary<Neat, CartAndPole>();
        List<Neat> champions = _deadSpecimens.GetRange(0, _championSize); // first _championSize specimens are the best ones
        List<Neat> antichampions = _deadSpecimens.GetRange(_deadSpecimens.Count - _antichampionSize, _antichampionSize); // last _antichampionSize specimens are the worst ones
        for (int i = 0; i < _championSize; i++)
        {
            CartAndPole cartAndPole = InstantiateCartAndPole(i);
            // Randomize the initial rotation of the pole
            var poleRotation = RandomizeRotation();
            cartAndPole.pole.transform.rotation = poleRotation;
            // set the cart number to the best specimen index
            cartAndPole.cart.SetNumber(i);
            champions[i].IsDead = false; // reset the dead state of the specimen
            newGeneration.Add(champions[i], cartAndPole);
        }
        for (int i = _championSize; i < _championSize + _antichampionSize; i++)
        {
            var cartAndPole = InstantiateCartAndPole(i);
            // Randomize the initial rotation of the pole
            var poleRotation = RandomizeRotation();
            cartAndPole.pole.transform.rotation = poleRotation;
            // set the cart number to the best specimen index
            cartAndPole.cart.SetNumber(i);
            antichampions[i - _championSize].IsDead = false; // reset the dead state of the specimen
            newGeneration.Add(antichampions[i - _championSize], cartAndPole);
        }
        int j = 0;
        for (int i = _championSize + _antichampionSize; i < _populationSize; i++)
        {
            // Crossover between the best specimens
            var parent1 = champions[j % _championSize];
            var parent2 = champions[(j + 1) % _championSize];
            var child = parent1.Crossover(parent2);
            var cartAndPole = InstantiateCartAndPole(i);
            // Randomize the initial rotation of the pole
            cartAndPole.pole.transform.rotation = RandomizeRotation();
            // set the cart number to the best specimen index
            cartAndPole.cart.SetNumber(i);
            newGeneration.Add(child, cartAndPole);
            j++;
        }
        _deadSpecimens.Clear();
        _currentGeneration = newGeneration;
        _currentGenerationIsFinished = false;
        _currentGenerationIndex++;
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
        foreach (var specimen in _deadSpecimens)
        {
            totalFitness += specimen.Fitness;
            if (specimen.Fitness > _maxFitness)
            {
                _maxFitness = specimen.Fitness;
            }
        }

        float averageFitness = totalFitness / _deadSpecimens.Count;

        statisticsSO.SetAverageFitness(averageFitness);
        statisticsSO.SetBestFitness(_maxFitness);
        statisticsSO.SetLastSpecimenFitness(_deadSpecimens.Last().Fitness);
    }

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
}
