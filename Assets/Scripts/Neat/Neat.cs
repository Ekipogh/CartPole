using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
public class NeatData
{
    public float fitness;
    public List<Dictionary<string, object>> nodes;
    public List<Dictionary<string, object>> connections;
}
public class NodeData
{
    public int id;
    public string type;
    public string function;
}

public class ConnectionData
{
    public int id;
    public int from;
    public int to;
    public float weight;
    public bool enabled;
}

public class Neat
{
    protected List<Node> _nodes;
    protected List<Node> _inputes;
    private List<Node> _outputs;
    private List<Connection> _connections;
    private long _frames = 0;

    private float _fitness = 0;
    public float Fitness { get { return _fitness; } }

    private float _poleAngleSum;

    private const float _addNodeMutationRate = 0.1f;
    private const float _addConnectionMutationRate = 0.1f;
    private const float _weightMutationRate = 0.8f;
    private const float _enableDisableMutationRate = 0.1f;

    public Neat(int inputSize, int outputSize)
    {
        var localNodeId = 0;
        var localConnectionId = 0;
        _nodes = new List<Node>();
        _connections = new List<Connection>();
        _inputes = new List<Node>();
        _outputs = new List<Node>();

        for (int i = 0; i < inputSize; i++)
        {
            var node = new Node(NodeType.Input, id: localNodeId++);
            _nodes.Add(node);
            _inputes.Add(node);
        }

        for (int i = 0; i < outputSize; i++)
        {
            var node = new Node(NodeType.Output, id: localNodeId++);
            _nodes.Add(node);
            _outputs.Add(node);
        }

        foreach (var outputNode in _outputs)
        {
            foreach (var inputNode in _inputes)
            {
                var randomWeight = Random.Range(-1.0f, 1.0f);
                var connection = new Connection(inputNode, outputNode, randomWeight, id: localConnectionId++);
                _connections.Add(connection);
                inputNode.AddOutConnection(connection);
                outputNode.AddInConnection(connection);
            }
        }

        Sequencer.Instance.SetNodeIdMax(localNodeId);
        Sequencer.Instance.SetConnectionIdMax(localConnectionId);
    }


    public Neat(NeatData data)
    {
        _nodes = new List<Node>();
        _connections = new List<Connection>();
        _inputes = new List<Node>();
        _outputs = new List<Node>();

        var nodeIdMax = 0;
        var connectionIdMax = 0;
        var nodes = data.nodes;
        foreach (Dictionary<string, object> node in nodes)
        {
            var id = int.Parse(node["id"].ToString());
            var nodeType = NodeType.Input;

            if (node["type"].ToString() == "Hidden")
            {
                nodeType = NodeType.Hidden;
            }
            else if (node["type"].ToString() == "Output")
            {
                nodeType = NodeType.Output;
            }
            var function = ActivationFunction.GetActivationFunction(node["function"].ToString());
            var newNode = new Node(nodeType, id)
            {
                ActivationFunction = function
            };
            _nodes.Add(newNode);
            if (nodeType == NodeType.Input)
            {
                _inputes.Add(newNode);
            }
            else if (nodeType == NodeType.Output)
            {
                _outputs.Add(newNode);
            }
            if (id > nodeIdMax)
            {
                nodeIdMax = id;
            }
        }

        var connections = data.connections;
        foreach (var connection in connections)
        {
            var id = int.Parse(connection["id"].ToString());
            var from = int.Parse(connection["from"].ToString());
            var to = int.Parse(connection["to"].ToString());
            var weight = float.Parse(connection["weight"].ToString());
            var enabled = bool.Parse(connection["enabled"].ToString());
            var fromNode = _nodes.Find(n => n.Id == from);
            var toNode = _nodes.Find(n => n.Id == to);
            var newConnection = new Connection(fromNode, toNode, weight, id)
            {
                Enabled = enabled
            };
            _connections.Add(newConnection);
            fromNode.AddOutConnection(newConnection);
            toNode.AddInConnection(newConnection);
            if (id > connectionIdMax)
            {
                connectionIdMax = id;
            }
        }
        Sequencer.Instance.SetNodeIdMax(nodeIdMax);
        Sequencer.Instance.SetConnectionIdMax(connectionIdMax);
    }

    public Neat()
    {
        _nodes = new List<Node>();
        _connections = new List<Connection>();
        _inputes = new List<Node>();
        _outputs = new List<Node>();
    }

    public List<float> Evaluate(float[] inputs)
    {
        var output = new List<float>();
        for (int i = 0; i < inputs.Length; i++)
        {
            _inputes[i].Value = inputs[i];
        }
        try{
        foreach (var outputNode in _outputs)
        {
            output.Add(outputNode.CalculateValue());
        }
        }catch(System.Exception e){
            var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            Save($"error-{timestamp}");
            Debug.Log(e);
        }
        _frames++;
        return output;
    }

    public void Start()
    {
        _fitness = 0;
        _frames = 0;
        _poleAngleSum = 0;
    }

    public void Dead(float fitnessBonus = 0.0f)
    {
        _fitness = CalculateFitness();
        _fitness += fitnessBonus;
    }

    public float CalculateFitness()
    {
        var pole_straightness = 1 - Mathf.Abs(_poleAngleSum / _frames) / 180;
        var fitness = _frames / 100.0f * pole_straightness;
        return fitness;
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
        foreach (var node in parent1._nodes)
        {
            nodes.Add(node.Id, (node, null));
        }
        foreach (var node in parent2._nodes)
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
                _nodes.Add(node1Copy);
                if (node1Copy.Type == NodeType.Input)
                {
                    _inputes.Add(node1Copy);
                }
                else if (node1Copy.Type == NodeType.Output)
                {
                    _outputs.Add(node1Copy);
                }
            }
            else if (node1 != null)
            {
                if (parent1Fitness > parent2Fitness)
                {
                    _nodes.Add(node1Copy);
                    if (node1Copy.Type == NodeType.Input)
                    {
                        _inputes.Add(node1Copy);
                    }
                    else if (node1Copy.Type == NodeType.Output)
                    {
                        _outputs.Add(node1Copy);
                    }
                }
            }
            else if (node2 != null)
            {
                if (parent2Fitness > parent1Fitness)
                {
                    _nodes.Add(node2Copy);
                    if (node2Copy.Type == NodeType.Input)
                    {
                        _inputes.Add(node2Copy);
                    }
                    else if (node2Copy.Type == NodeType.Output)
                    {
                        _outputs.Add(node2Copy);
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
        foreach (var connection in parent1._connections)
        {
            connections.Add(connection.Id, (connection, null));
        }
        foreach (var connection in parent2._connections)
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
                    var connection1Copy = connection1.Copy(_nodes);
                    _connections.Add(connection1Copy);
                }
                else
                {
                    var connection2Copy = connection2.Copy(_nodes);
                    _connections.Add(connection2Copy);
                }
            }
            else if (connection1 != null)
            {
                if (parent1Fitness > parent2Fitness)
                {
                    var connection1Copy = connection1.Copy(_nodes);
                    _connections.Add(connection1Copy);
                }
            }
            else if (connection2 != null)
            {
                if (parent2Fitness > parent1Fitness)
                {
                    var connection2Copy = connection2.Copy(_nodes);
                    _connections.Add(connection2Copy);
                }
            }
        }
    }

    private void Mutate()
    {
        var randomAddNode = Random.Range(0.0f, 1.0f);
        var randomAddConnection = Random.Range(0.0f, 1.0f);

        if (randomAddNode < _addNodeMutationRate)
        {
            MutateAddNode();
        }
        if (randomAddConnection < _addConnectionMutationRate)
        {
            MutateAddConnection();
        }
        foreach (var connection in _connections)
        {
            var random = Random.Range(0.0f, 1.0f);
            if (random < _weightMutationRate)
            {
                connection.Weight += Random.Range(-0.1f, 0.1f);
            }
        }
        // disable/enabled connections
        foreach (var connection in _connections)
        {
            var random = Random.Range(0.0f, 1.0f);
            if (random < _enableDisableMutationRate)
            {
                connection.Enabled = !connection.Enabled;
            }
        }
    }

    private void MutateAddNode()
    {
        var connection = _connections[Random.Range(0, _connections.Count)];
        connection.Enabled = false;

        var newNode = new Node(NodeType.Hidden);
        _nodes.Add(newNode);

        var weight1 = Random.Range(-1.0f, 1.0f);
        var connection1 = new Connection(connection.FromNode, newNode, weight1);
        _connections.Add(connection1);
        connection.FromNode.AddOutConnection(connection1);
        newNode.AddInConnection(connection1);

        var weight2 = Random.Range(-1.0f, 1.0f);
        var connection2 = new Connection(newNode, connection.ToNode, weight2);
        _connections.Add(connection2);
        newNode.AddOutConnection(connection2);
        connection.ToNode.AddInConnection(connection2);
    }

    private void MutateAddConnection()
    {
        var fromNode = _nodes[Random.Range(0, _nodes.Count)];
        var toNode = _nodes[Random.Range(0, _nodes.Count)];

        if (fromNode.Type == NodeType.Output || toNode.Type == NodeType.Input)
        {
            return;
        }

        var connectionExists = false;
        foreach (var connection in _connections)
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
            _connections.Add(connection);
            fromNode.AddOutConnection(connection);
            toNode.AddInConnection(connection);
        }
    }

    public void Clear()
    {
        foreach (var node in _nodes)
        {
            node.InConnections.Clear();
            node.OutConnections.Clear();
        }
        _nodes.Clear();
        _connections.Clear();
        _inputes.Clear();
        _outputs.Clear();
    }

    public void Save(string saveName)
    {
        var saveData = new NeatData
        {
            fitness = _fitness,
            nodes = _nodes.Select(n => new Dictionary<string, object>
            {
                { "id", n.Id },
                { "type", n.Type.ToString() },
                { "function", n.ActivationFunction.Name }
            }).ToList(),
            connections = _connections.Select(c => new Dictionary<string, object>
            {
                { "id", c.Id },
                { "from", c.FromNode.Id },
                { "to", c.ToNode.Id },
                { "weight", c.Weight },
                { "enabled", c.Enabled }
            }).ToList()
        };

        var saveString = JsonConvert.SerializeObject(saveData, Formatting.Indented);

        var directory = "SavedSpecimen";
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }
        System.IO.File.WriteAllText($"SavedSpecimen/{saveName}.json", saveString);
    }

    public static Neat Load(string loadName)
    {
        var saveString = System.IO.File.ReadAllText(loadName);
        var saveData = JsonConvert.DeserializeObject<NeatData>(saveString);

        var neat = new Neat(saveData);
        return neat;
    }

    public void SetPoleAngle(float angle)
    {
        _poleAngleSum += angle;
    }
}