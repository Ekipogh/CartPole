using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Random = UnityEngine.Random;

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
    protected List<Node> _inputs;
    private List<Node> _outputs;
    private List<Connection> _connections;
    private float _fitness = 0;
    public float Fitness { get { return _fitness; } }
    private const float _addNodeMutationRate = 0.1f;
    private const float _addConnectionMutationRate = 0.1f;
    private const float _weightMutationRate = 0.8f;
    private const float _enableDisableMutationRate = 0.1f;

    private bool _isDead = false;
    public bool IsDead
    {
        get { return _isDead; }
        set { _isDead = value; }
    }

    public Neat(int inputSize, int outputSize)
    {
        var localNodeId = 0;
        var localConnectionId = 0;
        _nodes = new List<Node>();
        _connections = new List<Connection>();
        _inputs = new List<Node>();
        _outputs = new List<Node>();

        for (int i = 0; i < inputSize; i++)
        {
            var node = new Node(NodeType.Input, id: localNodeId++);
            _nodes.Add(node);
            _inputs.Add(node);
        }

        for (int i = 0; i < outputSize; i++)
        {
            var node = new Node(NodeType.Output, id: localNodeId++);
            _nodes.Add(node);
            _outputs.Add(node);
        }

        foreach (var outputNode in _outputs)
        {
            foreach (var inputNode in _inputs)
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
        _inputs = new List<Node>();
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
                _inputs.Add(newNode);
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
        Sequencer.Instance.SetNodeIdMax(nodeIdMax + 1);
        Sequencer.Instance.SetConnectionIdMax(connectionIdMax + 1);
    }

    public Neat()
    {
        _nodes = new List<Node>();
        _connections = new List<Connection>();
        _inputs = new List<Node>();
        _outputs = new List<Node>();
    }

    public List<float> Evaluate(float[] inputs)
    {
        ResetVisits();
        var output = new List<float>();
        for (int i = 0; i < inputs.Length; i++)
        {
            _inputs[i].Value = inputs[i];
        }
        foreach (var outputNode in _outputs)
        {
            output.Add(outputNode.CalculateValue());
        }
        Update();
        return output;
    }

    public virtual void Update()
    {
    }

    public void Dead(float fitnessBonus = 0.0f)
    {
        _fitness = CalculateFitness();
        _fitness += fitnessBonus;
        _isDead = true;
    }

    public virtual float CalculateFitness()
    {
        return 0;
    }

    private bool CheckOrphanedNodes()
    {
        var orphanedNodes = new List<Node>();
        foreach (var node in _nodes)
        {
            if (node.InConnections.Count == 0 && node.OutConnections.Count == 0)
            {
                orphanedNodes.Add(node);
            }
        }
        return orphanedNodes.Count > 0;
    }

    public T Crossover<T>(Neat other) where T : Neat, new()
    {
        var child = new T();
        child.InheritGenes(this, other);
        child.Mutate();
        return child;
    }


    private void InheritGenes(Neat parent1, Neat parent2)
    {
        // Ensure all input and output nodes from both parents are inherited
        foreach (var node in parent1._nodes.Concat(parent2._nodes))
        {
            if (node.Type == NodeType.Input || node.Type == NodeType.Output)
            {
                GetOrCreate(node.Id, node.Type);
            }
        }
        // Compile all connections from both parents
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
        foreach (var connection in connections)
        {
            Connection connectionToAdd = null;
            if (connection.Value.Item1 != null && connection.Value.Item2 != null)
            {
                // both parents have the connection, add a random of the two to the child
                if (Random.value < 0.5f)
                {
                    connectionToAdd = connection.Value.Item1;
                }
                else
                {
                    connectionToAdd = connection.Value.Item2;
                }
            }
            else if (connection.Value.Item1 != null)
            {
                // disjoint or excess connection of parent 1
                // add it if parent 1 has higher fitness or if they are equal and random is less than 0.5
                if (parent1._fitness > parent2._fitness || (parent1._fitness == parent2._fitness && Random.value < 0.5f))
                {
                    connectionToAdd = connection.Value.Item1;
                }
            }
            else if (connection.Value.Item2 != null)
            {
                // disjoint or excess connection of parent 2
                // add it if parent 2 has higher fitness or if they are equal and random is less than 0.5
                if (parent2._fitness > parent1._fitness || (parent1._fitness == parent2._fitness && Random.value < 0.5f))
                {
                    connectionToAdd = connection.Value.Item2;
                }
            }
            else
            {
                // neither parent has the connection, skip it
                continue;
            }
            if (connectionToAdd == null)
            {
                continue;
            }
            InheritConnection(connectionToAdd);
        }
    }

    private Node GetOrCreate(int nodeId, NodeType nodeType)
    {
        var node = _nodes.Find(n => n.Id == nodeId);
        if (node == null)
        {
            node = new Node(nodeType, nodeId);
            _nodes.Add(node);
            if (nodeType == NodeType.Input)
            {
                _inputs.Add(node);
            }
            else if (nodeType == NodeType.Output)
            {
                _outputs.Add(node);
            }
        }
        return node;
    }

    private void InheritConnection(Connection connection)
    {
        var fromNode = GetOrCreate(connection.FromNode.Id, connection.FromNode.Type);
        var toNode = GetOrCreate(connection.ToNode.Id, connection.ToNode.Type);
        var newConnection = new Connection(fromNode, toNode, connection.Weight, connection.Id);
        newConnection.Enabled = connection.Enabled;
        newConnection.FromNode = fromNode;
        newConnection.ToNode = toNode;
        fromNode.AddOutConnection(newConnection);
        toNode.AddInConnection(newConnection);
        _connections.Add(newConnection);
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

        var FromNode = connection.FromNode;
        var ToNode = connection.ToNode;

        var weightFrom = Random.Range(-1.0f, 1.0f);
        var connectionFrom = new Connection(FromNode, newNode, weightFrom);
        _connections.Add(connectionFrom);
        FromNode.AddOutConnection(connectionFrom);
        newNode.AddInConnection(connectionFrom);

        var weightTo = Random.Range(-1.0f, 1.0f);
        var connectionTo = new Connection(newNode, ToNode, weightTo);
        _connections.Add(connectionTo);
        newNode.AddOutConnection(connectionTo);
        ToNode.AddInConnection(connectionTo);
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
        _inputs.Clear();
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

    public static T Load<T>(string loadName) where T : Neat, new()
    {
        var saveString = System.IO.File.ReadAllText(loadName);
        var saveData = JsonConvert.DeserializeObject<NeatData>(saveString);

        var neat = new T();
        neat.InitializeFromData(saveData); // Add a method to initialize from NeatData
        return neat;
    }

    private void InitializeFromData(NeatData data)
    {
        var nodeIdMax = 0;
        var connectionIdMax = 0;
        foreach (var nodeData in data.nodes)
        {
            var nodeType = Enum.TryParse<NodeType>(nodeData["type"].ToString(), out var parsedNodeType) ? parsedNodeType : NodeType.Hidden;
            var function = ActivationFunction.GetActivationFunction(nodeData["function"].ToString());
            var id = int.Parse(nodeData["id"].ToString());
            if (id > nodeIdMax)
            {
                nodeIdMax = id;
            }
            var newNode = new Node(nodeType, id)
            {
                ActivationFunction = function
            };
            _nodes.Add(newNode);
            if (nodeType == NodeType.Input)
            {
                _inputs.Add(newNode);
            }
            else if (nodeType == NodeType.Output)
            {
                _outputs.Add(newNode);
            }
        }
        foreach (var connectionData in data.connections)
        {
            var fromID = int.Parse(connectionData["from"].ToString());
            var toID = int.Parse(connectionData["to"].ToString());
            var fromNode = _nodes.Find(n => n.Id == fromID);
            var toNode = _nodes.Find(n => n.Id == toID);
            var id = int.Parse(connectionData["id"].ToString());
            if (id > connectionIdMax)
            {
                connectionIdMax = id;
            }
            var weight = float.Parse(connectionData["weight"].ToString());
            var connection = new Connection(fromNode, toNode, weight, id)
            {
                Enabled = (bool)connectionData["enabled"]
            };
            _connections.Add(connection);
            fromNode.AddOutConnection(connection);
            toNode.AddInConnection(connection);
        }
        Sequencer.Instance.SetNodeIdMax(nodeIdMax + 1);
        Sequencer.Instance.SetConnectionIdMax(connectionIdMax + 1);
    }

    private void ResetVisits()
    {
        foreach (var node in _nodes)
        {
            node.ResetVisits();
        }
    }

    public virtual void Reset()
    {
        foreach (var node in _nodes)
        {
            node.ResetVisits();
        }
        _isDead = false;
        _fitness = 0;
    }
}