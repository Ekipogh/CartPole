using System.Collections.Generic;

public class Connection
{
    public int Id;

    public Node FromNode { get; set; }

    public Node ToNode { get; set; }

    public float Weight { get; set; }

    public bool Enabled { get; set; }

    public Connection(Node fromNode, Node toNode, float weight, int id = -1)
    {
        if ( id == -1)
        {
            Id = Sequencer.Instance.GetNextConnectionId();
        }
        else
        {
            Id = id;
        }
        FromNode = fromNode;
        ToNode = toNode;
        Weight = weight;
        Enabled = true;
    }

    public Connection Copy(List<Node> nodes)
    {
        var FromNode = nodes.Find(n => n.Id == this.FromNode.Id);
        var ToNode = nodes.Find(n => n.Id == this.ToNode.Id);
        var connection = new Connection(FromNode, ToNode, Weight, Id);
        connection.Enabled = Enabled;
        FromNode.AddOutConnection(connection);
        ToNode.AddInConnection(connection);
        return connection;
    }
}