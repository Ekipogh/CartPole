using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NodeSO", menuName = "Scriptable Objects/NodeSO")]
public class NodeSO : ScriptableObject
{
    public List<float> inputs = new(5);
    public List<float> outputs = new(1);

    public void SetInputs(float[] inputs)
    {
        // format values to 2 decimal places
        for (int i = 0; i < inputs.Length; i++)
        {
            this.inputs[i] = (float)System.Math.Round(inputs[i], 2);
        }
    }

    public void SetOutputs(List<float> outputs)
    {
        // format values to 2 decimal places
        for (int i = 0; i < outputs.Count; i++)
        {
            this.outputs[i] = (float)System.Math.Round(outputs[i], 2);
        }
    }
}
