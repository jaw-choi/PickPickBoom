using System.Collections.Generic;
using UnityEngine;

public static class WeightedRandomUtility
{
    public static int ChooseIndex(IReadOnlyList<int> weights)
    {
        int totalWeight = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            totalWeight += Mathf.Max(0, weights[i]);
        }

        if (totalWeight <= 0)
        {
            return 0;
        }

        int roll = Random.Range(0, totalWeight);
        int cumulativeWeight = 0;

        for (int i = 0; i < weights.Count; i++)
        {
            cumulativeWeight += Mathf.Max(0, weights[i]);
            if (roll < cumulativeWeight)
            {
                return i;
            }
        }

        return weights.Count - 1;
    }
}
