using UnityEngine;

[CreateAssetMenu(fileName = "StatisticsSO", menuName = "Scriptable Objects/StatisticsSO")]
public class StatisticsSO : ScriptableObject
{
    public int generation = 0;
    public float averageFitness = 0;
    public float bestFitness = 0;
    public float lastSpecimenFitness = 0;

    public int currentPopulation = 0;

    public void SetGeneration(int generation)
    {
        this.generation = generation;
    }
    public void SetAverageFitness(float averageFitness)
    {
        this.averageFitness = (float)System.Math.Round(averageFitness, 2);
    }
    public void SetBestFitness(float bestFitness)
    {
        this.bestFitness = (float)System.Math.Round(bestFitness, 2);
    }
    public void SetLastSpecimenFitness(float lastSpecimenFitness)
    {
        this.lastSpecimenFitness = (float)System.Math.Round(lastSpecimenFitness, 2);
    }

    public void AttemptToSetMaxFitness(float fitness)
    {
        if (fitness > bestFitness)
        {
            SetBestFitness(fitness);
        }
    }

    public void SetCurrentPopulation(int currentPopulation)
    {
        this.currentPopulation = currentPopulation;
    }

    public void ResetStatistics()
    {
        generation = 0;
        averageFitness = 0;
        bestFitness = 0;
        lastSpecimenFitness = 0;
    }

}
