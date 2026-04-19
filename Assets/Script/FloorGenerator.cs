using System.Collections.Generic;
using UnityEngine;

public sealed class FloorGenerator : MonoBehaviour
{
    [Header("Third Card Weights")]
    [SerializeField, Min(0)] private int emptyWeight = 60;
    [SerializeField, Min(0)] private int badItemWeight = 20;
    [SerializeField, Min(0)] private int goodItemWeight = 20;

    public int GetRowCountForFloor(int floorNumber)
    {
        return Mathf.Max(1, floorNumber);
    }

    public List<CardData> GenerateFloor(int floorNumber)
    {
        int rowCount = GetRowCountForFloor(floorNumber);
        List<CardData> cards = new(rowCount * 3);

        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            List<CardData> rowCards = new(3)
            {
                CardData.CreateStair(),
                CardData.CreateMonster(),
                RollRandomThirdCard()
            };

            Shuffle(rowCards);
            cards.AddRange(rowCards);
        }

        return cards;
    }

    private CardData RollRandomThirdCard()
    {
        int[] weights =
        {
            emptyWeight,
            badItemWeight,
            goodItemWeight
        };

        return WeightedRandomUtility.ChooseIndex(weights) switch
        {
            0 => CardData.CreateEmpty(),
            1 => CardData.CreateBadItem(BadItemType.Curse),
            2 => CardData.CreateGoodItem(GoodItemType.Shield),
            _ => CardData.CreateEmpty()
        };
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
        }
    }
}
