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

    // Population settings
    private const int _populationSize = 100;
    private const int _eliteSize = 10;
    private const int _epochs = 100;

    private Neat _bestSpecimen;
    private List<Neat> _deadSpecimens = new List<Neat>();

    private bool _isRunning = false;

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

        _randomBias = Random.Range(-1.0f, 1.0f);
        // NEAT settings
        _currentSpecimen = new Neat(_inputSize, _outputSize);
    }

    // Update is called once per frame
    void Update()
    {
        ManageTraining();
        NeatThink();
    }

    private void ManageTraining()
    {
        // todo: Implement training logic
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
            _deadSpecimens.Add(_currentSpecimen);
            ResetScene();
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
            Debug.Log("Pole has fallen below the initial position, marking specimen as dead");
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
