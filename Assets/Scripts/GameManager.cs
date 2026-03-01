using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Central game loop controller.
/// Owns ALL keyboard input so that no individual hand manager
/// can accidentally draw cards on its own.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Player Hand")]
    [SerializeField] private HandManager playerHand;

    [Header("Opponent Hands")]
    [SerializeField] private OpponentHandManager centerRightHand;
    [SerializeField] private OpponentHandManager centerLeftHand;
    [SerializeField] private OpponentHandManager topCenterHand;

    public List<Card> deck = new();

    private void Awake()
    {
        InitCards();
    }

    private void Update()
    {
        // Press SPACE → all four players draw one card simultaneously
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            GiveRandomCard(playerHand);
            GiveRandomCard(centerRightHand);
            GiveRandomCard(centerLeftHand);
            GiveRandomCard(topCenterHand);
        }
    }

    private void InitCards()
    {
        deck.Clear();

        foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
        {
            for (int rank = 1; rank <= 13; rank++)
            {
                Card card = new();
                card.Setup(suit, rank);
                deck.Add(card);
            }
        }

        // Shuffle the deck (Fisher-Yates)
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }

        Debug.Log("Deck created with " + deck.Count + " cards.");
    }

    private void GiveRandomCard(HandManagerBase hand)
    {
        if (deck.Count == 0)
        {
            Debug.Log("Deck is empty.");
            return;
        }

        Card drawnCard = deck[0];
        deck.RemoveAt(0);
        hand.DrawCard(drawnCard);
    }

}