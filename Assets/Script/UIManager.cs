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
    [SerializeField] private Button restartButton = null!;
    [SerializeField] private Button mainMenuButton = null!;

    [Header("Status Colors")]
    [SerializeField] private Color neutralColor = new(0.95f, 0.95f, 0.95f, 1f);
    [SerializeField] private Color goodColor = new(0.58f, 0.91f, 0.63f, 1f);
    [SerializeField] private Color badColor = new(1f, 0.57f, 0.57f, 1f);
    [SerializeField] private Color warningColor = new(1f, 0.86f, 0.46f, 1f);

    private readonly List<CardView> cardViews = new();
    private Action restartRequested;
    private Action mainMenuRequested;
    private Action<GameDifficulty> difficultySelected;
    private int activeCardCount;
    private bool hasCachedBaseGridPadding;
    private int baseGridPaddingTop;
    private int baseGridPaddingBottom;

    public int ActiveCardCount => activeCardCount;

    private void Awake()
    {
        CreateDefaultIntroPanelIfNeeded();
        CreateDefaultGameOverButtonsIfNeeded();

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
    }

    public void HideIntro()
    {
        if (introPanel != null)
        {
            introPanel.SetActive(false);
        }
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
        curseStatusText.text = "저주: 뽑으면 2칸 뒤로";
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
