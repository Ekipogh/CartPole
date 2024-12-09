using System.Diagnostics;

public class Sequencer
{
    private int _nodeId = 0;
    private int _connectionId = 0;

    private static Sequencer _instance;

    public static Sequencer Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new Sequencer();
            }
            return _instance;
        }
    }

    public int GetNextNodeId()
    {
        return _nodeId++;
    }

    public int GetNextConnectionId()
    {
        return _connectionId++;
    }
}