using System.Collections.Generic;
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

    private IGameController game;

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


    private bool interactableHand = true;
    private bool endTurnEnabled = false;

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
}
