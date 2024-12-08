
using UnityEngine;

public abstract class ActivationFunction
{
    public abstract float Activate(float x);
}

public class Linear : ActivationFunction
{

    public override float Activate(float x)
    {
        return x;
    }
}

public class Sigmoid : ActivationFunction
{
    public override float Activate(float x)
    {
        return 1.0f / (1.0f + Mathf.Exp(-x));
    }
}

public class PassThrough : ActivationFunction
{
    public override float Activate(float x)
    {
        return x;
    }
}