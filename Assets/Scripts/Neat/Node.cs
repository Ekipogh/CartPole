using System.Collections.Generic;

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

    public Node(NodeType type)
    {
        var sequencer = new Sequencer();
        Type = type;
        Id = sequencer.GetNextNodeId();
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
        var sum = Value;
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
}