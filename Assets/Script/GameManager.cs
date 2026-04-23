using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloorGenerator floorGenerator = null!;
    [SerializeField] private UIManager uiManager = null!;

    [Header("Difficulty")]
    [SerializeField] private GameDifficulty currentDifficulty = GameDifficulty.Normal;
    [SerializeField] private GameDifficultyProfile easyProfile = GameDifficultyProfile.CreateEasy();
    [SerializeField] private GameDifficultyProfile normalProfile = GameDifficultyProfile.CreateNormal();
    [SerializeField] private GameDifficultyProfile hardProfile = GameDifficultyProfile.CreateHard();

    [Header("Common Fast Timing")]
    [SerializeField, Min(0f)] private float commonFloorPreviewFlipDuration = 0.14f;
    [SerializeField, Min(0f)] private float commonRowAdvanceScrollDuration = 0.22f;

    private readonly PlayerState playerState = new();
    private List<CardData> currentFloorCards = new(3);
    private bool isResolvingCard;
    private bool isGameOver;
    private int currentTowerHeight;
    private int bestTowerHeight;
    private int currentBoardRowCount;
    private int activeRowContentIndex;

    private void Start()
    {
        if (!HasValidReferences())
        {
            enabled = false;
            return;
        }

        uiManager.Initialize(RestartSession, StartSessionWithDifficulty, ReturnToMainMenu);
        RefreshIntroBestScores();

        if (uiManager.HasIntroPanel)
        {
            uiManager.ShowIntro();
        }
        else
        {
            RestartSession();
        }
    }

    public void StartSessionWithDifficulty(GameDifficulty difficulty)
    {
        currentDifficulty = difficulty;
        RestartSession();
    }

    public void RestartSession()
    {
        StopAllCoroutines();
        isResolvingCard = false;
        isGameOver = false;

        playerState.ResetSession();
        currentTowerHeight = 1;
        bestTowerHeight = Mathf.Max(1, GetSavedBestScore(currentDifficulty));

        uiManager.HideIntro();
        uiManager.HideGameOver();
        LoadFloor(currentTowerHeight, "1층부터 시작합니다. 위에서 아래까지 보드를 확인한 뒤 가장 아래 행의 카드 하나를 고르세요.");
    }

    public void ReturnToMainMenu()
    {
        StopAllCoroutines();
        isResolvingCard = false;
        isGameOver = false;
        RefreshIntroBestScores();
        uiManager.ShowIntro();
        uiManager.SetStatusMessage("난이도를 선택하세요.", StatusTone.Neutral);
    }

    private void HandleCardSelected(int cardIndex)
    {
        if (isGameOver || isResolvingCard)
        {
            return;
        }

        if (cardIndex < 0 || cardIndex >= currentFloorCards.Count)
        {
            return;
        }

        if (!IsIndexInActiveRow(cardIndex))
        {
            return;
        }

        CardData selectedCard = currentFloorCards[cardIndex];
        if (selectedCard.IsConsumed || selectedCard.IsPlaceholder)
        {
            return;
        }

        StartCoroutine(ResolveCardRoutine(cardIndex));
    }

    private IEnumerator ResolveCardRoutine(int cardIndex)
    {
        isResolvingCard = true;
        uiManager.SetAllCardInteraction(currentFloorCards, false);
        uiManager.SetBoardNavigationEnabled(false);

        CardData selectedCard = currentFloorCards[cardIndex];
        CardView selectedView = uiManager.GetCardView(cardIndex);

        bool hideResultMessage = false;

        GameDifficultyProfile difficultyProfile = GetCurrentDifficultyProfile();

        yield return selectedView.PlayRevealAnimation(difficultyProfile.revealAnimationDuration);

        if (difficultyProfile.postRevealDelay > 0f)
        {
            yield return new WaitForSeconds(difficultyProfile.postRevealDelay);
        }

        switch (selectedCard.Type)
        {
            case CardType.Placeholder:
                isResolvingCard = false;
                yield break;
            case CardType.Stair:
                yield return ResolveStair(selectedCard, selectedView, hideResultMessage);
                yield break;
            case CardType.Chest:
                yield return ResolveChest(selectedCard, selectedView, hideResultMessage);
                yield break;
            case CardType.Monster:
                yield return ResolveMonster(selectedCard, selectedView, hideResultMessage);
                yield break;
            case CardType.GoodItem:
                yield return ResolveGoodItem(selectedCard, selectedView, hideResultMessage);
                yield break;
            case CardType.BadItem:
                yield return ResolveBadItem(selectedCard, selectedView, hideResultMessage);
                yield break;
            case CardType.Empty:
                yield return ResolveEmpty(selectedCard, selectedView, hideResultMessage);
                yield break;
        }
    }

    private IEnumerator ResolveStair(CardData selectedCard, CardView selectedView, bool hideResultMessage)
    {
        selectedCard.Consume();
        selectedView.SetConsumedVisual();

        uiManager.SetStatusMessage(
            hideResultMessage
                ? "저주 때문에 결과 문구는 흐렸지만, 다음 층으로 향하는 안전한 길을 찾았습니다."
                : "계단 카드였습니다. 다음 층으로 올라갑니다.",
            StatusTone.Good);

        yield return AdvanceAfterRowSelection();
    }

    private IEnumerator ResolveChest(CardData selectedCard, CardView selectedView, bool hideResultMessage)
    {
        selectedCard.Consume();
        selectedView.SetConsumedVisual();

        uiManager.SetStatusMessage(
            hideResultMessage
                ? "저주 때문에 결과 문구는 흐렸지만, 보물 상자를 열어 스테이지를 통과합니다."
                : "보물 상자를 열었습니다. 스테이지를 통과합니다.",
            StatusTone.Good);

        yield return AdvanceAfterRowSelection();
    }

    private IEnumerator ResolveMonster(CardData selectedCard, CardView selectedView, bool hideResultMessage)
    {
        selectedCard.Consume();
        selectedView.SetConsumedVisual();
        uiManager.PlayDangerPulse();

        if (playerState.ConsumeShield())
        {
            uiManager.SetStatusMessage(
                hideResultMessage
                    ? "저주 때문에 결과 문구는 흐렸지만, 방패가 깨지며 몬스터를 막아냈습니다."
                    : "몬스터 카드였지만 방패가 깨지며 살아남았습니다.",
                StatusTone.Warning);

            uiManager.RefreshHud(currentTowerHeight, bestTowerHeight, GetActiveFloorNumber(), playerState);
            if (IsActiveRowTopRow())
            {
                ContinueSelectingCurrentRow("방패로 몬스터를 막았습니다. 보물 상자를 찾아야 스테이지를 통과합니다.", StatusTone.Warning);
                yield break;
            }

            yield return AdvanceAfterRowSelection();
            yield break;
        }

        isGameOver = true;
        isResolvingCard = false;
        uiManager.RefreshHud(currentTowerHeight, bestTowerHeight, GetActiveFloorNumber(), playerState);
        uiManager.SetStatusMessage(
            hideResultMessage
                ? "저주 때문에 결과 문구를 읽지 못한 채 몬스터에게 쓰러졌습니다."
                : "몬스터 카드였습니다. 게임 오버입니다.",
            StatusTone.Bad);
        bestTowerHeight = Mathf.Max(bestTowerHeight, currentTowerHeight);
        SaveBestScoreIfNeeded(currentDifficulty, bestTowerHeight);
        RefreshIntroBestScores();
        uiManager.SetAllCardInteraction(currentFloorCards, false);
        uiManager.ShowGameOver(currentTowerHeight, bestTowerHeight);
    }

    private IEnumerator ResolveGoodItem(CardData selectedCard, CardView selectedView, bool hideResultMessage)
    {
        selectedCard.Consume();
        selectedView.SetConsumedVisual();

        string message = selectedCard.GoodItemType switch
        {
            GoodItemType.Shield when playerState.TryGainShield() => hideResultMessage
                ? "저주 때문에 결과 문구는 흐렸지만, 방패를 얻었습니다. 같은 층에서 다시 카드를 선택하세요."
                : "방패를 획득했습니다. 같은 층에서 다시 카드를 선택하세요.",
            GoodItemType.Shield => hideResultMessage
                ? "저주 때문에 결과 문구는 흐렸고, 이미 방패를 보유 중이라 추가 획득은 없었습니다. 같은 층에서 다시 카드를 선택하세요."
                : "이미 방패를 가지고 있어서 추가로 쌓이지 않았습니다. 같은 층에서 다시 카드를 선택하세요.",
            _ => "알 수 없는 아이템입니다. 같은 층에서 다시 카드를 선택하세요."
        };

        ContinueSelectingCurrentRow(message, StatusTone.Good);
        yield break;
    }

    private IEnumerator ResolveBadItem(CardData selectedCard, CardView selectedView, bool hideResultMessage)
    {
        selectedCard.Consume();
        selectedView.SetConsumedVisual();
        uiManager.PlayDangerPulse();

        yield return RewindAfterCurse();
    }

    private IEnumerator ResolveEmpty(CardData selectedCard, CardView selectedView, bool hideResultMessage)
    {
        selectedCard.Consume();
        selectedView.SetConsumedVisual();

        ContinueSelectingCurrentRow(
            hideResultMessage
                ? "저주 때문에 결과 문구는 흐렸지만, 빈 카드였습니다. 같은 층에서 다시 카드를 선택하세요."
                : "빈 카드였습니다. 같은 층에서 다시 카드를 선택하세요.",
            StatusTone.Neutral);
        yield break;
    }

    private IEnumerator AdvanceAfterRowSelection()
    {
        if (activeRowContentIndex > 0)
        {
            int currentRowContentIndex = activeRowContentIndex;
            activeRowContentIndex -= 1;
            int nextFloorNumber = GetActiveFloorNumber();

            yield return uiManager.PlayScrollToRow(
                currentRowContentIndex,
                activeRowContentIndex,
                currentBoardRowCount,
                commonRowAdvanceScrollDuration);

            isResolvingCard = false;
            uiManager.RefreshHud(currentTowerHeight, bestTowerHeight, nextFloorNumber, playerState);
            uiManager.SetCardInteractionForRow(currentFloorCards, activeRowContentIndex, true);
            uiManager.SetStatusMessage($"{nextFloorNumber}층으로 이동했습니다. 이 행의 카드 하나를 선택하세요.", StatusTone.Neutral);
            yield break;
        }

        float floorTransitionDelay = GetCurrentDifficultyProfile().floorTransitionDelay;
        if (floorTransitionDelay > 0f)
        {
            yield return new WaitForSeconds(floorTransitionDelay);
        }

        currentTowerHeight += 1;
        bestTowerHeight = Mathf.Max(bestTowerHeight, currentTowerHeight);
        SaveBestScoreIfNeeded(currentDifficulty, bestTowerHeight);
        RefreshIntroBestScores();
        LoadFloor(currentTowerHeight, $"{currentTowerHeight}층 타워가 생성되었습니다. 위에서 아래까지 확인한 뒤 가장 아래 행부터 시작하세요.");
    }

    private IEnumerator RewindAfterCurse()
    {
        int fromRowContentIndex = activeRowContentIndex;
        int targetRowContentIndex = Mathf.Min(currentBoardRowCount - 1, activeRowContentIndex + 2);

        ResetRowsForRetry(fromRowContentIndex, targetRowContentIndex);
        activeRowContentIndex = targetRowContentIndex;

        if (fromRowContentIndex != targetRowContentIndex)
        {
            yield return uiManager.PlayScrollToRow(
                fromRowContentIndex,
                targetRowContentIndex,
                currentBoardRowCount,
                commonRowAdvanceScrollDuration);
        }
        else
        {
            uiManager.JumpToRow(activeRowContentIndex, currentBoardRowCount);
        }

        isResolvingCard = false;
        uiManager.RefreshHud(currentTowerHeight, bestTowerHeight, GetActiveFloorNumber(), playerState);
        uiManager.SetCardInteractionForRow(currentFloorCards, activeRowContentIndex, true);
        uiManager.SetStatusMessage("저주에 걸렸습니다. 2칸 전으로 돌아가고 카드가 다시 뒤집혔습니다. 이 행의 카드 하나를 선택하세요.", StatusTone.Warning);
    }

    private void ResetRowsForRetry(int fromRowContentIndex, int targetRowContentIndex)
    {
        int firstRow = Mathf.Min(fromRowContentIndex, targetRowContentIndex);
        int lastRow = Mathf.Max(fromRowContentIndex, targetRowContentIndex);

        for (int rowIndex = firstRow; rowIndex <= lastRow; rowIndex++)
        {
            int rowStartIndex = rowIndex * 3;
            for (int offset = 0; offset < 3; offset++)
            {
                int cardIndex = rowStartIndex + offset;
                if (cardIndex >= currentFloorCards.Count)
                {
                    break;
                }

                currentFloorCards[cardIndex].ResetConsumed();
                uiManager.GetCardView(cardIndex).ShowFaceDownImmediate();
            }
        }
    }

    private void ContinueSelectingCurrentRow(string message, StatusTone tone)
    {
        isResolvingCard = false;
        uiManager.RefreshHud(currentTowerHeight, bestTowerHeight, GetActiveFloorNumber(), playerState);
        uiManager.SetCardInteractionForRow(currentFloorCards, activeRowContentIndex, true);
        uiManager.SetStatusMessage(message, tone);
    }

    private void LoadFloor(int towerHeight, string entryMessage)
    {
        currentTowerHeight = towerHeight;
        bestTowerHeight = Mathf.Max(bestTowerHeight, currentTowerHeight);
        currentBoardRowCount = floorGenerator.GetTotalRowCountForFloor(currentTowerHeight);
        currentFloorCards = floorGenerator.GenerateFloor(currentTowerHeight, GetCurrentDifficultyProfile());
        activeRowContentIndex = currentBoardRowCount - 1;

        uiManager.BindFloorCards(currentFloorCards, HandleCardSelected);
        uiManager.RefreshHud(currentTowerHeight, bestTowerHeight, 1, playerState);
        uiManager.SetStatusMessage(entryMessage, StatusTone.Neutral);
        uiManager.SetAllCardInteraction(currentFloorCards, false);
        uiManager.SetBoardNavigationEnabled(false);
        isResolvingCard = true;

        StartCoroutine(BeginFloorPreviewRoutine());
    }

    private IEnumerator BeginFloorPreviewRoutine()
    {
        for (int i = 0; i < uiManager.ActiveCardCount; i++)
        {
            uiManager.GetCardView(i).ShowRevealedImmediate();
        }

        GameDifficultyProfile difficultyProfile = GetCurrentDifficultyProfile();

        yield return uiManager.PlayBoardPreviewPan(currentBoardRowCount, difficultyProfile.boardPanDurationPerRow);

        if (difficultyProfile.floorPreviewDuration > 0f)
        {
            yield return new WaitForSeconds(difficultyProfile.floorPreviewDuration);
        }

        if (commonFloorPreviewFlipDuration > 0f)
        {
            for (int i = 0; i < uiManager.ActiveCardCount; i++)
            {
                StartCoroutine(uiManager.GetCardView(i).PlayFlipToFaceDownAnimation(commonFloorPreviewFlipDuration));
            }

            yield return new WaitForSeconds(commonFloorPreviewFlipDuration);
        }
        else
        {
            for (int i = 0; i < uiManager.ActiveCardCount; i++)
            {
                uiManager.GetCardView(i).ShowFaceDownImmediate();
            }
        }

        uiManager.JumpToRow(activeRowContentIndex, currentBoardRowCount);
        isResolvingCard = false;
        uiManager.SetCardInteractionForRow(currentFloorCards, activeRowContentIndex, true);
        uiManager.SetStatusMessage("가장 아래 행부터 시작합니다. 이 행의 카드 하나를 선택하세요.", StatusTone.Neutral);
        uiManager.RefreshHud(currentTowerHeight, bestTowerHeight, GetActiveFloorNumber(), playerState);
    }

    private int GetActiveFloorNumber()
    {
        return activeRowContentIndex == 0
            ? currentTowerHeight
            : currentBoardRowCount - activeRowContentIndex;
    }

    private bool IsActiveRowTopRow()
    {
        return activeRowContentIndex == 1;
    }

    private GameDifficultyProfile GetCurrentDifficultyProfile()
    {
        return currentDifficulty switch
        {
            GameDifficulty.Easy => easyProfile ?? GameDifficultyProfile.CreateEasy(),
            GameDifficulty.Hard => hardProfile ?? GameDifficultyProfile.CreateHard(),
            _ => normalProfile ?? GameDifficultyProfile.CreateNormal()
        };
    }

    private void RefreshIntroBestScores()
    {
        uiManager.SetIntroBestScores(
            GetSavedBestScore(GameDifficulty.Easy),
            GetSavedBestScore(GameDifficulty.Normal),
            GetSavedBestScore(GameDifficulty.Hard));
    }

    private static int GetSavedBestScore(GameDifficulty difficulty)
    {
        return PlayerPrefs.GetInt(GetBestScoreKey(difficulty), 0);
    }

    private static void SaveBestScoreIfNeeded(GameDifficulty difficulty, int score)
    {
        string key = GetBestScoreKey(difficulty);
        if (score <= PlayerPrefs.GetInt(key, 0))
        {
            return;
        }

        PlayerPrefs.SetInt(key, score);
        PlayerPrefs.Save();
    }

    private static string GetBestScoreKey(GameDifficulty difficulty)
    {
        return $"PickPickTower.BestScore.{difficulty}";
    }

    private bool IsIndexInActiveRow(int cardIndex)
    {
        int rowStartIndex = activeRowContentIndex * 3;
        return cardIndex >= rowStartIndex && cardIndex < rowStartIndex + 3;
    }

    private bool HasValidReferences()
    {
        bool hasAllReferences = floorGenerator != null && uiManager != null && uiManager.HasValidReferences();
        if (!hasAllReferences)
        {
            Debug.LogError("GameManager references are missing. Assign FloorGenerator and UIManager in the Inspector.");
        }

        return hasAllReferences;
    }
}
