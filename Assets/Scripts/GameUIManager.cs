using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HandView handView;
    [SerializeField] public PlayerPlayView playerArea;
    [SerializeField] public PlayerPlayView opponentArea;
    [SerializeField] private TurnUpdateView turnBar;
    [SerializeField] private CostDisplayView costDisplay;
    [SerializeField] private EndTurnView endTurnBtn;
    [SerializeField] private EndGameView endGameView;

    [Header("UX State Overlays")]
    [SerializeField] private GameObject waitingOverlay;
    [SerializeField] private GameObject inputBlocker;
    [SerializeField] private GameObject playerWaitingOverlay;
    [SerializeField] private AbilityOverlay localOverlay;
    [SerializeField] private AbilityOverlay opponentOverlay;

    private IGameController game;
    private bool interactableHand = true;
    private bool endTurnEnabled = false;

    void Start()
    {
        endTurnBtn.OnEndTurnClicked = OnEndTurn;
        handView.OnCardClicked = OnHandCardClicked;

        SetHandInteractable(true);
        SetEndTurnButtonActive(false);
        ShowWaiting(false);
    }

    public void Bind(IGameController gameController)
    {
        this.game = gameController;
    }

    void OnHandCardClicked(int cardId)
    {
        if (!interactableHand) return;
        game.TrySelectCard(cardId);
    }

    void OnEndTurn()
    {
        if (!endTurnEnabled) return;
        game.EndTurn();
    }

    public void UpdateHandUI(IEnumerable<Card> hand)
    {
        handView.RenderHand(hand);
    }

    public void UpdateCostUI(int used, int max)
    {
        costDisplay.SetCost(used, max);
    }

    public void UpdateTurn(int turn, int total)
    {
        turnBar.SetTurn(turn, total);
    }

    public void UpdateTimer(float timeLeft)
    {
        turnBar.SetTimer(timeLeft);
    }

    public void UpdateLocalPlayed(IEnumerable<Card> cards)
    {
        playerArea.RenderPlayedCards(cards);
    }

    public void UpdateOpponentPlayed(IEnumerable<Card> cards)
    {
        opponentArea.RenderPlayedCards(cards);
    }

    public void SetHandInteractable(bool canInteract)
    {
        interactableHand = canInteract;

        handView.SetInteractable(canInteract);

        if (inputBlocker != null)
            inputBlocker.SetActive(!canInteract);
    }

    public void SetEndTurnButtonActive(bool active)
    {
        endTurnEnabled = active;
        endTurnBtn.SetInteractable(active);
    }

    public void ShowWaiting(bool show)
    {
        if (waitingOverlay != null)
            waitingOverlay.SetActive(show);
    }

    public void CloseWaitingForPlayersPanel()
    {
        playerWaitingOverlay.SetActive(false);
    }

    public void ShowEndScreen(int myScore, int opponentScore)
    {
        string status = "";

        if (myScore == opponentScore)
        {
            status = "Draw";
        }
        else
        {
            status = myScore > opponentScore ? "You Win" : "You Lose";
        }
        
        endGameView.SetGameEnd(myScore.ToString(), opponentScore.ToString(), status);
    }

    public IEnumerator PlayAbilitySequence(List<AbilityEvent> events, string localId)
    {
        var localEvents = events.Where(e => e.playerId == localId).ToList();
        var opponentEvents = events.Where(e => e.playerId != localId).ToList();

        if(localEvents.Count > 0)
        {
            localOverlay.gameObject.SetActive(true);
            // 1. Play local abilities
            foreach (var evt in localEvents)
                localOverlay.Enqueue(evt.description);
        }

        yield return new WaitUntil(() => !localOverlay.isActiveAndEnabled);

        // Optional delay
        if (localEvents.Count > 0 && opponentEvents.Count > 0)
            yield return new WaitForSeconds(0.25f);

        if(opponentEvents.Count > 0)
        {
            opponentOverlay.gameObject.SetActive(true);

            // 2. Play opponent abilities
            foreach (var evt in opponentEvents)
                opponentOverlay.Enqueue(evt.description);
        }

        yield return new WaitUntil(() => !opponentOverlay.isActiveAndEnabled);
    }
}