using System.Collections.Generic;
using UnityEngine;

public class NeatController : MonoBehaviour
{
    public Transform poleTopPoint;
    public Transform poleMiddlePoint;
    public Transform poleBottomPoint;

    public Pole pole;
    private Vector3 _poleInitialPosition;
    private Quaternion _poleInitialRotation;
    public Cart cart;
    private Vector3 _cartInitialPosition;

    public Transform poleDebugLinePosition;

    private LineRenderer lineRenderer;

    private float debugLineLength = 1.0f;

    // NEAT settings
    private Neat _currentSpecimen;

    private float _randomBias;

    private const int _inputSize = 5;
    private const int _outputSize = 1;

    // genetic algorithm settings
    private List<Neat> _currentGeneration;

    private const int _generations = 50;
    private int _currentGenerationIndex = 0;
    private const int _populationSize = 50; // number of specimens in the current generation
    private int _currentSpecimenIndex = 0;
    private bool _currentSpecimenIsDead = false;

    private const int _championSize = 10; // number of specimens that will be preserved in the next generation
    private const int _antichampionSize = 1; // number of worst specimens that will be saved in the next generation

    // statistics
    private List<float> _averageFitness = new List<float>();
    private List<float> _maxFitness = new List<float>();

    public StatisticsSO statisticsSO;
    public NodeSO nodeSO;

    public float didntmoveDelta = 30;

    void Start()
    {
        _poleInitialPosition = pole.transform.position;
        _poleInitialRotation = pole.transform.rotation;
        _cartInitialPosition = cart.transform.position;

        _currentGeneration = new List<Neat>();
        _randomBias = Random.Range(-1.0f, 1.0f);

        for (int i = 0; i < _populationSize; i++)
        {
            var connections = new List<Connection>();
            var inputNodes = new List<Node>();
            var outputNodes = new List<Node>();
            for (int j = 0; j < _inputSize; j++)
            {
                inputNodes.Add(new Node(NodeType.Input));
            }
            for (int j = 0; j < _outputSize; j++)
            {
                outputNodes.Add(new Node(NodeType.Output));
            }
            // reset the sequencer if it's not the last specimen
            if (i < _populationSize - 1)
            {
                Sequencer.Instance.ResetNodeIds();
            }
            for (int j = 0; j < _inputSize; j++)
            {
                for (int k = 0; k < _outputSize; k++)
                {
                    var connection = new Connection(inputNodes[j], outputNodes[k], Random.Range(-1.0f, 1.0f));
                    connections.Add(connection);
                    inputNodes[j].AddOutConnection(connection);
                    outputNodes[k].AddInConnection(connection);
                }
            }
            if (i < _populationSize - 1)
            {
                Sequencer.Instance.ResetConnectionIds();
            }
            _currentGeneration.Add(new Neat(inputNodes, outputNodes, connections));
        }
        _currentSpecimen = _currentGeneration[_currentSpecimenIndex];
    }

    // Update is called once per frame
    void Update()
    {
        ManageTraining();
        NeatThink();
    }

    private void ManageTraining()
    {
        if (_currentGenerationIndex < _generations)
        {
            if (_currentSpecimenIsDead)
            {
                var fitnessBonus = 0.0f;
                const float nonMovedBonus = -5f;
                if (cart.moveAmount < didntmoveDelta)
                {
                    Debug.Log("Specimen " + _currentSpecimenIndex + " didn't move.");
                    fitnessBonus += nonMovedBonus;
                }
                _currentSpecimen.Dead(fitnessBonus);
                statisticsSO.lastSpecimenFitness = _currentSpecimen.Fitness;
                Debug.Log("Specimen " + _currentSpecimenIndex + " died. Fitness: " + _currentSpecimen.Fitness);
                _currentSpecimenIndex++;
                if (_currentSpecimenIndex >= _populationSize)
                {
                    _currentGeneration.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));
                    // Save the best specimen
                    _currentGeneration[0].Save($"gen{_currentGenerationIndex}_best");
                    Statistics();
                    _currentSpecimenIndex = 0;
                    _currentGenerationIndex++;
                    Evolution();
                }
                _currentSpecimen = _currentGeneration[_currentSpecimenIndex];
                _currentSpecimen.Start();
                _currentSpecimenIsDead = false;
                ResetScene();
            }
        }
        else
        {
            Debug.Log("Training finished");
            _currentSpecimen = _currentGeneration[0];
            _currentSpecimen.Save("best");
        }
    }

    void NeatThink()
    {
        if (!CheckForDeath())
        {
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
            var poleSlide = poleBottomPoint.position.x - cart.transform.position.x;
            inputs[1] = poleSlide;
            // Cart x position
            var cartX = cart.transform.position.x;
            inputs[2] = cartX;
            // Pole height
            var poleHeight = poleTopPoint.position.y;
            inputs[3] = poleHeight;
            // Include a random bias in the inputs
            inputs[4] = _randomBias;

            var outputs = _currentSpecimen.Evaluate(inputs);

            nodeSO.SetInputs(inputs);
            nodeSO.SetOutputs(outputs);
            cart.moveAmount += Mathf.Abs(outputs[0]);
            cart.Move(new Vector2(outputs[0], 0));
        }
        else
        {
            _currentSpecimenIsDead = true;
        }
    }
    private void ResetScene()
    {
        // Reset the pole to its initial position and rotation
        pole.Reset();
        // Reset the cart to its initial position
        cart.Reset();
    }

    private bool CheckForDeath()
    {
        if (poleTopPoint.position.y < poleMiddlePoint.position.y)
        {
            return true;
        }
        if (pole.IsFallen())
        {
            return true;
        }
        return false;
    }

    private void NeatDebug()
    {
        var poleOrientation = poleTopPoint.position - poleBottomPoint.position;
        var debugTop = poleDebugLinePosition.position + poleOrientation.normalized * debugLineLength;
        var debugBottom = poleDebugLinePosition.position - poleOrientation.normalized * debugLineLength;
        lineRenderer.SetPosition(0, debugTop);
        lineRenderer.SetPosition(1, debugBottom);
    }

    public void Evolution()
    {
        var newGeneration = new List<Neat>();
        newGeneration.AddRange(_currentGeneration.GetRange(0, _championSize));
        newGeneration.AddRange(_currentGeneration.GetRange(_populationSize - _antichampionSize, _antichampionSize));
        for (int i = 0; i < _populationSize - _championSize - _antichampionSize; i++)
        {
            var parent1 = _currentGeneration[i % _championSize];
            var parent2 = _currentGeneration[(i + 1) % _championSize];
            var child = parent1.Crossover(parent2);
            newGeneration.Add(child);
        }
        _currentGeneration = newGeneration;
    }

    private void Statistics()
    {
        float averageFitness = 0;
        float maxFitness = _currentGeneration[0].Fitness;
        foreach (var specimen in _currentGeneration)
        {
            averageFitness += specimen.Fitness;
        }
        averageFitness /= _populationSize;
        _averageFitness.Add(averageFitness);
        _maxFitness.Add(maxFitness);
        statisticsSO.averageFitness = averageFitness;
        statisticsSO.bestFitness = maxFitness;
        Debug.Log("Generation: " + _currentGenerationIndex + " Average Fitness: " + averageFitness + " Max Fitness: " + maxFitness);
        if (_currentGenerationIndex > 0)
        {
            var previousAverageDifference = averageFitness - _averageFitness[_currentGenerationIndex - 1];
            var previousMaxDifference = maxFitness - _maxFitness[_currentGenerationIndex - 1];
            Debug.Log("Average difference from previous generation: " + previousAverageDifference);
            Debug.Log("Maximum difference from previous generation: " + previousMaxDifference);
        }
    }

    private bool LoadBest()
    {
        var best_file = "SavedSpecimen/best.txt";
        if (System.IO.File.Exists(best_file))
        {
            var best = Neat.Load(best_file);
            _currentGeneration.Add(best);
            return true;
        }
        return false;
    }
}
