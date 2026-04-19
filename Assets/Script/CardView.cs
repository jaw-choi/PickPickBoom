using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CardView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button button = null!;
    [SerializeField] private Image backgroundImage = null!;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private CanvasGroup canvasGroup = null!;

    [Header("Card Sprites")]
    [SerializeField] private Sprite faceDownSprite = null!;
    [SerializeField] private Sprite stairSprite = null!;
    [SerializeField] private Sprite monsterSprite = null!;
    [SerializeField] private Sprite shieldSprite = null!;
    [SerializeField] private Sprite curseSprite = null!;
    [SerializeField] private Sprite emptySprite = null!;
    [SerializeField] private Sprite fallbackGoodItemSprite;
    [SerializeField] private Sprite fallbackBadItemSprite;

    private Action<int> onSelected;
    private CardData cardData = null!;
    private int cardIndex;
    private Vector3 initialScale;

    private void Awake()
    {
        initialScale = transform.localScale;

        if (!HasValidReferences())
        {
            Debug.LogError(
                "CardView references are missing. Assign Button, Background Image, FaceDown Sprite, Stair Sprite, Monster Sprite, Shield Sprite, Curse Sprite, and Empty Sprite in the Inspector.",
                this);
            enabled = false;
            return;
        }

        button.onClick.AddListener(HandleClicked);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClicked);
        }
    }

    public void Bind(CardData data, int index, Action<int> selectedCallback)
    {
        if (!HasValidReferences())
        {
            return;
        }

        cardData = data;
        cardIndex = index;
        onSelected = selectedCallback;
        transform.localScale = initialScale;

        ShowFaceDownImmediate();
        SetInteractable(true);
    }

    public void SetInteractable(bool interactable)
    {
        button.interactable = interactable && !cardData.IsConsumed;
    }

    public void SetConsumedVisual()
    {
        button.interactable = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.42f;
        }

        SetOptionalText(detailText, "선택 완료");
        transform.localScale = initialScale;
    }

    public void ShowRevealedImmediate()
    {
        ApplySprite(GetRevealSprite(cardData));
        SetOptionalText(titleText, cardData.GetRevealTitle());
        SetOptionalText(detailText, cardData.GetRevealSubtitle());
        RestoreVisibleState();
    }

    public void ShowFaceDownImmediate()
    {
        ApplySprite(faceDownSprite);
        SetOptionalText(titleText, $"카드 {cardIndex + 1}");
        SetOptionalText(detailText, "눌러서 공개");
        RestoreVisibleState();
    }

    public IEnumerator PlayRevealAnimation(float duration)
    {
        if (!HasValidReferences())
        {
            yield break;
        }

        yield return PlayFlipAnimation(duration, ShowRevealedImmediate);
    }

    public IEnumerator PlayFlipToFaceDownAnimation(float duration)
    {
        if (!HasValidReferences())
        {
            yield break;
        }

        yield return PlayFlipAnimation(duration, ShowFaceDownImmediate);
    }

    private IEnumerator PlayFlipAnimation(float duration, Action midFlipAction)
    {
        if (duration <= 0f)
        {
            midFlipAction.Invoke();
            yield break;
        }

        float halfDuration = duration * 0.5f;
        yield return ScaleCard(initialScale.x, 0.05f, halfDuration);
        midFlipAction.Invoke();
        yield return ScaleCard(0.05f, initialScale.x, halfDuration);
        transform.localScale = initialScale;
    }

    private IEnumerator ScaleCard(float fromX, float toX, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            Vector3 scale = initialScale;
            scale.x = Mathf.Lerp(fromX, toX, t);
            transform.localScale = scale;
            yield return null;
        }
    }

    private void HandleClicked()
    {
        onSelected?.Invoke(cardIndex);
    }

    private void RestoreVisibleState()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    private void ApplySprite(Sprite sprite)
    {
        backgroundImage.sprite = sprite;
        backgroundImage.color = Color.white;
        backgroundImage.preserveAspect = true;
    }

    private Sprite GetRevealSprite(CardData data)
    {
        switch (data.Type)
        {
            case CardType.Stair:
                return stairSprite;
            case CardType.Monster:
                return monsterSprite;
            case CardType.GoodItem:
                return GetGoodItemSprite(data.GoodItemType);
            case CardType.BadItem:
                return GetBadItemSprite(data.BadItemType);
            case CardType.Empty:
                return emptySprite;
            default:
                return faceDownSprite;
        }
    }

    private Sprite GetGoodItemSprite(GoodItemType goodItemType)
    {
        return goodItemType switch
        {
            GoodItemType.Shield => shieldSprite,
            _ when fallbackGoodItemSprite != null => fallbackGoodItemSprite,
            _ => shieldSprite
        };
    }

    private Sprite GetBadItemSprite(BadItemType badItemType)
    {
        return badItemType switch
        {
            BadItemType.Curse => curseSprite,
            _ when fallbackBadItemSprite != null => fallbackBadItemSprite,
            _ => curseSprite
        };
    }

    public bool HasValidReferences()
    {
        return button != null &&
            backgroundImage != null &&
            faceDownSprite != null &&
            stairSprite != null &&
            monsterSprite != null &&
            shieldSprite != null &&
            curseSprite != null &&
            emptySprite != null;
    }

    private static void SetOptionalText(TMP_Text targetText, string value)
    {
        if (targetText != null)
        {
            targetText.text = value;
        }
    }
}
