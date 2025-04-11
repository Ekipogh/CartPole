using UnityEngine;
using System;

class CartNeat : Neat
{
    private int _id;
    private long _frames = 0;

    private float _distance = 0;

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
        if (_frames == 0)
        {
            return 0; // Avoid division by zero
        }
        // Normalize pole straightness (1 = perfectly upright, 0 = completely horizontal)
        var poleStraightness = CalculatePoleStraightness();

        // Reward time alive (frames)
        var timeReward = _frames / 100.0f;

        // Penalize excessive cart movement (distance)
        var movementPenalty = _distance * 0.1f; // Adjust weight as needed

        // Penalize large pole angles
        var anglePenalty = Mathf.Abs(_poleAngleSum / _frames) * 0.05f; // Adjust weight as needed

        // Combine metrics into a single fitness score
        var fitness = timeReward * poleStraightness - movementPenalty - anglePenalty;

        return fitness;
    }

    public override void Update()
    {
        _frames++;
    }

    public void UpdateDistance(float distance)
    {
        var absDistance = Math.Abs(distance);
        if (absDistance > _distance)
        {
            _distance = absDistance;
        }
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

    public int Id
    {
        get { return _id; }
        set { _id = value; }
    }
}