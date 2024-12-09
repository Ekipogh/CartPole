using System.Collections.Generic;
using Unity.VisualScripting;

public enum NodeType
{
    Input,
    Hidden,
    Output
}

public class Node
{
    public NodeType Type { get; set; }
    public int Id { get; }

    public float Value { get; set; }

    public List<Connection> InConnections { get; set; }

    public List<Connection> OutConnections { get; set; }

    private readonly ActivationFunction _activationFunction;

    public Node(NodeType type, int id = -1)
    {
        Type = type;
        if (id == -1)
        {
            Id = Sequencer.Instance.GetNextNodeId();
        }
        else
        {
            Id = id;
        }
        Value = 0.0f;

        InConnections = new List<Connection>();
        OutConnections = new List<Connection>();

        if (Type == NodeType.Hidden)
        {
            _activationFunction = new Linear();
        }
        else if (Type == NodeType.Output)
        {
            _activationFunction = new Sigmoid();
        }
        else
        {
            _activationFunction = new PassThrough();
        }
    }

    // 1. Take the Value of the node, aplly activation function
    // 2. Multiply the result by the weight of the connection
    // 3. Sum the values of all incoming connections and save it the Value of the node
    public float CalculateValue()
    {
        if (Type == NodeType.Input)
        {
            return Value;
        }
        var sum = 0.0f;
        foreach (var connection in InConnections)
        {
            if (connection.Enabled)
            {
                sum += connection.FromNode.CalculateValue() * connection.Weight;
            }
        }
        Value = _activationFunction.Activate(sum);
        return Value;
    }
    public void AddInConnection(Connection connection)
    {
        var connectionId = connection.Id;
        var exists = false;
        foreach (var c in InConnections)
        {
            if (c.Id == connectionId)
            {
                exists = true;
                break;
            }
        }
        if (!exists)
        {
            InConnections.Add(connection);
        }
    }
    public void AddOutConnection(Connection connection)
    {
        var connectionId = connection.Id;
        var exists = false;
        foreach (var c in OutConnections)
        {
            if (c.Id == connectionId)
            {
                exists = true;
                break;
            }
        }
        if (!exists)
        {
            OutConnections.Add(connection);
        }
    }

    public Node Copy()
    {
        var node = new Node(Type, Id);
        return node;
    }

    public string Save()
    {
        return $"Node: {Id} {Type} {_activationFunction}";
    }
}