using UnityEngine;
using UnityEngine.InputSystem;

public class CardInputHandler : MonoBehaviour
{
    [SerializeField] private float longPressDuration = 0.2f;

    private Camera _cam;
    private float _pressTime;
    private Card _pressedCard;
    private bool _isDragging;

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
                _isDragging = false;
            }
        }

        if (Mouse.current.leftButton.isPressed && _pressedCard != null)
        {
            float duration = Time.time - _pressTime;
            if (!_isDragging && duration >= longPressDuration)
            {
                _isDragging = true;
                _pressedCard.LongPress();
            }

            if (_isDragging)
            {
                // Move the card to the mouse every frame while dragging
                Vector3 mousePos = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                mousePos.z = _pressedCard.transform.position.z;
                _pressedCard.transform.SetPositionAndRotation(mousePos, Quaternion.identity);
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && _pressedCard != null)
        {
            float duration = Time.time - _pressTime;
            if (duration >= longPressDuration)
                _pressedCard.Release();
            else
                _pressedCard.Click();
            _pressedCard = null;
            _isDragging = false;
        }
    }
}