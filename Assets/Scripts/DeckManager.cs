using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    [SerializeField] private Card cardPrefab;

    private List<Card> deck = new List<Card>();

    void Start()
    {
        GenerateDeck();
    }

    void GenerateDeck()
    {
        foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
        {
            for (int rank = 1; rank <= 13; rank++)
            {
                Card card = Instantiate(cardPrefab, transform);
                card.Setup(suit, rank);
                card.Flip(true);

                deck.Add(card);
            }
        }
    }
}