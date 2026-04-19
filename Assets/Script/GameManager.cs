using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloorGenerator floorGenerator = null!;
    [SerializeField] private UIManager uiManager = null!;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float boardPanDurationPerRow = 0.4f;
    [SerializeField, Min(0f)] private float floorPreviewDuration = 0.8f;
    [SerializeField, Min(0f)] private float floorPreviewFlipDuration = 0.22f;
    [SerializeField, Min(0f)] private float rowAdvanceScrollDuration = 0.35f;
    [SerializeField, Min(0f)] private float revealAnimationDuration = 0.18f;
    [SerializeField, Min(0f)] private float postRevealDelay = 0.18f;
    [SerializeField, Min(0f)] private float floorTransitionDelay = 0.45f;

    private readonly PlayerState playerState = new();
    private List<CardData> currentFloorCards = new(3);
    private bool isResolvingCard;
    private bool isGameOver;
    private int currentTowerHeight;
    private int bestTowerHeight;
    private int activeRowContentIndex;

    private void Start()
    {
        if (!HasValidReferences())
        {
            enabled = false;
            return;
        }

        uiManager.Initialize(RestartSession);
        RestartSession();
    }

    public void RestartSession()
    {
        StopAllCoroutines();
        isResolvingCard = false;
        isGameOver = false;

        playerState.ResetSession();
        currentTowerHeight = 1;
        bestTowerHeight = 1;

        uiManager.HideGameOver();
        LoadFloor(currentTowerHeight, "1층부터 시작합니다. 위에서 아래까지 보드를 확인한 뒤 가장 아래 행의 카드 하나를 고르세요.");
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
        if (selectedCard.IsConsumed)
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

        // Curse hides only the next result message, then clears itself.
        bool hideResultMessage = playerState.ConsumeCurse();

        yield return selectedView.PlayRevealAnimation(revealAnimationDuration);

        if (postRevealDelay > 0f)
        {
            yield return new WaitForSeconds(postRevealDelay);
        }

        switch (selectedCard.Type)
        {
            case CardType.Stair:
                yield return ResolveStair(selectedCard, selectedView, hideResultMessage);
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

    private IEnumerator ResolveMonster(CardData selectedCard, CardView selectedView, bool hideResultMessage)
    {
        selectedCard.Consume();
        selectedView.SetConsumedVisual();

        if (playerState.ConsumeShield())
        {
            uiManager.SetStatusMessage(
                hideResultMessage
                    ? "저주 때문에 결과 문구는 흐렸지만, 방패가 깨지며 몬스터를 막아냈습니다."
                    : "몬스터 카드였지만 방패가 깨지며 살아남았습니다.",
                StatusTone.Warning);

            uiManager.RefreshHud(currentTowerHeight, bestTowerHeight, GetActiveFloorNumber(), playerState);
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
        playerState.ApplyCurse();

        uiManager.RefreshHud(currentTowerHeight, bestTowerHeight, GetActiveFloorNumber(), playerState);
        uiManager.SetStatusMessage(
            hideResultMessage
                ? "이전 저주 때문에 결과 문구는 흐렸고, 새로운 저주를 받은 채 다음 층으로 이동합니다."
                : "저주에 걸렸습니다. 다음 층으로 이동합니다.",
            StatusTone.Warning);
        yield return AdvanceAfterRowSelection();
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
                currentTowerHeight,
                rowAdvanceScrollDuration);

            isResolvingCard = false;
            uiManager.RefreshHud(currentTowerHeight, bestTowerHeight, nextFloorNumber, playerState);
            uiManager.SetCardInteractionForRow(currentFloorCards, activeRowContentIndex, true);
            uiManager.SetStatusMessage($"{nextFloorNumber}층으로 이동했습니다. 이 행의 카드 하나를 선택하세요.", StatusTone.Neutral);
            yield break;
        }

        if (floorTransitionDelay > 0f)
        {
            yield return new WaitForSeconds(floorTransitionDelay);
        }

        currentTowerHeight += 1;
        bestTowerHeight = Mathf.Max(bestTowerHeight, currentTowerHeight);
        LoadFloor(currentTowerHeight, $"{currentTowerHeight}층 타워가 생성되었습니다. 위에서 아래까지 확인한 뒤 가장 아래 행부터 시작하세요.");
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
        currentFloorCards = floorGenerator.GenerateFloor(currentTowerHeight);
        activeRowContentIndex = currentTowerHeight - 1;

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

        yield return uiManager.PlayBoardPreviewPan(currentTowerHeight, boardPanDurationPerRow);

        if (floorPreviewDuration > 0f)
        {
            yield return new WaitForSeconds(floorPreviewDuration);
        }

        if (floorPreviewFlipDuration > 0f)
        {
            for (int i = 0; i < uiManager.ActiveCardCount; i++)
            {
                StartCoroutine(uiManager.GetCardView(i).PlayFlipToFaceDownAnimation(floorPreviewFlipDuration));
            }

            yield return new WaitForSeconds(floorPreviewFlipDuration);
        }
        else
        {
            for (int i = 0; i < uiManager.ActiveCardCount; i++)
            {
                uiManager.GetCardView(i).ShowFaceDownImmediate();
            }
        }

        uiManager.JumpToRow(activeRowContentIndex, currentTowerHeight);
        isResolvingCard = false;
        uiManager.SetCardInteractionForRow(currentFloorCards, activeRowContentIndex, true);
        uiManager.SetStatusMessage("가장 아래 행부터 시작합니다. 이 행의 카드 하나를 선택하세요.", StatusTone.Neutral);
        uiManager.RefreshHud(currentTowerHeight, bestTowerHeight, GetActiveFloorNumber(), playerState);
    }

    private int GetActiveFloorNumber()
    {
        return currentTowerHeight - activeRowContentIndex;
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
