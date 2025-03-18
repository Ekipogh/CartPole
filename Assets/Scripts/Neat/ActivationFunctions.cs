
using UnityEngine;

public abstract class ActivationFunction
{
    public abstract string Name { get; }
    public abstract float Activate(float x);

    public static ActivationFunction GetActivationFunction(string name)
    {
        switch (name)
        {
            case "Sigmoid":
                return new Sigmoid();
            case "Linear":
                return new Linear();
            case "PassThrough":
                return new PassThrough();
            default:
                return new PassThrough();
        }
    }
}

public class Linear : ActivationFunction
{
    public override string Name { get { return "Linear"; } }
    public override float Activate(float x)
    {
        return x;
    }
}

public class Sigmoid : ActivationFunction
{
    public override string Name { get { return "Sigmoid"; } }
    public override float Activate(float x)
    {
        return 1.0f / (1.0f + Mathf.Exp(-x));
    }
}

public class PassThrough : ActivationFunction
{
    public override string Name { get { return "PassThrough"; } }
    public override float Activate(float x)
    {
        return x;
    }
}
