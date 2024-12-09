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
}