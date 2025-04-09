using UnityEngine;
using Newtonsoft.Json;

class CartNeat : Neat
{
    private long _frames = 0;

    private float _poleAngleSum;

    public CartNeat() : base()
    {
    }

    public CartNeat(int inputs, int outputs) : base(inputs, outputs)
    {
    }

    public CartNeat(NeatData data) : base(data)
    {
    }

    private float CalculatePoleStraightness()
    {
        if (_frames == 0)
        {
            return 1; // Default value when no frames have been processed
        }
        return 1 - Mathf.Abs(_poleAngleSum / _frames) / 180;
    }

    public override float CalculateFitness()
    {
        var pole_straightness = CalculatePoleStraightness();
        var fitness = _frames / 100.0f * pole_straightness;
        return fitness;
    }

    public override void Update()
    {
        _frames++;
    }

    public void SetPoleAngle(float angle)
    {
        _poleAngleSum += angle;
    }

    public override void Reset()
    {
        base.Reset();
        _frames = 0;
        _poleAngleSum = 0;
    }
}