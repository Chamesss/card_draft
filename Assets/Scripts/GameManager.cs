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

    private void Update()
    {
        // Press SPACE → all four players draw one card simultaneously
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            playerHand.DrawCard();
            centerRightHand.DrawCard();
            centerLeftHand.DrawCard();
            topCenterHand.DrawCard();
        }
    }
}