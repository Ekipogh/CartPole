using UnityEngine;
using System;

class CartNeat : Neat
{
    private int _id;
    private long _frames = 0;

    private float _distance = 0;

    private float _poleAngleSum;

    private const float _gliderPenaltyModifier = 2f; // Adjust this value to change the penalty for glider behavior
    private const float _anglePenaltyModifier = 0.5f; // Adjust this value to change the penalty for angle deviation
    private const float _timeRewardModifier = 1f; // Adjust this value to change the reward for time alive
    private const float _poleStraightnessModifier = 1f; // Adjust this value to change the reward for pole straightness

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
        // importanse of variables:
        // 1. Time the cart is alive (frames)
        // 2. Pole straightness (1 = perfectly upright, 0 = completely horizontal)
        // 3. Distance traveled (frames), penalize "gliders", slowly moving one way carts
        // 4. Pole angle (degrees), penalize large angles
        if (_frames == 0)
        {
            return 0; // Avoid division by zero
        }
        // Normalize pole straightness (1 = perfectly upright, 0 = completely horizontal)
        var poleStraightness = CalculatePoleStraightness() * _poleStraightnessModifier;

        // Reward time alive (frames)
        var timeReward = _frames / 100.0f * _timeRewardModifier;

        // Penalize excessive cart movement (distance)
        var movementPenalty = _distance * _gliderPenaltyModifier;

        // Penalize large pole angles
        var anglePenalty = Mathf.Abs(_poleAngleSum / _frames) * _anglePenaltyModifier;

        var penalty = movementPenalty + anglePenalty;

        // Combine metrics into a single fitness score
        var fitness = timeReward * poleStraightness / (penalty + 1); // Add 1 to avoid division by zero

        return fitness;
    }

    public override void Update()
    {
        _frames++;
    }

    public void UpdateDistance(float distance)
    {
        var absDistance = Math.Abs(distance);
        _distance = Mathf.Max(_distance, absDistance);
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