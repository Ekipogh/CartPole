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

    private const int _championSize = 5; // number of specimens that will be preserved in the next generation
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

        var bestSpecimen = LoadBest();
        var startPopulationSize = bestSpecimen ? _populationSize - 1 : _populationSize;

        for (int i = 0; i < startPopulationSize; i++)
        {
            var newSpecimen = new Neat(_inputSize, _outputSize);
            _currentGeneration.Add(newSpecimen);
        }
        _currentSpecimen = _currentGeneration[_currentSpecimenIndex];
        var angle = pole.transform.rotation.eulerAngles.z;
        if (angle > 180)
        {
            angle -= 360;
        }
        _currentSpecimen.SetPoleAngle(angle);
        statisticsSO.generation = 0;
        Statistics();
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
                Statistics();
                if (_currentSpecimenIndex >= _populationSize)
                {
                    _currentGeneration.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));
                    // Save the best specimen
                    _currentGeneration[0].Save($"gen{_currentGenerationIndex}_best");
                    // save the worst specimen
                    _currentGeneration[_populationSize - 1].Save($"gen{_currentGenerationIndex}_worst");
                    _currentSpecimenIndex = 0;
                    _currentGenerationIndex++;
                    statisticsSO.generation = _currentGenerationIndex;
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

            // output[0] sigmoid value between 0 and 1
            var outputs = _currentSpecimen.Evaluate(inputs);
            // move value between -1 and 1
            var move = outputs[0] * 2 - 1;

            nodeSO.SetInputs(inputs);
            nodeSO.SetMove(move);
            cart.moveAmount += Mathf.Abs(move);
            cart.Move(new Vector2(move, 0));
            _currentSpecimen.SetPoleAngle(angle);
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


        if (_currentSpecimenIndex == 0)
        {
            // Reset statistics
            statisticsSO.averageFitness = 0;
            statisticsSO.bestFitness = 0;
            statisticsSO.lastSpecimenFitness = 0;
        }
        else
        {
            // Calculate continuous statistics
            float totalFitness = 0;
            float maxFitness = float.MinValue;
            foreach (var specimen in _currentGeneration.GetRange(0, _currentSpecimenIndex))
            {
                totalFitness += specimen.Fitness;
                if (specimen.Fitness > maxFitness)
                {
                    maxFitness = specimen.Fitness;
                }
            }

            float averageFitness = totalFitness / (_currentSpecimenIndex + 1);

            statisticsSO.averageFitness = averageFitness;
            statisticsSO.bestFitness = maxFitness;
            statisticsSO.lastSpecimenFitness = _currentGeneration[_currentSpecimenIndex - 1].Fitness;
        }
    }

    private bool LoadBest()
    {
        const string bestFilePath = "SavedSpecimen/best.json";
        if (System.IO.File.Exists(bestFilePath))
        {
            var best = Neat.Load(bestFilePath);
            _currentGeneration.Add(best);
            return true;
        }
        return false;
    }
}
