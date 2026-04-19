using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TMP_Text currentFloorText = null!;
    [SerializeField] private TMP_Text bestFloorText = null!;
    [SerializeField] private TMP_Text shieldStatusText = null!;
    [SerializeField] private TMP_Text curseStatusText = null!;
    [SerializeField] private TMP_Text statusMessageText = null!;

    [Header("Board")]
    [SerializeField] private ScrollRect boardScrollRect = null!;
    [SerializeField] private RectTransform cardContentRoot = null!;
    [SerializeField] private CardView cardViewPrefab = null!;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel = null!;
    [SerializeField] private TMP_Text gameOverText = null!;
    [SerializeField] private Button restartButton = null!;

    [Header("Status Colors")]
    [SerializeField] private Color neutralColor = new(0.95f, 0.95f, 0.95f, 1f);
    [SerializeField] private Color goodColor = new(0.58f, 0.91f, 0.63f, 1f);
    [SerializeField] private Color badColor = new(1f, 0.57f, 0.57f, 1f);
    [SerializeField] private Color warningColor = new(1f, 0.86f, 0.46f, 1f);

    private readonly List<CardView> cardViews = new();
    private Action restartRequested;
    private int activeCardCount;
    private bool hasCachedBaseGridPadding;
    private int baseGridPaddingTop;
    private int baseGridPaddingBottom;

    public int ActiveCardCount => activeCardCount;

    private void Awake()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(HandleRestartClicked);
        }

        if (gameOverPanel != null)
        {
            HideGameOver();
        }
    }

    private void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(HandleRestartClicked);
        }
    }

    public void Initialize(Action restartCallback)
    {
        restartRequested = restartCallback;
    }

    public void BindFloorCards(IReadOnlyList<CardData> cards, Action<int> selectedCallback)
    {
        EnsureCardPoolSize(cards.Count);
        activeCardCount = cards.Count;

        for (int i = 0; i < cardViews.Count; i++)
        {
            bool isActive = i < activeCardCount;
            cardViews[i].gameObject.SetActive(isActive);

            if (isActive)
            {
                cardViews[i].Bind(cards[i], i, selectedCallback);
            }
        }

        RebuildBoardLayout();
        ResetBoardScrollToTop();
    }

    public CardView GetCardView(int index)
    {
        return cardViews[index];
    }

    public void RefreshHud(int currentTowerHeight, int bestTowerHeight, int activeFloorNumber, PlayerState playerState)
    {
        currentFloorText.text = $"현재 층: {activeFloorNumber} / {currentTowerHeight}";
        bestFloorText.text = $"최고 타워 높이: {bestTowerHeight}";
        shieldStatusText.text = playerState.HasShield ? "방패: 보유 중" : "방패: 없음";
        curseStatusText.text = playerState.HideNextResultMessage ? "저주: 다음 결과 숨김" : "저주: 없음";
    }

    public void SetAllCardInteraction(IReadOnlyList<CardData> cards, bool interactable)
    {
        for (int i = 0; i < activeCardCount; i++)
        {
            bool canInteract = interactable && !cards[i].IsConsumed;
            cardViews[i].SetInteractable(canInteract);
        }
    }

    public void SetCardInteractionForRow(IReadOnlyList<CardData> cards, int rowContentIndex, bool interactable)
    {
        int rowStartIndex = rowContentIndex * 3;
        int rowEndIndex = rowStartIndex + 2;

        for (int i = 0; i < activeCardCount; i++)
        {
            bool isInActiveRow = i >= rowStartIndex && i <= rowEndIndex;
            bool canInteract = interactable && isInActiveRow && !cards[i].IsConsumed;
            cardViews[i].SetInteractable(canInteract);
        }
    }

    public void SetBoardNavigationEnabled(bool enabled)
    {
        if (boardScrollRect != null)
        {
            boardScrollRect.horizontal = false;
            boardScrollRect.vertical = enabled;

            if (!enabled)
            {
                boardScrollRect.StopMovement();
            }
        }
    }

    public void ResetBoardScrollToTop()
    {
        if (boardScrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        RebuildBoardLayout();
        boardScrollRect.verticalNormalizedPosition = 1f;
    }

    public void JumpToRow(int rowContentIndex, int totalRows)
    {
        if (boardScrollRect == null)
        {
            return;
        }

        RebuildBoardLayout();
        boardScrollRect.StopMovement();
        boardScrollRect.verticalNormalizedPosition = GetNormalizedPositionForRow(rowContentIndex, totalRows);
    }

    public IEnumerator PlayBoardPreviewPan(int rowCount, float perRowDuration)
    {
        if (boardScrollRect == null || rowCount <= 1)
        {
            yield break;
        }

        RebuildBoardLayout();
        boardScrollRect.verticalNormalizedPosition = 1f;

        float duration = Mathf.Max(0f, rowCount - 1) * perRowDuration;
        if (duration <= 0f)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            boardScrollRect.verticalNormalizedPosition = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        boardScrollRect.verticalNormalizedPosition = 0f;
    }

    public IEnumerator PlayScrollToRow(int fromRowContentIndex, int toRowContentIndex, int totalRows, float duration)
    {
        if (boardScrollRect == null)
        {
            yield break;
        }

        RebuildBoardLayout();
        boardScrollRect.StopMovement();

        float start = GetNormalizedPositionForRow(fromRowContentIndex, totalRows);
        float target = GetNormalizedPositionForRow(toRowContentIndex, totalRows);
        boardScrollRect.verticalNormalizedPosition = start;

        if (duration <= 0f)
        {
            boardScrollRect.verticalNormalizedPosition = target;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            boardScrollRect.verticalNormalizedPosition = Mathf.Lerp(start, target, t);
            yield return null;
        }

        boardScrollRect.verticalNormalizedPosition = target;
        boardScrollRect.StopMovement();
    }

    public void SetStatusMessage(string message, StatusTone tone)
    {
        statusMessageText.text = message;
        statusMessageText.color = GetToneColor(tone);
    }

    public void HideGameOver()
    {
        gameOverPanel.SetActive(false);
    }

    public void ShowGameOver(int currentTowerHeight, int bestTowerHeight)
    {
        gameOverPanel.SetActive(true);
        gameOverText.text = $"게임 오버\n도달 타워 높이: {currentTowerHeight}\n최고 타워 높이: {bestTowerHeight}";
    }

    public bool HasValidReferences()
    {
        return currentFloorText != null &&
            bestFloorText != null &&
            shieldStatusText != null &&
            curseStatusText != null &&
            statusMessageText != null &&
            boardScrollRect != null &&
            cardContentRoot != null &&
            cardViewPrefab != null &&
            cardViewPrefab.HasValidReferences() &&
            gameOverPanel != null &&
            gameOverText != null &&
            restartButton != null;
    }

    private void EnsureCardPoolSize(int requiredCount)
    {
        while (cardViews.Count < requiredCount)
        {
            CardView cardView = Instantiate(cardViewPrefab, cardContentRoot);
            cardView.name = $"CardSlot_{cardViews.Count + 1}";
            cardViews.Add(cardView);
        }
    }

    private void RebuildBoardLayout()
    {
        if (cardContentRoot == null)
        {
            return;
        }

        ConfigureContentRectForScrolling();
        ApplyCenteredEdgePadding();
        ResizeContentHeight();
        LayoutRebuilder.ForceRebuildLayoutImmediate(cardContentRoot);
        Canvas.ForceUpdateCanvases();
    }

    private float GetNormalizedPositionForRow(int rowContentIndex, int totalRows)
    {
        if (boardScrollRect == null || cardContentRoot == null || totalRows <= 0)
        {
            return 1f;
        }

        RectTransform viewport = boardScrollRect.viewport != null
            ? boardScrollRect.viewport
            : boardScrollRect.GetComponent<RectTransform>();

        GridLayoutGroup gridLayoutGroup = cardContentRoot.GetComponent<GridLayoutGroup>();
        float viewportHeight = viewport.rect.height;
        float contentHeight = cardContentRoot.rect.height;
        float maxScrollOffset = Mathf.Max(0f, contentHeight - viewportHeight);

        if (maxScrollOffset <= 0f)
        {
            return 1f;
        }

        float cellHeight = GetRowCellHeight(gridLayoutGroup);
        float rowSpacing = gridLayoutGroup != null ? gridLayoutGroup.spacing.y : 0f;
        float paddingTop = gridLayoutGroup != null ? gridLayoutGroup.padding.top : 0f;
        float rowStride = cellHeight + rowSpacing;
        float rowCenterY = paddingTop + (rowContentIndex * rowStride) + (cellHeight * 0.5f);
        float targetOffset = Mathf.Clamp(rowCenterY - (viewportHeight * 0.5f), 0f, maxScrollOffset);

        return 1f - (targetOffset / maxScrollOffset);
    }

    private void ApplyCenteredEdgePadding()
    {
        if (boardScrollRect == null || cardContentRoot == null)
        {
            return;
        }

        GridLayoutGroup gridLayoutGroup = cardContentRoot.GetComponent<GridLayoutGroup>();
        if (gridLayoutGroup == null)
        {
            return;
        }

        CacheBaseGridPadding(gridLayoutGroup);

        RectTransform viewport = boardScrollRect.viewport != null
            ? boardScrollRect.viewport
            : boardScrollRect.GetComponent<RectTransform>();

        float cellHeight = GetRowCellHeight(gridLayoutGroup);
        int extraEdgePadding = Mathf.Max(0, Mathf.RoundToInt((viewport.rect.height - cellHeight) * 0.5f));
        int targetTopPadding = baseGridPaddingTop + extraEdgePadding;
        int targetBottomPadding = baseGridPaddingBottom + extraEdgePadding;

        if (gridLayoutGroup.padding.top == targetTopPadding && gridLayoutGroup.padding.bottom == targetBottomPadding)
        {
            return;
        }

        gridLayoutGroup.padding.top = targetTopPadding;
        gridLayoutGroup.padding.bottom = targetBottomPadding;
    }

    private void CacheBaseGridPadding(GridLayoutGroup gridLayoutGroup)
    {
        if (hasCachedBaseGridPadding)
        {
            return;
        }

        baseGridPaddingTop = gridLayoutGroup.padding.top;
        baseGridPaddingBottom = gridLayoutGroup.padding.bottom;
        hasCachedBaseGridPadding = true;
    }

    private void ConfigureContentRectForScrolling()
    {
        if (cardContentRoot == null)
        {
            return;
        }

        cardContentRoot.anchorMin = new Vector2(0f, 1f);
        cardContentRoot.anchorMax = new Vector2(1f, 1f);
        cardContentRoot.pivot = new Vector2(0.5f, 1f);
        cardContentRoot.anchoredPosition = Vector2.zero;
    }

    private void ResizeContentHeight()
    {
        if (cardContentRoot == null)
        {
            return;
        }

        GridLayoutGroup gridLayoutGroup = cardContentRoot.GetComponent<GridLayoutGroup>();
        RectTransform viewport = boardScrollRect != null && boardScrollRect.viewport != null
            ? boardScrollRect.viewport
            : boardScrollRect != null
                ? boardScrollRect.GetComponent<RectTransform>()
                : null;

        float viewportHeight = viewport != null ? viewport.rect.height : 0f;
        float contentHeight = GetRequiredContentHeight(gridLayoutGroup);
        float targetHeight = Mathf.Max(viewportHeight, contentHeight);
        cardContentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
    }

    private float GetRequiredContentHeight(GridLayoutGroup gridLayoutGroup)
    {
        if (gridLayoutGroup == null)
        {
            return cardContentRoot != null ? cardContentRoot.rect.height : 0f;
        }

        int columnCount = Mathf.Max(1, GetColumnCount(gridLayoutGroup));
        int rowCount = Mathf.CeilToInt(activeCardCount / (float)columnCount);
        if (rowCount <= 0)
        {
            return 0f;
        }

        float cellHeight = GetRowCellHeight(gridLayoutGroup);
        float spacingHeight = Mathf.Max(0, rowCount - 1) * gridLayoutGroup.spacing.y;
        return gridLayoutGroup.padding.top +
            gridLayoutGroup.padding.bottom +
            (rowCount * cellHeight) +
            spacingHeight;
    }

    private static int GetColumnCount(GridLayoutGroup gridLayoutGroup)
    {
        return gridLayoutGroup.constraint switch
        {
            GridLayoutGroup.Constraint.FixedColumnCount => gridLayoutGroup.constraintCount,
            GridLayoutGroup.Constraint.FixedRowCount => Mathf.Max(1, gridLayoutGroup.constraintCount),
            _ => 3
        };
    }

    private float GetRowCellHeight(GridLayoutGroup gridLayoutGroup)
    {
        if (gridLayoutGroup != null && gridLayoutGroup.cellSize.y > 0f)
        {
            return gridLayoutGroup.cellSize.y;
        }

        for (int i = 0; i < activeCardCount; i++)
        {
            RectTransform rectTransform = cardViews[i].transform as RectTransform;
            if (rectTransform != null)
            {
                return rectTransform.rect.height;
            }
        }

        return 0f;
    }

    private void HandleRestartClicked()
    {
        restartRequested?.Invoke();
    }

    private Color GetToneColor(StatusTone tone)
    {
        return tone switch
        {
            StatusTone.Good => goodColor,
            StatusTone.Bad => badColor,
            StatusTone.Warning => warningColor,
            _ => neutralColor
        };
    }
}
