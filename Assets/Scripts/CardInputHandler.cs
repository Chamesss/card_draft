using UnityEngine;
using UnityEngine.InputSystem;

public class CardInputHandler : MonoBehaviour
{
    [SerializeField] private float longPressDuration = 0.5f;

    private Camera _cam;
    private float _pressTime;
    private Card _pressedCard;

    void Awake() => _cam = Camera.main;

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                _pressedCard = hit.collider.GetComponent<Card>();
                _pressTime = Time.time;
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && _pressedCard != null)
        {
            float duration = Time.time - _pressTime;

            if (duration >= longPressDuration)
                _pressedCard.LongPress();
            else
                _pressedCard.Click();

            _pressedCard = null;
        }
    }
}