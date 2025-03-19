using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NodeSO", menuName = "Scriptable Objects/NodeSO")]
public class NodeSO : ScriptableObject
{
    public float angle = 0;
    public float slide = 0;
    public float cartx = 0;

    public float poleHeight = 0;

    public float randomBias = 0;
    public float move = 0;

    public void SetInputs(float[] inputs)
    {
        if (inputs.Length != 5)
        {
            Debug.LogError("Invalid number of inputs");
            return;
        }
        angle = (float)System.Math.Round(inputs[0], 2);
        slide = (float)System.Math.Round(inputs[1], 2);
        cartx = (float)System.Math.Round(inputs[2], 2);
        poleHeight = (float)System.Math.Round(inputs[3], 2);
        randomBias = (float)System.Math.Round(inputs[4], 2);
    }

    public void SetMove(float move)
    {
        this.move = (float)System.Math.Round(move, 2);

    }
}
