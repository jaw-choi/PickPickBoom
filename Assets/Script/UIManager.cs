using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIManager : MonoBehaviour
{
    private sealed class BoardRowView
    {
        public RectTransform Root = null!;
        public Image Background = null!;
        public RectTransform CardAnchor = null!;
    }

    [Header("HUD")]
    [SerializeField] private TMP_Text currentFloorText = null!;
    [SerializeField] private TMP_Text bestFloorText = null!;
    [SerializeField] private Image shieldStatusImage;
    [HideInInspector, SerializeField] private TMP_Text shieldStatusText = null!;
    [HideInInspector, SerializeField] private TMP_Text curseStatusText = null!;
    [SerializeField] private TMP_Text statusMessageText = null!;

    [Header("Board")]
    [SerializeField] private ScrollRect boardScrollRect = null!;
    [SerializeField] private RectTransform cardContentRoot = null!;
    [SerializeField] private CardView cardViewPrefab = null!;

    [Header("Board Background")]
    [SerializeField] private RectTransform boardBackgroundRoot;
    [SerializeField] private Image boardWallImage;
    [SerializeField] private Image boardGroundImage;
    [SerializeField] private Image boardFinalImage;
    [SerializeField] private Sprite wallSprite;
    [SerializeField] private Sprite lastWallSprite;
    [SerializeField] private Sprite groundSprite;
    [SerializeField] private Sprite finalSprite;
    [SerializeField] private Color fallbackWallColor = new(0.24f, 0.28f, 0.33f, 1f);
    [SerializeField] private Color fallbackGroundColor = new(0.26f, 0.3f, 0.18f, 1f);
    [SerializeField] private Color fallbackFinalColor = new(0.68f, 0.58f, 0.28f, 1f);

    [Header("Board Layout Tuning")]
    [SerializeField] private Vector2 cardContentOffset;
    [SerializeField, Min(0.1f)] private float cardContentScale = 1f;
    [SerializeField, Min(0.1f)] private float cardWidthScale = 1f;
    [SerializeField, Min(0.1f)] private float cardHeightScale = 1f;
    [SerializeField] private bool alignCardContentFromBottom = true;
    [SerializeField] private Vector2 bottomAlignedCardOffset;
    [SerializeField, Range(-600f, 600f)] private float rowFocusOffsetY;
    [SerializeField, Range(0f, 1f)] private float previewPanEndNormalizedPosition;
    [SerializeField] private Vector2 wallTileOffset;
    [SerializeField, Min(0.1f)] private float wallTileScale = 1f;
    [SerializeField, Min(0.1f)] private float wallTileWidthScale = 1f;
    [SerializeField, Min(0.1f)] private float wallTileVisualHeightScale = 1f;
    [SerializeField, Min(0.1f)] private float wallTileHeightMultiplier = 1f;
    [SerializeField] private float wallTileSpacing;
    [SerializeField] private Vector2 groundOffset;
    [SerializeField, Min(0.1f)] private float groundScale = 1f;
    [SerializeField, Min(0.1f)] private float groundWidthScale = 1f;
    [SerializeField, Min(0.1f)] private float groundVisualHeightScale = 1f;
    [SerializeField, Min(0.1f)] private float groundHeightMultiplier = 1f;
    [SerializeField] private Vector2 finalOffset;
    [SerializeField, Min(0.1f)] private float finalHeightMultiplier = 1f;

    [Header("Intro")]
    [SerializeField] private GameObject introPanel;
    [SerializeField] private Button easyButton;
    [SerializeField] private Button normalButton;
    [SerializeField] private Button hardButton;
    [SerializeField] private TMP_Text easyBestScoreText;
    [SerializeField] private TMP_Text normalBestScoreText;
    [SerializeField] private TMP_Text hardBestScoreText;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel = null!;
    [SerializeField] private TMP_Text gameOverText = null!;
    [SerializeField] private TMP_Text gameOverReachedTowerHeightText;
    [SerializeField] private TMP_Text gameOverBestTowerHeightText;
    [SerializeField] private Button restartButton = null!;
    [SerializeField] private Button mainMenuButton = null!;

    [Header("Status Colors")]
    [SerializeField] private Color neutralColor = new(0.95f, 0.95f, 0.95f, 1f);
    [SerializeField] private Color goodColor = new(0.58f, 0.91f, 0.63f, 1f);
    [SerializeField] private Color badColor = new(1f, 0.57f, 0.57f, 1f);
    [SerializeField] private Color warningColor = new(1f, 0.86f, 0.46f, 1f);

    [Header("Danger FX")]
    [SerializeField] private CanvasGroup dangerPulseGroup;
    [SerializeField] private Color dangerPulseColor = new(1f, 0.03f, 0.02f, 0.22f);
    [SerializeField, Min(0.01f)] private float dangerPulseDuration = 0.42f;
    [SerializeField, Min(0f)] private float dangerPulsePeakScale = 1.08f;

    private readonly List<CardView> cardViews = new();
    private Action restartRequested;
    private Action mainMenuRequested;
    private Action<GameDifficulty> difficultySelected;
    private int activeCardCount;
    private bool hasCachedBaseGridPadding;
    private bool hasCachedBaseCardMetrics;
    private int baseGridPaddingTop;
    private int baseGridPaddingBottom;
    private Vector2 baseCardCellSize;
    private Vector2 baseCardSpacing;
    private Coroutine dangerPulseRoutine;
    private RectTransform dangerPulseRect;
    private RectTransform boardWallRect;
    private RectTransform boardGroundRect;
    private RectTransform boardFinalRect;
    private readonly List<Image> boardWallTiles = new();
    private readonly List<BoardRowView> boardRows = new();
    private bool hasCachedBoardTuning;
    private Vector2 cachedCardContentOffset;
    private float cachedCardContentScale;
    private float cachedCardWidthScale;
    private float cachedCardHeightScale;
    private bool cachedAlignCardContentFromBottom;
    private Vector2 cachedBottomAlignedCardOffset;
    private float cachedRowFocusOffsetY;
    private float cachedPreviewPanEndNormalizedPosition;
    private Vector2 cachedWallTileOffset;
    private float cachedWallTileScale;
    private float cachedWallTileWidthScale;
    private float cachedWallTileVisualHeightScale;
    private float cachedWallTileHeightMultiplier;
    private float cachedWallTileSpacing;
    private Vector2 cachedGroundOffset;
    private float cachedGroundScale;
    private float cachedGroundWidthScale;
    private float cachedGroundVisualHeightScale;
    private float cachedGroundHeightMultiplier;
    private Vector2 cachedFinalOffset;
    private float cachedFinalHeightMultiplier;
    private Sprite cachedWallSprite;
    private Sprite cachedLastWallSprite;
    private Sprite cachedGroundSprite;
    private Sprite cachedFinalSprite;
    private Color cachedFallbackWallColor;
    private Color cachedFallbackGroundColor;
    private Color cachedFallbackFinalColor;

    private enum DangerEdgeDirection
    {
        Top,
        Bottom,
        Left,
        Right
    }

    public int ActiveCardCount => activeCardCount;

    private void Awake()
    {
        CreateDefaultIntroPanelIfNeeded();
        CreateDefaultGameOverButtonsIfNeeded();
        CreateDefaultBoardBackgroundIfNeeded();
        CreateDefaultDangerPulseIfNeeded();

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(HandleRestartClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
        }

        if (easyButton != null)
        {
            easyButton.onClick.AddListener(HandleEasyClicked);
        }

        if (normalButton != null)
        {
            normalButton.onClick.AddListener(HandleNormalClicked);
        }

        if (hardButton != null)
        {
            hardButton.onClick.AddListener(HandleHardClicked);
        }

        if (gameOverPanel != null)
        {
            HideGameOver();
        }

        ApplyTopBarStatusVisuals(false);
    }

    private void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(HandleRestartClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
        }

        if (easyButton != null)
        {
            easyButton.onClick.RemoveListener(HandleEasyClicked);
        }

        if (normalButton != null)
        {
            normalButton.onClick.RemoveListener(HandleNormalClicked);
        }

        if (hardButton != null)
        {
            hardButton.onClick.RemoveListener(HandleHardClicked);
        }
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        RefreshBoardTuningIfChanged();
    }

    public bool HasIntroPanel => introPanel != null;

    public void Initialize(
        Action restartCallback,
        Action<GameDifficulty> difficultySelectedCallback,
        Action mainMenuCallback)
    {
        restartRequested = restartCallback;
        difficultySelected = difficultySelectedCallback;
        mainMenuRequested = mainMenuCallback;
    }

    public void ShowIntro()
    {
        if (introPanel != null)
        {
            introPanel.SetActive(true);
        }

        if (gameOverPanel != null)
        {
            HideGameOver();
        }

        SetAllActiveCards(false);
        SetBoardNavigationEnabled(false);
        SetBoardVisible(false);
        SetTopBarVisible(false);
    }

    public void HideIntro()
    {
        if (introPanel != null)
        {
            introPanel.SetActive(false);
        }

        SetBoardVisible(true);
        SetTopBarVisible(true);
    }

    public void SetIntroBestScores(int easyBestScore, int normalBestScore, int hardBestScore)
    {
        SetBestScoreText(easyBestScoreText, easyBestScore);
        SetBestScoreText(normalBestScoreText, normalBestScore);
        SetBestScoreText(hardBestScoreText, hardBestScore);
    }

    public void BindFloorCards(IReadOnlyList<CardData> cards, Action<int> selectedCallback)
    {
        EnsureCardPoolSize(cards.Count);
        activeCardCount = cards.Count;
        EnsureBoardRowPoolSize(GetBoardRowCount());

        for (int i = 0; i < cardViews.Count; i++)
        {
            bool isActive = i < activeCardCount;
            cardViews[i].gameObject.SetActive(isActive);

            if (isActive)
            {
                cardViews[i].Bind(cards[i], i, selectedCallback);
            }
        }

        ConfigureBoardRows(cards);
        RebuildBoardLayout();
        ResetBoardScrollToTop();
    }

    public CardView GetCardView(int index)
    {
        return cardViews[index];
    }

    public void RefreshHud(int currentTowerHeight, int bestTowerHeight, int activeFloorNumber, PlayerState playerState)
    {
        currentFloorText.text = $" {activeFloorNumber} / {currentTowerHeight}";
        bestFloorText.text = $" {bestTowerHeight}";
        ApplyTopBarStatusVisuals(playerState.HasShield);
    }

    private void ApplyTopBarStatusVisuals(bool hasShield)
    {
        if (shieldStatusImage != null)
        {
            shieldStatusImage.gameObject.SetActive(hasShield);
        }

        if (shieldStatusText != null)
        {
            shieldStatusText.gameObject.SetActive(false);
        }

        if (curseStatusText != null)
        {
            curseStatusText.gameObject.SetActive(false);
        }
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

    private void SetBoardVisible(bool visible)
    {
        if (boardScrollRect != null)
        {
            boardScrollRect.gameObject.SetActive(visible);
        }
    }

    private void SetTopBarVisible(bool visible)
    {
        if (currentFloorText != null && currentFloorText.transform.parent != null)
        {
            currentFloorText.transform.parent.gameObject.SetActive(visible);
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
        UpdateBoardBackground(1f);
    }

    public void JumpToRow(int rowContentIndex, int totalRows)
    {
        if (boardScrollRect == null)
        {
            return;
        }

        RebuildBoardLayout();
        boardScrollRect.StopMovement();
        float targetPosition = GetNormalizedPositionForRow(rowContentIndex, totalRows);
        boardScrollRect.verticalNormalizedPosition = targetPosition;
        UpdateBoardBackground(targetPosition);
    }

    public IEnumerator PlayBoardPreviewPan(int rowCount, float perRowDuration)
    {
        if (boardScrollRect == null || rowCount <= 1)
        {
            yield break;
        }

        RebuildBoardLayout();
        boardScrollRect.verticalNormalizedPosition = 1f;
        UpdateBoardBackground(1f);
        float previewPanEndPosition = Mathf.Clamp01(previewPanEndNormalizedPosition);

        float duration = Mathf.Max(0f, rowCount - 1) * perRowDuration;
        if (duration <= 0f)
        {
            boardScrollRect.verticalNormalizedPosition = previewPanEndPosition;
            UpdateBoardBackground(previewPanEndPosition);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float position = Mathf.Lerp(1f, previewPanEndPosition, t);
            boardScrollRect.verticalNormalizedPosition = position;
            UpdateBoardBackground(position);
            yield return null;
        }

        boardScrollRect.verticalNormalizedPosition = previewPanEndPosition;
        UpdateBoardBackground(previewPanEndPosition);
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
        UpdateBoardBackground(start);

        if (duration <= 0f)
        {
            boardScrollRect.verticalNormalizedPosition = target;
            UpdateBoardBackground(target);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float position = Mathf.Lerp(start, target, t);
            boardScrollRect.verticalNormalizedPosition = position;
            UpdateBoardBackground(position);
            yield return null;
        }

        boardScrollRect.verticalNormalizedPosition = target;
        UpdateBoardBackground(target);
        boardScrollRect.StopMovement();
    }

    public void SetStatusMessage(string message, StatusTone tone)
    {
        statusMessageText.text = message;
        statusMessageText.color = GetToneColor(tone);
    }

    public void PlayDangerPulse()
    {
        if (dangerPulseGroup == null)
        {
            return;
        }

        if (dangerPulseRoutine != null)
        {
            StopCoroutine(dangerPulseRoutine);
        }

        dangerPulseRoutine = StartCoroutine(PlayDangerPulseRoutine());
    }

    public void HideGameOver()
    {
        gameOverPanel.SetActive(false);
    }

    public void ShowGameOver(int currentTowerHeight, int bestTowerHeight)
    {
        gameOverPanel.SetActive(true);
        if (gameOverReachedTowerHeightText != null || gameOverBestTowerHeightText != null)
        {
            if (gameOverText != null)
            {
                gameOverText.text = "게임 오버";
            }

            if (gameOverReachedTowerHeightText != null)
            {
                gameOverReachedTowerHeightText.text = currentTowerHeight.ToString();
            }

            if (gameOverBestTowerHeightText != null)
            {
                gameOverBestTowerHeightText.text = bestTowerHeight.ToString();
            }

            return;
        }
        gameOverText.text = $"게임 오버\n도달 타워 높이: {currentTowerHeight}\n최고 타워 높이: {bestTowerHeight}";
    }

    public bool HasValidReferences()
    {
        return currentFloorText != null &&
            bestFloorText != null &&
            statusMessageText != null &&
            boardScrollRect != null &&
            cardContentRoot != null &&
            cardViewPrefab != null &&
            cardViewPrefab.HasValidReferences() &&
            HasValidIntroReferences() &&
            gameOverPanel != null &&
            gameOverText != null &&
            restartButton != null &&
            mainMenuButton != null;
    }

    private bool HasValidIntroReferences()
    {
        return introPanel == null ||
            easyButton != null &&
            normalButton != null &&
            hardButton != null &&
            easyBestScoreText != null &&
            normalBestScoreText != null &&
            hardBestScoreText != null;
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

    private void EnsureBoardRowPoolSize(int requiredCount)
    {
        while (boardRows.Count < requiredCount)
        {
            boardRows.Add(CreateBoardRowView(boardRows.Count));
        }

        for (int i = 0; i < boardRows.Count; i++)
        {
            boardRows[i].Root.gameObject.SetActive(i < requiredCount);
        }
    }

    private BoardRowView CreateBoardRowView(int rowIndex)
    {
        GameObject rowObject = new GameObject($"BoardRow_{rowIndex + 1}", typeof(RectTransform), typeof(Image));
        rowObject.transform.SetParent(cardContentRoot, false);

        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);

        Image backgroundImage = rowObject.GetComponent<Image>();
        backgroundImage.raycastTarget = false;
        backgroundImage.type = Image.Type.Simple;
        backgroundImage.preserveAspect = false;

        GameObject cardAnchorObject = new GameObject("Cards", typeof(RectTransform));
        cardAnchorObject.transform.SetParent(rowObject.transform, false);
        RectTransform cardAnchorRect = cardAnchorObject.GetComponent<RectTransform>();
        cardAnchorRect.anchorMin = Vector2.zero;
        cardAnchorRect.anchorMax = Vector2.one;
        cardAnchorRect.offsetMin = Vector2.zero;
        cardAnchorRect.offsetMax = Vector2.zero;
        cardAnchorRect.pivot = new Vector2(0.5f, 0.5f);

        return new BoardRowView
        {
            Root = rowRect,
            Background = backgroundImage,
            CardAnchor = cardAnchorRect
        };
    }

    private void ConfigureBoardRows(IReadOnlyList<CardData> cards)
    {
        int rowCount = GetBoardRowCount();
        GridLayoutGroup gridLayoutGroup = cardContentRoot != null
            ? cardContentRoot.GetComponent<GridLayoutGroup>()
            : null;

        CacheBaseCardMetrics(gridLayoutGroup);

        for (int rowIndex = 0; rowIndex < boardRows.Count; rowIndex++)
        {
            bool isActiveRow = rowIndex < rowCount;
            BoardRowView rowView = boardRows[rowIndex];
            rowView.Root.gameObject.SetActive(isActiveRow);

            if (!isActiveRow)
            {
                continue;
            }

            ConfigureBoardRowVisual(rowView, rowIndex, rowCount);
            LayoutRowCards(rowView, rowIndex, cards);
        }
    }

    private void ConfigureBoardRowVisual(BoardRowView rowView, int rowIndex, int rowCount)
    {
        Sprite rowSprite = GetWallSpriteForTile(rowIndex, rowCount);
        rowView.Background.sprite = rowSprite;
        rowView.Background.color = rowSprite != null ? Color.white : fallbackWallColor;
        rowView.Background.preserveAspect = true;

        RectTransform backgroundRect = rowView.Background.rectTransform;
        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.anchoredPosition = wallTileOffset;
    }

    private void LayoutRowCards(BoardRowView rowView, int rowIndex, IReadOnlyList<CardData> cards)
    {
        Vector2 baseCardSize = GetBaseCardCellSize();
        Vector2 cardSize = new Vector2(
            baseCardSize.x * cardWidthScale,
            baseCardSize.y * cardHeightScale);
        float cardSpacingX = GetBaseCardSpacingX();
        float totalWidth = (cardSize.x * 3f) + (cardSpacingX * 2f);
        float startX = (-totalWidth * 0.5f) + (cardSize.x * 0.5f);
        int rowStartIndex = rowIndex * 3;

        for (int slotIndex = 0; slotIndex < 3; slotIndex++)
        {
            int cardIndex = rowStartIndex + slotIndex;
            if (cardIndex >= cards.Count || cardIndex >= cardViews.Count)
            {
                continue;
            }

            RectTransform cardRect = cardViews[cardIndex].transform as RectTransform;
            if (cardRect == null)
            {
                continue;
            }

            cardRect.SetParent(rowView.CardAnchor, false);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardSize.x);
            cardRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cardSize.y);
            cardRect.anchoredPosition = new Vector2(startX + (slotIndex * (cardSize.x + cardSpacingX)), 0f);
            cardRect.localScale = Vector3.one;
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
        UpdateBoardRowLayout();
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
        float targetOffset = Mathf.Clamp(rowCenterY - (viewportHeight * 0.5f) + rowFocusOffsetY, 0f, maxScrollOffset);

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

    private void CacheBaseCardMetrics(GridLayoutGroup gridLayoutGroup)
    {
        if (hasCachedBaseCardMetrics || gridLayoutGroup == null)
        {
            return;
        }

        baseCardCellSize = gridLayoutGroup.cellSize;
        baseCardSpacing = gridLayoutGroup.spacing;
        hasCachedBaseCardMetrics = true;
    }

    private void ConfigureContentRectForScrolling()
    {
        if (cardContentRoot == null)
        {
            return;
        }

        GridLayoutGroup gridLayoutGroup = cardContentRoot.GetComponent<GridLayoutGroup>();
        ConfigureRowGridLayout(gridLayoutGroup);
        cardContentRoot.anchorMin = new Vector2(0f, 1f);
        cardContentRoot.anchorMax = new Vector2(1f, 1f);
        cardContentRoot.pivot = new Vector2(0.5f, 1f);
        cardContentRoot.anchoredPosition = GetCardContentAnchoredPosition();
        cardContentRoot.localScale = new Vector3(cardContentScale, cardContentScale, 1f);
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
        cardContentRoot.anchoredPosition = GetCardContentAnchoredPosition();
    }

    private float GetRequiredContentHeight(GridLayoutGroup gridLayoutGroup)
    {
        if (gridLayoutGroup == null)
        {
            return cardContentRoot != null ? cardContentRoot.rect.height : 0f;
        }

        int columnCount = Mathf.Max(1, GetColumnCount(gridLayoutGroup));
        int rowCount = Mathf.CeilToInt(GetBoardRowCount() / (float)columnCount);
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

    private void CreateDefaultBoardBackgroundIfNeeded()
    {
        if (boardBackgroundRoot != null || boardScrollRect == null)
        {
            CacheBackgroundRects();
            return;
        }

        RectTransform scrollRectTransform = boardScrollRect.transform as RectTransform;
        if (scrollRectTransform == null)
        {
            return;
        }

        GameObject backgroundRootObject = new GameObject("BoardBackground", typeof(RectTransform));
        backgroundRootObject.transform.SetParent(scrollRectTransform, false);
        backgroundRootObject.transform.SetAsFirstSibling();

        boardBackgroundRoot = backgroundRootObject.GetComponent<RectTransform>();
        boardBackgroundRoot.anchorMin = Vector2.zero;
        boardBackgroundRoot.anchorMax = Vector2.one;
        boardBackgroundRoot.offsetMin = Vector2.zero;
        boardBackgroundRoot.offsetMax = Vector2.zero;

        boardWallImage = CreateBackgroundImage("WallTileTemplate", boardBackgroundRoot, wallSprite, fallbackWallColor);
        boardWallImage.gameObject.SetActive(false);
        boardGroundImage = CreateBackgroundImage("Ground", boardBackgroundRoot, groundSprite, fallbackGroundColor);
        boardFinalImage = CreateBackgroundImage("FinalFloor", boardBackgroundRoot, finalSprite, fallbackFinalColor);
        CacheBackgroundRects();
        UpdateBoardBackground(1f);
    }

    private Image CreateBackgroundImage(string objectName, Transform parent, Sprite sprite, Color fallbackColor)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : fallbackColor;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.type = Image.Type.Simple;
        return image;
    }

    private Vector2 GetCardContentAnchoredPosition()
    {
        if (!alignCardContentFromBottom || boardScrollRect == null || cardContentRoot == null)
        {
            return cardContentOffset;
        }

        RectTransform viewport = boardScrollRect.viewport != null
            ? boardScrollRect.viewport
            : boardScrollRect.GetComponent<RectTransform>();
        float viewportHeight = viewport != null ? viewport.rect.height : 0f;
        float contentHeight = cardContentRoot.rect.height;
        float bottomCompensation = Mathf.Max(0f, contentHeight - viewportHeight);

        return cardContentOffset + bottomAlignedCardOffset + new Vector2(0f, bottomCompensation);
    }

    private void RefreshBoardTuningIfChanged()
    {
        if (!HasBoardTuningChanged())
        {
            return;
        }

        bool shouldRefreshBoardBackground = HasBoardBackgroundTuningChanged();
        CacheBoardTuning();
        RebuildBoardLayout();

        if (shouldRefreshBoardBackground && boardScrollRect != null)
        {
            UpdateBoardBackground(boardScrollRect.verticalNormalizedPosition);
        }
    }

    private bool HasBoardTuningChanged()
    {
        if (!hasCachedBoardTuning)
        {
            return true;
        }

        return cachedCardContentOffset != cardContentOffset ||
            !Mathf.Approximately(cachedCardContentScale, cardContentScale) ||
            !Mathf.Approximately(cachedCardWidthScale, cardWidthScale) ||
            !Mathf.Approximately(cachedCardHeightScale, cardHeightScale) ||
            cachedAlignCardContentFromBottom != alignCardContentFromBottom ||
            cachedBottomAlignedCardOffset != bottomAlignedCardOffset ||
            !Mathf.Approximately(cachedRowFocusOffsetY, rowFocusOffsetY) ||
            !Mathf.Approximately(cachedPreviewPanEndNormalizedPosition, previewPanEndNormalizedPosition) ||
            cachedWallTileOffset != wallTileOffset ||
            !Mathf.Approximately(cachedWallTileScale, wallTileScale) ||
            !Mathf.Approximately(cachedWallTileWidthScale, wallTileWidthScale) ||
            !Mathf.Approximately(cachedWallTileVisualHeightScale, wallTileVisualHeightScale) ||
            !Mathf.Approximately(cachedWallTileHeightMultiplier, wallTileHeightMultiplier) ||
            !Mathf.Approximately(cachedWallTileSpacing, wallTileSpacing) ||
            cachedGroundOffset != groundOffset ||
            !Mathf.Approximately(cachedGroundScale, groundScale) ||
            !Mathf.Approximately(cachedGroundWidthScale, groundWidthScale) ||
            !Mathf.Approximately(cachedGroundVisualHeightScale, groundVisualHeightScale) ||
            !Mathf.Approximately(cachedGroundHeightMultiplier, groundHeightMultiplier) ||
            cachedFinalOffset != finalOffset ||
            !Mathf.Approximately(cachedFinalHeightMultiplier, finalHeightMultiplier) ||
            cachedWallSprite != wallSprite ||
            cachedLastWallSprite != lastWallSprite ||
            cachedGroundSprite != groundSprite ||
            cachedFinalSprite != finalSprite ||
            cachedFallbackWallColor != fallbackWallColor ||
            cachedFallbackGroundColor != fallbackGroundColor ||
            cachedFallbackFinalColor != fallbackFinalColor;
    }

    private bool HasBoardBackgroundTuningChanged()
    {
        if (!hasCachedBoardTuning)
        {
            return true;
        }

        return !Mathf.Approximately(cachedWallTileScale, wallTileScale) ||
            !Mathf.Approximately(cachedWallTileWidthScale, wallTileWidthScale) ||
            !Mathf.Approximately(cachedWallTileVisualHeightScale, wallTileVisualHeightScale) ||
            !Mathf.Approximately(cachedWallTileHeightMultiplier, wallTileHeightMultiplier) ||
            !Mathf.Approximately(cachedWallTileSpacing, wallTileSpacing) ||
            cachedWallTileOffset != wallTileOffset ||
            cachedGroundOffset != groundOffset ||
            !Mathf.Approximately(cachedGroundScale, groundScale) ||
            !Mathf.Approximately(cachedGroundWidthScale, groundWidthScale) ||
            !Mathf.Approximately(cachedGroundVisualHeightScale, groundVisualHeightScale) ||
            !Mathf.Approximately(cachedGroundHeightMultiplier, groundHeightMultiplier) ||
            cachedFinalOffset != finalOffset ||
            !Mathf.Approximately(cachedFinalHeightMultiplier, finalHeightMultiplier) ||
            cachedWallSprite != wallSprite ||
            cachedLastWallSprite != lastWallSprite ||
            cachedGroundSprite != groundSprite ||
            cachedFinalSprite != finalSprite ||
            cachedFallbackWallColor != fallbackWallColor ||
            cachedFallbackGroundColor != fallbackGroundColor ||
            cachedFallbackFinalColor != fallbackFinalColor;
    }

    private void CacheBoardTuning()
    {
        hasCachedBoardTuning = true;
        cachedCardContentOffset = cardContentOffset;
        cachedCardContentScale = cardContentScale;
        cachedCardWidthScale = cardWidthScale;
        cachedCardHeightScale = cardHeightScale;
        cachedAlignCardContentFromBottom = alignCardContentFromBottom;
        cachedBottomAlignedCardOffset = bottomAlignedCardOffset;
        cachedRowFocusOffsetY = rowFocusOffsetY;
        cachedPreviewPanEndNormalizedPosition = previewPanEndNormalizedPosition;
        cachedWallTileOffset = wallTileOffset;
        cachedWallTileScale = wallTileScale;
        cachedWallTileWidthScale = wallTileWidthScale;
        cachedWallTileVisualHeightScale = wallTileVisualHeightScale;
        cachedWallTileHeightMultiplier = wallTileHeightMultiplier;
        cachedWallTileSpacing = wallTileSpacing;
        cachedGroundOffset = groundOffset;
        cachedGroundScale = groundScale;
        cachedGroundWidthScale = groundWidthScale;
        cachedGroundVisualHeightScale = groundVisualHeightScale;
        cachedGroundHeightMultiplier = groundHeightMultiplier;
        cachedFinalOffset = finalOffset;
        cachedFinalHeightMultiplier = finalHeightMultiplier;
        cachedWallSprite = wallSprite;
        cachedLastWallSprite = lastWallSprite;
        cachedGroundSprite = groundSprite;
        cachedFinalSprite = finalSprite;
        cachedFallbackWallColor = fallbackWallColor;
        cachedFallbackGroundColor = fallbackGroundColor;
        cachedFallbackFinalColor = fallbackFinalColor;
    }

    private void CacheBackgroundRects()
    {
        boardWallRect = boardWallImage != null ? boardWallImage.transform as RectTransform : null;
        boardGroundRect = boardGroundImage != null ? boardGroundImage.transform as RectTransform : null;
        boardFinalRect = boardFinalImage != null ? boardFinalImage.transform as RectTransform : null;
    }

    private void UpdateBoardBackground(float scrollNormalizedPosition)
    {
        if (boardBackgroundRoot == null)
        {
            return;
        }

        CacheBackgroundRects();

        RectTransform viewport = boardScrollRect != null && boardScrollRect.viewport != null
            ? boardScrollRect.viewport
            : boardScrollRect != null
                ? boardScrollRect.GetComponent<RectTransform>()
                : null;

        float viewportHeight = viewport != null ? viewport.rect.height : boardBackgroundRoot.rect.height;
        float viewportWidth = viewport != null ? viewport.rect.width : boardBackgroundRoot.rect.width;
        float contentHeight = Mathf.Max(viewportHeight, cardContentRoot != null ? cardContentRoot.rect.height : viewportHeight);
        float scrollOffset = (1f - Mathf.Clamp01(scrollNormalizedPosition)) * Mathf.Max(0f, contentHeight - viewportHeight);
        float rowHeight = GetBoardRowHeight();
        float rowStep = rowHeight + GetBaseCardSpacingY();
        int rowCount = GetBoardWallRowCount();

        for (int i = 0; i < boardWallTiles.Count; i++)
        {
            boardWallTiles[i].gameObject.SetActive(false);
        }

        if (boardWallImage != null)
        {
            boardWallImage.gameObject.SetActive(false);
        }

        if (boardGroundRect != null)
        {
            boardGroundImage.sprite = groundSprite;
            boardGroundImage.color = groundSprite != null ? Color.white : fallbackGroundColor;
            boardGroundImage.preserveAspect = true;

            float groundHeight = Mathf.Max(90f, viewportHeight * 0.16f) * groundHeightMultiplier;
            Vector2 groundBasePosition = new Vector2(0f, scrollOffset - (rowCount * rowStep));

            boardGroundRect.anchorMin = new Vector2(0.5f, 1f);
            boardGroundRect.anchorMax = new Vector2(0.5f, 1f);
            boardGroundRect.pivot = new Vector2(0.5f, 1f);
            if (groundSprite != null)
            {
                boardGroundImage.SetNativeSize();
            }
            else
            {
                boardGroundRect.sizeDelta = new Vector2(viewportWidth, groundHeight);
            }

            boardGroundRect.localScale = new Vector3(
                groundScale * groundWidthScale,
                groundScale * groundVisualHeightScale * groundHeightMultiplier,
                1f);

            if (groundSprite == null)
            {
                boardGroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, groundHeight);
            }

            boardGroundRect.anchoredPosition = groundBasePosition + groundOffset;

            float groundTopY = boardGroundRect.anchoredPosition.y;
            bool isGroundVisible = groundTopY > -viewportHeight;
            boardGroundRect.gameObject.SetActive(isGroundVisible);
        }

        if (boardFinalRect != null)
        {
            boardFinalRect.gameObject.SetActive(false);
        }
    }

    private float GetBackgroundRowHeight()
    {
        return GetBoardRowHeight() + GetBaseCardSpacingY();
    }

    private void ConfigureRowGridLayout(GridLayoutGroup gridLayoutGroup)
    {
        if (gridLayoutGroup == null || boardScrollRect == null)
        {
            return;
        }

        CacheBaseCardMetrics(gridLayoutGroup);

        RectTransform viewport = boardScrollRect.viewport != null
            ? boardScrollRect.viewport
            : boardScrollRect.GetComponent<RectTransform>();
        float viewportWidth = viewport != null ? viewport.rect.width : cardContentRoot.rect.width;

        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = 1;
        gridLayoutGroup.startAxis = GridLayoutGroup.Axis.Vertical;
        gridLayoutGroup.cellSize = new Vector2(viewportWidth, GetBoardRowHeight());
        gridLayoutGroup.spacing = new Vector2(0f, GetBaseCardSpacingY());
    }

    private void UpdateBoardRowLayout()
    {
        if (cardContentRoot == null)
        {
            return;
        }

        GridLayoutGroup gridLayoutGroup = cardContentRoot.GetComponent<GridLayoutGroup>();
        float rowHeight = GetBoardRowHeight();
        float rowWidth = gridLayoutGroup != null ? gridLayoutGroup.cellSize.x : cardContentRoot.rect.width;
        int rowCount = GetBoardWallRowCount();

        for (int rowIndex = 0; rowIndex < boardRows.Count; rowIndex++)
        {
            BoardRowView rowView = boardRows[rowIndex];
            if (!rowView.Root.gameObject.activeSelf)
            {
                continue;
            }

            RectTransform backgroundRect = rowView.Background.rectTransform;
            Sprite rowSprite = GetWallSpriteForTile(rowIndex, rowCount);
            rowView.Background.sprite = rowSprite;
            rowView.Background.color = rowSprite != null ? Color.white : fallbackWallColor;

            if (rowSprite != null)
            {
                rowView.Background.SetNativeSize();
            }
            else
            {
                backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rowWidth);
                backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rowHeight);
            }

            backgroundRect.anchoredPosition = wallTileOffset;
            backgroundRect.localScale = new Vector3(
                wallTileScale * wallTileWidthScale,
                wallTileScale * wallTileVisualHeightScale * wallTileHeightMultiplier,
                1f);
        }
    }

    private float GetBoardRowHeight()
    {
        return Mathf.Max(1f, GetBaseCardCellSize().y);
    }

    private Vector2 GetBaseCardCellSize()
    {
        if (hasCachedBaseCardMetrics)
        {
            return baseCardCellSize;
        }

        GridLayoutGroup gridLayoutGroup = cardContentRoot != null
            ? cardContentRoot.GetComponent<GridLayoutGroup>()
            : null;
        CacheBaseCardMetrics(gridLayoutGroup);
        return hasCachedBaseCardMetrics ? baseCardCellSize : new Vector2(341f, 666f);
    }

    private float GetBaseCardSpacingX()
    {
        if (hasCachedBaseCardMetrics)
        {
            return baseCardSpacing.x;
        }

        GridLayoutGroup gridLayoutGroup = cardContentRoot != null
            ? cardContentRoot.GetComponent<GridLayoutGroup>()
            : null;
        CacheBaseCardMetrics(gridLayoutGroup);
        return hasCachedBaseCardMetrics ? baseCardSpacing.x : 20f;
    }

    private float GetBaseCardSpacingY()
    {
        if (hasCachedBaseCardMetrics)
        {
            return baseCardSpacing.y + wallTileSpacing;
        }

        GridLayoutGroup gridLayoutGroup = cardContentRoot != null
            ? cardContentRoot.GetComponent<GridLayoutGroup>()
            : null;
        CacheBaseCardMetrics(gridLayoutGroup);
        return (hasCachedBaseCardMetrics ? baseCardSpacing.y : 100f) + wallTileSpacing;
    }

    private int GetBoardRowCount()
    {
        return Mathf.Max(0, Mathf.CeilToInt(activeCardCount / 3f));
    }

    private int GetBoardWallRowCount()
    {
        return Mathf.Max(1, GetBoardRowCount());
    }

    private void EnsureBoardWallTileCount(int rowCount)
    {
        while (boardWallTiles.Count < rowCount)
        {
            Image tileImage = CreateBackgroundImage($"WallTile_{boardWallTiles.Count + 1}", boardBackgroundRoot, wallSprite, fallbackWallColor);
            tileImage.transform.SetAsFirstSibling();
            boardWallTiles.Add(tileImage);
        }

        for (int i = 0; i < boardWallTiles.Count; i++)
        {
            boardWallTiles[i].gameObject.SetActive(i < rowCount);
            Sprite tileSprite = GetWallSpriteForTile(i, rowCount);
            boardWallTiles[i].sprite = tileSprite;
            boardWallTiles[i].color = tileSprite != null ? Color.white : fallbackWallColor;
            boardWallTiles[i].preserveAspect = true;
        }

        if (boardGroundRect != null)
        {
            boardGroundRect.transform.SetAsLastSibling();
        }

        if (boardFinalRect != null)
        {
            boardFinalRect.transform.SetAsLastSibling();
        }
    }

    private void UpdateBoardWallTiles(int rowCount, float rowHeight, float rowStep, float contentHeight, float scrollOffset, Vector2 bottomAnchoredWallOffset)
    {
        for (int i = 0; i < rowCount; i++)
        {
            RectTransform tileRect = boardWallTiles[i].transform as RectTransform;
            if (tileRect == null)
            {
                continue;
            }

            tileRect.anchorMin = new Vector2(0f, 1f);
            tileRect.anchorMax = new Vector2(1f, 1f);
            tileRect.pivot = new Vector2(0.5f, 1f);
            tileRect.sizeDelta = new Vector2(0f, rowHeight);
            float reversedRowIndex = rowCount - 1 - i;
            tileRect.anchoredPosition = new Vector2(0f, scrollOffset - (reversedRowIndex * rowStep)) + wallTileOffset + bottomAnchoredWallOffset;
            tileRect.localScale = new Vector3(
                wallTileScale * wallTileWidthScale,
                wallTileScale * wallTileVisualHeightScale,
                1f);
        }
    }

    private Sprite GetWallSpriteForTile(int tileIndex, int rowCount)
    {
        if (tileIndex == 0)
        {
            return GetLastWallSprite();
        }

        return wallSprite;
    }

    private Sprite GetLastWallSprite()
    {
        if (lastWallSprite != null)
        {
            return lastWallSprite;
        }

        if (finalSprite != null)
        {
            return finalSprite;
        }

        return wallSprite;
    }

    private void HandleRestartClicked()
    {
        restartRequested?.Invoke();
    }

    private void HandleMainMenuClicked()
    {
        mainMenuRequested?.Invoke();
    }

    private void HandleEasyClicked()
    {
        difficultySelected?.Invoke(GameDifficulty.Easy);
    }

    private void HandleNormalClicked()
    {
        difficultySelected?.Invoke(GameDifficulty.Normal);
    }

    private void HandleHardClicked()
    {
        difficultySelected?.Invoke(GameDifficulty.Hard);
    }

    private void SetAllActiveCards(bool active)
    {
        for (int i = 0; i < cardViews.Count; i++)
        {
            cardViews[i].gameObject.SetActive(active);
        }

        for (int i = 0; i < boardRows.Count; i++)
        {
            boardRows[i].Root.gameObject.SetActive(active && i < GetBoardRowCount());
        }

        if (!active)
        {
            activeCardCount = 0;
        }
    }

    private static void SetBestScoreText(TMP_Text targetText, int bestScore)
    {
        if (targetText != null)
        {
            targetText.text = $"BEST {bestScore}";
        }
    }

    private void CreateDefaultIntroPanelIfNeeded()
    {
        if (introPanel != null || boardScrollRect == null)
        {
            return;
        }

        Canvas canvas = boardScrollRect.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        introPanel = new GameObject("IntroPanel", typeof(RectTransform), typeof(Image));
        introPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = introPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = introPanel.GetComponent<Image>();
        panelImage.color = new Color(0.05f, 0.07f, 0.09f, 0.92f);

        CreateIntroText("Pick Pick Tower", new Vector2(0f, 210f), 42f, FontStyles.Bold);
        CreateIntroText("난이도를 선택하세요", new Vector2(0f, 150f), 24f, FontStyles.Normal);

        easyButton = CreateDifficultyButton("EasyButton", "EASY", new Vector2(0f, 60f), out easyBestScoreText);
        normalButton = CreateDifficultyButton("NormalButton", "NORMAL", new Vector2(0f, -45f), out normalBestScoreText);
        hardButton = CreateDifficultyButton("HardButton", "HARD", new Vector2(0f, -150f), out hardBestScoreText);
    }

    private void CreateDefaultGameOverButtonsIfNeeded()
    {
        if (gameOverPanel == null || restartButton == null || mainMenuButton != null)
        {
            return;
        }

        RectTransform restartRect = restartButton.transform as RectTransform;
        if (restartRect != null)
        {
            restartRect.anchoredPosition = new Vector2(-90f, restartRect.anchoredPosition.y);
            restartRect.sizeDelta = new Vector2(160f, restartRect.sizeDelta.y);
        }

        mainMenuButton = CreateGameOverButton("MainMenuButton", "MainMenu", new Vector2(90f, restartRect != null ? restartRect.anchoredPosition.y : -120f));
    }

    private Button CreateGameOverButton(string objectName, string label, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(gameOverPanel.transform, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(160f, 52f);
        buttonRect.anchoredPosition = anchoredPosition;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.92f, 0.78f, 0.45f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        TMP_Text labelText = CreateChildText(buttonObject.transform, $"{label}Label", label, Vector2.zero, 24f, FontStyles.Bold);
        labelText.color = new Color(0.12f, 0.09f, 0.05f, 1f);
        return button;
    }

    private void CreateDefaultDangerPulseIfNeeded()
    {
        if (dangerPulseGroup != null || boardScrollRect == null)
        {
            return;
        }

        Canvas canvas = boardScrollRect.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        GameObject pulseRoot = new GameObject("DangerPulseFX", typeof(RectTransform), typeof(CanvasGroup));
        pulseRoot.transform.SetParent(canvas.transform, false);

        dangerPulseRect = pulseRoot.GetComponent<RectTransform>();
        dangerPulseRect.anchorMin = Vector2.zero;
        dangerPulseRect.anchorMax = Vector2.one;
        dangerPulseRect.offsetMin = Vector2.zero;
        dangerPulseRect.offsetMax = Vector2.zero;

        dangerPulseGroup = pulseRoot.GetComponent<CanvasGroup>();
        dangerPulseGroup.alpha = 0f;
        dangerPulseGroup.blocksRaycasts = false;
        dangerPulseGroup.interactable = false;

        CreateDangerEdge("Top", DangerEdgeDirection.Top, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -64f), new Vector2(0f, 128f));
        CreateDangerEdge("Bottom", DangerEdgeDirection.Bottom, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 64f), new Vector2(0f, 128f));
        CreateDangerEdge("Left", DangerEdgeDirection.Left, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(64f, 0f), new Vector2(128f, 0f));
        CreateDangerEdge("Right", DangerEdgeDirection.Right, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-64f, 0f), new Vector2(128f, 0f));

        pulseRoot.transform.SetAsLastSibling();
    }

    private void CreateDangerEdge(
        string edgeName,
        DangerEdgeDirection direction,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        GameObject edgeObject = new GameObject($"DangerEdge_{edgeName}", typeof(RectTransform), typeof(Image));
        edgeObject.transform.SetParent(dangerPulseGroup.transform, false);

        RectTransform edgeRect = edgeObject.GetComponent<RectTransform>();
        edgeRect.anchorMin = anchorMin;
        edgeRect.anchorMax = anchorMax;
        edgeRect.anchoredPosition = anchoredPosition;
        edgeRect.sizeDelta = sizeDelta;

        Image edgeImage = edgeObject.GetComponent<Image>();
        edgeImage.sprite = CreateDangerGradientSprite(direction);
        edgeImage.color = dangerPulseColor;
        edgeImage.raycastTarget = false;
    }

    private static Sprite CreateDangerGradientSprite(DangerEdgeDirection direction)
    {
        const int textureSize = 64;
        Texture2D texture = new(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float normalizedX = x / (float)(textureSize - 1);
                float normalizedY = y / (float)(textureSize - 1);
                float edgeStrength = direction switch
                {
                    DangerEdgeDirection.Top => normalizedY,
                    DangerEdgeDirection.Bottom => 1f - normalizedY,
                    DangerEdgeDirection.Left => 1f - normalizedX,
                    DangerEdgeDirection.Right => normalizedX,
                    _ => 0f
                };

                float alpha = Mathf.SmoothStep(0f, 1f, edgeStrength);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
    }

    private IEnumerator PlayDangerPulseRoutine()
    {
        if (dangerPulseRect == null && dangerPulseGroup != null)
        {
            dangerPulseRect = dangerPulseGroup.transform as RectTransform;
        }

        float elapsed = 0f;
        float halfDuration = dangerPulseDuration * 0.5f;
        while (elapsed < dangerPulseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float pulse = halfDuration <= 0f
                ? 0f
                : Mathf.PingPong(elapsed, halfDuration) / halfDuration;
            float easedPulse = Mathf.Sin(pulse * Mathf.PI * 0.5f);

            dangerPulseGroup.alpha = Mathf.Lerp(0.02f, 0.58f, easedPulse);
            if (dangerPulseRect != null)
            {
                float scale = Mathf.Lerp(1f, dangerPulsePeakScale, easedPulse);
                dangerPulseRect.localScale = new Vector3(scale, scale, 1f);
            }

            yield return null;
        }

        dangerPulseGroup.alpha = 0f;
        if (dangerPulseRect != null)
        {
            dangerPulseRect.localScale = Vector3.one;
        }

        dangerPulseRoutine = null;
    }

    private TMP_Text CreateIntroText(string text, Vector2 anchoredPosition, float fontSize, FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(text, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(introPanel.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(520f, 64f);
        textRect.anchoredPosition = anchoredPosition;

        TMP_Text textComponent = textObject.GetComponent<TMP_Text>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.white;
        return textComponent;
    }

    private Button CreateDifficultyButton(string objectName, string label, Vector2 anchoredPosition, out TMP_Text bestScoreText)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(introPanel.transform, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(330f, 82f);
        buttonRect.anchoredPosition = anchoredPosition;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.92f, 0.78f, 0.45f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        TMP_Text labelText = CreateChildText(buttonObject.transform, $"{label}Label", label, new Vector2(0f, -8f), 34f, FontStyles.Bold);
        labelText.color = new Color(0.12f, 0.09f, 0.05f, 1f);

        bestScoreText = CreateChildText(buttonObject.transform, $"{label}BestScore", "BEST 0", new Vector2(0f, 25f), 17f, FontStyles.Bold);
        bestScoreText.color = new Color(0.2f, 0.13f, 0.03f, 1f);
        return button;
    }

    private static TMP_Text CreateChildText(Transform parent, string objectName, string text, Vector2 anchoredPosition, float fontSize, FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(300f, 34f);
        textRect.anchoredPosition = anchoredPosition;

        TMP_Text textComponent = textObject.GetComponent<TMP_Text>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.alignment = TextAlignmentOptions.Center;
        return textComponent;
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
