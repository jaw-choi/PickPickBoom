using System;

public enum CardType
{
    Stair,
    Chest,
    Monster,
    GoodItem,
    BadItem,
    Empty
}

public enum GoodItemType
{
    None,
    Shield
}

public enum BadItemType
{
    None,
    Curse
}

public enum StatusTone
{
    Neutral,
    Good,
    Bad,
    Warning
}

[Serializable]
public sealed class CardData
{
    public CardType Type { get; }
    public GoodItemType GoodItemType { get; }
    public BadItemType BadItemType { get; }
    public bool IsConsumed { get; private set; }

    private CardData(CardType type, GoodItemType goodItemType = GoodItemType.None, BadItemType badItemType = BadItemType.None)
    {
        Type = type;
        GoodItemType = goodItemType;
        BadItemType = badItemType;
    }

    public static CardData CreateStair()
    {
        return new CardData(CardType.Stair);
    }

    public static CardData CreateChest()
    {
        return new CardData(CardType.Chest);
    }

    public static CardData CreateMonster()
    {
        return new CardData(CardType.Monster);
    }

    public static CardData CreateGoodItem(GoodItemType goodItemType)
    {
        return new CardData(CardType.GoodItem, goodItemType);
    }

    public static CardData CreateBadItem(BadItemType badItemType)
    {
        return new CardData(CardType.BadItem, GoodItemType.None, badItemType);
    }

    public static CardData CreateEmpty()
    {
        return new CardData(CardType.Empty);
    }

    public void Consume()
    {
        IsConsumed = true;
    }

    public void ResetConsumed()
    {
        IsConsumed = false;
    }

    public string GetRevealTitle()
    {
        return Type switch
        {
            CardType.Stair => "계단",
            CardType.Chest => "보물 상자",
            CardType.Monster => "몬스터",
            CardType.GoodItem when GoodItemType == GoodItemType.Shield => "방패",
            CardType.BadItem when BadItemType == BadItemType.Curse => "저주",
            CardType.Empty => "빈 카드",
            CardType.GoodItem => "아이템",
            CardType.BadItem => "함정",
            _ => "알 수 없음"
        };
    }

    public string GetRevealSubtitle()
    {
        return Type switch
        {
            CardType.Stair => "다음 층으로 이동",
            CardType.Chest => "스테이지 통과",
            CardType.Monster => "방패가 없으면 즉시 패배",
            CardType.GoodItem when GoodItemType == GoodItemType.Shield => "몬스터를 한 번 막아 줌",
            CardType.BadItem when BadItemType == BadItemType.Curse => "2칸 전으로 되돌아감",
            CardType.Empty => "아무 일도 일어나지 않음",
            CardType.GoodItem => "도움이 되는 효과",
            CardType.BadItem => "나쁜 효과",
            _ => string.Empty
        };
    }
}
