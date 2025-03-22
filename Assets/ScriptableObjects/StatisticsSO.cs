using UnityEngine;

[CreateAssetMenu(fileName = "StatisticsSO", menuName = "Scriptable Objects/StatisticsSO")]
public class StatisticsSO : ScriptableObject
{
    public int generation = 0;
    [Header("Average Fitness")]
    public float averageFitness = 0;
    public float bestFitness = 0;
    public float lastSpecimenFitness = 0;
    
}
