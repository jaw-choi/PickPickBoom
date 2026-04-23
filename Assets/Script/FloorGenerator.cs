using System.Collections.Generic;
using UnityEngine;

public sealed class FloorGenerator : MonoBehaviour
{
    public int GetRowCountForFloor(int floorNumber)
    {
        return Mathf.Max(1, floorNumber);
    }

    public int GetTotalRowCountForFloor(int floorNumber)
    {
        return GetRowCountForFloor(floorNumber) + 1;
    }

    public List<CardData> GenerateFloor(int floorNumber, GameDifficultyProfile difficultyProfile)
    {
        int regularRowCount = GetRowCountForFloor(floorNumber);
        List<CardData> cards = new(GetTotalRowCountForFloor(floorNumber) * 3)
        {
            CardData.CreatePlaceholder(),
            CardData.CreateChest(),
            CardData.CreatePlaceholder()
        };

        for (int rowIndex = 0; rowIndex < regularRowCount; rowIndex++)
        {
            List<CardData> rowCards = new(3)
            {
                CardData.CreateStair(),
                CardData.CreateMonster(),
                RollRandomThirdCard(difficultyProfile)
            };

            Shuffle(rowCards);
            cards.AddRange(rowCards);
        }

        return cards;
    }

    private static CardData RollRandomThirdCard(GameDifficultyProfile difficultyProfile)
    {
        int[] weights =
        {
            difficultyProfile.emptyWeight,
            difficultyProfile.curseWeight,
            difficultyProfile.goodItemWeight
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
