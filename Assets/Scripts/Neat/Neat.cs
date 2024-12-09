using System.Collections.Generic;
using UnityEngine;

public class Neat
{
    protected List<Node> _nodeGenes;
    protected List<Node> _inputNodes;
    private List<Node> _outputNodes;
    private List<Connection> _connectionGenes;
    private float _timeCreated;

    private float _fitness;
    public float Fitness { get { return _fitness; } }

    private const float _mutationRate = 0.4f;

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
                inputNode.AddOutConnection(connection);
                outputNode.AddInConnection(connection);
            }
        }
    }

    public Neat(List<Node> inputNodes, List<Node> outputNodes, List<Connection> connections)
    {
        _nodeGenes = new List<Node>();
        _inputNodes = inputNodes;
        _outputNodes = outputNodes;
        _nodeGenes.AddRange(inputNodes);
        _nodeGenes.AddRange(outputNodes);

        _connectionGenes = connections;
    }

    public Neat()
    {
        _nodeGenes = new List<Node>();
        _connectionGenes = new List<Connection>();
        _inputNodes = new List<Node>();
        _outputNodes = new List<Node>();
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

    public void Dead(float fitnessBonus = 0.0f)
    {
        _fitness = CalculateFitness();
        _fitness += fitnessBonus;
    }

    public float CalculateFitness()
    {
        var timeAlive = Time.time - _timeCreated;
        return timeAlive;
    }

    public Neat Crossover(Neat other)
    {
        var child = new Neat();
        child.InheritNodes(this, other);
        child.InheritConnections(this, other);
        child.Mutate();

        return child;
    }

    private void InheritNodes(Neat parent1, Neat parent2)
    {
        var parent1Fitness = parent1._fitness;
        var parent2Fitness = parent2._fitness;
        // compile all nodes from both parents
        // id: (parent1Node|null, parent2Node|null)
        var nodes = new Dictionary<int, (Node, Node)>();
        foreach (var node in parent1._nodeGenes)
        {
            nodes.Add(node.Id, (node, null));
        }
        foreach (var node in parent2._nodeGenes)
        {
            if (!nodes.ContainsKey(node.Id))
            {
                nodes.Add(node.Id, (null, node));
            }
            else
            {
                nodes[node.Id] = (nodes[node.Id].Item1, node);
            }
        }
        // iterate over all nodes
        // if both parents have the node add it to the child
        // if only one parent has the node, add from the parent with the higher fitness
        // if neither parent has the node, skip it
        foreach (var (id, (node1, node2)) in nodes)
        {
            var node1Copy = node1?.Copy();
            var node2Copy = node2?.Copy();
            if (node1 != null && node2 != null)
            {
                // they are the same, just add one of them
                _nodeGenes.Add(node1Copy);
                if (node1Copy.Type == NodeType.Input)
                {
                    _inputNodes.Add(node1Copy);
                }
                else if (node1Copy.Type == NodeType.Output)
                {
                    _outputNodes.Add(node1Copy);
                }
            }
            else if (node1 != null)
            {
                if (parent1Fitness > parent2Fitness)
                {
                    _nodeGenes.Add(node1Copy);
                    if (node1Copy.Type == NodeType.Input)
                    {
                        _inputNodes.Add(node1Copy);
                    }
                    else if (node1Copy.Type == NodeType.Output)
                    {
                        _outputNodes.Add(node1Copy);
                    }
                }
            }
            else if (node2 != null)
            {
                if (parent2Fitness > parent1Fitness)
                {
                    _nodeGenes.Add(node2Copy);
                    if (node2Copy.Type == NodeType.Input)
                    {
                        _inputNodes.Add(node2Copy);
                    }
                    else if (node2Copy.Type == NodeType.Output)
                    {
                        _outputNodes.Add(node2Copy);
                    }
                }
            }
        }
    }

    private void InheritConnections(Neat parent1, Neat parent2)
    {
        var parent1Fitness = parent1._fitness;
        var parent2Fitness = parent2._fitness;
        // compile all connections from both parents
        // id: (parent1Connection|null, parent2Connection|null)
        var connections = new Dictionary<int, (Connection, Connection)>();
        foreach (var connection in parent1._connectionGenes)
        {
            connections.Add(connection.Id, (connection, null));
        }
        foreach (var connection in parent2._connectionGenes)
        {
            if (!connections.ContainsKey(connection.Id))
            {
                connections.Add(connection.Id, (null, connection));
            }
            else
            {
                connections[connection.Id] = (connections[connection.Id].Item1, connection);
            }
        }
        // iterate over all connections
        // if both parents have the connection with same Id, choose one randomly, they may have different weights
        // if only one parent has the connection, add from the parent with the higher fitness
        // if neither parent has the connection, skip it
        foreach (var (id, (connection1, connection2)) in connections)
        {
            if (connection1 != null && connection2 != null)
            {
                if (Random.value < 0.5f)
                {
                    var connection1Copy = connection1.Copy(_nodeGenes);
                    _connectionGenes.Add(connection1Copy);
                }
                else
                {
                    var connection2Copy = connection2.Copy(_nodeGenes);
                    _connectionGenes.Add(connection2Copy);
                }
            }
            else if (connection1 != null)
            {
                if (parent1Fitness > parent2Fitness)
                {
                    var connection1Copy = connection1.Copy(_nodeGenes);
                    _connectionGenes.Add(connection1Copy);
                }
            }
            else if (connection2 != null)
            {
                if (parent2Fitness > parent1Fitness)
                {
                    var connection2Copy = connection2.Copy(_nodeGenes);
                    _connectionGenes.Add(connection2Copy);
                }
            }
        }
    }

    private void Mutate()
    {
        if (Random.value < _mutationRate)
        {
            MutateAddNode();
        }
        if (Random.value < _mutationRate)
        {
            MutateAddConnection();
        }
        foreach (var connection in _connectionGenes)
        {
            if (Random.value < _mutationRate)
            {
                connection.Weight += Random.Range(-0.1f, 0.1f);
            }
        }
        // disable/enabled connections
        foreach (var connection in _connectionGenes)
        {
            if (Random.value < _mutationRate)
            {
                connection.Enabled = !connection.Enabled;
            }
        }
    }

    private void MutateAddNode()
    {
        var connection = _connectionGenes[Random.Range(0, _connectionGenes.Count)];
        connection.Enabled = false;

        var newNode = new Node(NodeType.Hidden);
        _nodeGenes.Add(newNode);

        var connection1 = new Connection(connection.FromNode, newNode, 1.0f);
        _connectionGenes.Add(connection1);
        connection.FromNode.AddOutConnection(connection1);
        newNode.AddInConnection(connection1);

        var connection2 = new Connection(newNode, connection.ToNode, connection.Weight);
        _connectionGenes.Add(connection2);
        newNode.AddOutConnection(connection2);
        connection.ToNode.AddInConnection(connection2);
    }

    private void MutateAddConnection()
    {
        var fromNode = _nodeGenes[Random.Range(0, _nodeGenes.Count)];
        var toNode = _nodeGenes[Random.Range(0, _nodeGenes.Count)];

        if (fromNode.Type == NodeType.Output || toNode.Type == NodeType.Input)
        {
            return;
        }

        var connectionExists = false;
        foreach (var connection in _connectionGenes)
        {
            if (connection.FromNode == fromNode && connection.ToNode == toNode)
            {
                connectionExists = true;
                break;
            }
        }

        if (!connectionExists)
        {
            var connection = new Connection(fromNode, toNode, Random.Range(-1.0f, 1.0f));
            _connectionGenes.Add(connection);
            fromNode.AddOutConnection(connection);
            toNode.AddInConnection(connection);
        }
    }

    public void Clear()
    {
        foreach (var node in _nodeGenes)
        {
            node.InConnections.Clear();
            node.OutConnections.Clear();
        }
        _nodeGenes.Clear();
        _connectionGenes.Clear();
        _inputNodes.Clear();
        _outputNodes.Clear();
    }

    public void Save()
    {
        var saveString = "";
        foreach (var node in _nodeGenes)
        {
            saveString += node.Save() + "\n";
        }
        foreach (var connection in _connectionGenes)
        {
            saveString += connection.Save() + "\n";
        }
        System.IO.File.WriteAllText("Assets/Save.txt", saveString);
    }
}