using System.Collections.Generic;
using UnityEngine;

public class Neat
{
    private List<Node> _nodeGenes;
    private List<Node> _inputNodes;
    private List<Node> _outputNodes;
    private List<Connection> _connectionGenes;
    private float _timeCreated;

    private float _fitness;
    public float Fitness { get { return _fitness; } }

    public Neat(int inputSize, int outputSize)
    {
        _nodeGenes = new List<Node>();
        _connectionGenes = new List<Connection>();
        _inputNodes = new List<Node>();
        _outputNodes = new List<Node>();

        for (int i = 0; i < inputSize; i++)
        {
            var node = new Node(NodeType.Input);
            _nodeGenes.Add(node);
            _inputNodes.Add(node);
        }

        for (int i = 0; i < outputSize; i++)
        {
            var node = new Node(NodeType.Output);
            _nodeGenes.Add(node);
            _outputNodes.Add(node);
        }

        foreach (var outputNode in _outputNodes)
        {
            foreach (var inputNode in _inputNodes)
            {
                var randomWeight = Random.Range(-1.0f, 1.0f);
                var connection = new Connection(inputNode, outputNode, randomWeight);
                _connectionGenes.Add(connection);
                inputNode.OutConnections.Add(connection);
                outputNode.InConnections.Add(connection);
            }
        }
    }

    public List<float> Evaluate(float[] inputs)
    {
        var output = new List<float>();
        for (int i = 0; i < inputs.Length; i++)
        {
            _inputNodes[i].Value = inputs[i];
        }
        foreach (var outputNode in _outputNodes)
        {
            output.Add(outputNode.CalculateValue());
        }

        return output;
    }

    public void Start()
    {
        _fitness = 0;
        _timeCreated = Time.time;
    }

    public void Dead()
    {
        _fitness = CalculateFitness();
    }

    public float CalculateFitness()
    {
        var timeAlive = Time.time - _timeCreated;
        return timeAlive;
    }

}