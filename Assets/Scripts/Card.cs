using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class Card : MonoBehaviour
{
    [SerializeField] private float riseHeight = 1f;
    [SerializeField] private float animDuration = 0.1f;
    [SerializeField] private float dragScale = 0.8f;
    [SerializeField] private float scaleDuration = 0.15f;

    public System.Action OnDragStarted;
    public HandManager handManager;

    private bool _isSelected = false;
    private bool _isInHand = false;
    private ICardDropArea _originArea;
    private Coroutine _anim;
    private Collider2D _collider;
    private SpriteRenderer _sr;
    private Vector3 _originalPosition;
    private int _savedSortingOrder;

    private const int DragSortingOrder = 9999;


    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
    }

    public void Click()
    {
        if (!_isInHand) return;
        _originalPosition = _isSelected ? transform.position - new Vector3(0, riseHeight, 0) : transform.position;
        _isSelected = !_isSelected;

        Vector3 target = _isSelected
            ? _originalPosition + new Vector3(0, riseHeight, 0)
            : _originalPosition;

        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(AnimatePosition(target));
    }

    public void LongPress()
    {
        _isInHand = false;
        _savedSortingOrder = _sr.sortingOrder;
        _sr.sortingOrder = DragSortingOrder;
        transform.DOKill();
        transform.DOScale(dragScale, scaleDuration).SetEase(Ease.OutQuad);
        OnDragStarted?.Invoke();
        transform.SetPositionAndRotation(GetMouseWorldPosition(), Quaternion.identity);
    }

    public void Release()
    {
        _collider.enabled = false;
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);
        _collider.enabled = true;

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out ICardDropArea dropArea))
            {
                Debug.Log("Dropping card on drop area");
                dropArea.OnCardDrop(this);
                return;
            }

            if (hit.TryGetComponent(out HandManager hand))
            {
                Debug.Log("Dropping card on hand manager");
                hand.OnCardDrop(this);
                return;
            }
        }

        if (_originArea != null)
            _originArea.OnCardDrop(this);
        else if (handManager != null)
            handManager.OnCardDrop(this);
    }

    public void SetOrigin(ICardDropArea origin)
    {
        _originArea = origin;
    }

    public void SetSortingOrder(int order)
    {
        _sr.sortingOrder = order;
    }

    public void SetInHand(bool inHand)
    {
        _isInHand = inHand;
    }

    public void ResetSelection()
    {
        _isSelected = false;
    }

    private IEnumerator AnimatePosition(Vector3 target)
    {
        Vector3 start = transform.position;
        float elapsed = 0f;
        float originalZ = transform.position.z; // preserve Z

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            t = t * t * (3f - 2f * t);
            Vector3 pos = Vector3.Lerp(start, target, t);
            pos.z = originalZ; // keep Z untouched
            transform.position = pos;
            yield return null;
        }

        target.z = originalZ;
        transform.position = target;
        _anim = null;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePos.z = transform.position.z; // keep Z unchanged
        return mousePos;
    }
}

internal class CardDropArea
{
}