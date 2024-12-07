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

    private const int _inputSize = 4;
    private const int _outputSize = 1;

    // genetic algorithm settings
    private List<Neat> _currentGeneration;
    private const int _populationSize = 100; // number of specimens in the current generation
    private int _currentSpecimenIndex = 0;
    private bool _currentSpecimentIsDead = false;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;

        _poleInitialPosition = pole.transform.position;
        _poleInitialRotation = pole.transform.rotation;
        _cartInitialPosition = cart.transform.position;

        _currentGeneration = new List<Neat>();
        for (int i = 0; i < _populationSize; i++)
        {
            _currentGeneration.Add(new Neat(_inputSize, _outputSize));
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
        if (_currentSpecimentIsDead)
        {
            _currentSpecimen.Dead();
            Debug.Log("Specimen " + _currentSpecimenIndex + " died. Fitness: " + _currentSpecimen.Fitness);
            _currentSpecimenIndex++;
            if (_currentSpecimenIndex >= _populationSize)
            {
                _currentSpecimenIndex = 0;
                _currentGeneration.Sort((a, b) => a.Fitness.CompareTo(b.Fitness));
                _currentGeneration.Reverse();
                // todo: evolve the generation
            }
            _currentSpecimen = _currentGeneration[_currentSpecimenIndex];
            _currentSpecimen.Start();
            _currentSpecimentIsDead = false;
            ResetScene();
        }
    }


    void NeatThink()
    {
        if (!CheckForDeath())
        {
            var inputs = new float[_inputSize];
            // Calculate the height of the pole
            inputs[0] = poleTopPoint.position.y - poleBottomPoint.position.y;
            // Calculate the angle of the pole relative to the vertical axis
            inputs[1] = Vector3.Angle(poleTopPoint.position - poleMiddlePoint.position, Vector3.up);
            // Calculate the relative x position of the pole's middle point to the cart
            inputs[2] = poleMiddlePoint.position.x - transform.position.x;
            // Include a random bias in the inputs
            inputs[3] = _randomBias;

            var outputs = _currentSpecimen.Evaluate(inputs);
            cart.Move(new Vector2(outputs[0], 0));
        }
        else
        {
            _currentSpecimentIsDead = true;
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
}
