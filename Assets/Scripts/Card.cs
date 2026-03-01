using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public enum Suit
{
    Spades,
    Hearts,
    Diamonds,
    Clubs
}

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SortingGroup))]
public class Card : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private GameObject front;               // card_blank sprite
    [SerializeField] private GameObject back;                // card_blank flipped
    [SerializeField] private SpriteRenderer suitRenderer;   // front > CenterSuit
    [SerializeField] private TextMeshPro rankRenderer;      // front > Rank
    [SerializeField] private SpriteRenderer backBackground; // back > background

    // Cached from children — set automatically in Awake
    private SpriteRenderer _frontRenderer;
    private SpriteRenderer _backRenderer;
    private SortingGroup _sortingGroup;

    [Header("Suit Sprites")]
    [SerializeField] private Sprite spadeSprite;
    [SerializeField] private Sprite heartSprite;
    [SerializeField] private Sprite diamondSprite;
    [SerializeField] private Sprite clubSprite;


    [SerializeField] private float riseHeight = 1f;
    [SerializeField] private float animDuration = 0.1f;
    [SerializeField] private float dragScale = 0.8f;
    [SerializeField] private float scaleDuration = 0.15f;
    [SerializeField] private float flipDuration = 0.3f;

    public System.Action OnDragStarted;
    public HandManager handManager;

    private bool _isSelected = false;
    private bool _isInHand = false;
    private bool _isFaceUp = true;
    private ICardDropArea _originArea;
    private Coroutine _anim;
    private Collider2D _collider;
    private SpriteRenderer _sr;
    private Camera _cam;
    private Vector3 _originalPosition;
    public Suit CardSuit;
    public int CardRank;
    private string _savedSortingLayer;


    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
        _sortingGroup = GetComponent<SortingGroup>();
        _cam = Camera.main;
        if (front != null) _frontRenderer = front.GetComponent<SpriteRenderer>();
        if (back != null) _backRenderer = back.GetComponent<SpriteRenderer>();

        // Fixed relative orders within the card — set once, never touched again.
        // SortingGroup makes these relative to the group, not the global layer.
        _sr.sortingOrder = 0;
        if (_backRenderer != null) _backRenderer.sortingOrder = 1;
        if (_frontRenderer != null) _frontRenderer.sortingOrder = 1;
        if (backBackground != null) backBackground.sortingOrder = 2;
        if (suitRenderer != null) suitRenderer.sortingOrder = 3;
        if (rankRenderer != null) rankRenderer.sortingOrder = 3;

        // Initialize face visibility immediately so prefab defaults don't bleed through
        float dot = Vector3.Dot(transform.forward, _cam.transform.forward);
        _isFaceUp = dot >= 0f;
        SetFaceVisibility(_isFaceUp);
    }

    private void Update()
    {
        // Use a threshold away from 0 to prevent flickering at exactly 90 degrees.
        // SetActive is only called when the state actually changes.
        float dot = Vector3.Dot(transform.forward, _cam.transform.forward);
        if (dot > 0.05f && !_isFaceUp)
        {
            _isFaceUp = true;
            SetFaceVisibility(true);
        }
        else if (dot < -0.05f && _isFaceUp)
        {
            _isFaceUp = false;
            SetFaceVisibility(false);
        }
    }

    public void SetInteractable(bool interactable)
    {
        _collider.enabled = interactable;
    }

    public void Setup(Suit suit, int rank)
    {
        CardSuit = suit;
        CardRank = rank;
        SetSuit(suit);
        SetRank(rank);
        // Update() will auto-set visibility based on rotation
    }

    void SetSuit(Suit suit)
    {
        if (suitRenderer == null)
        {
            Debug.LogWarning("suitRenderer is not assigned on the Card prefab!", this);
            return;
        }

        switch (suit)
        {
            case Suit.Spades:
                GetScaledSuitSprite(spadeSprite);
                break;
            case Suit.Hearts:
                GetScaledSuitSprite(heartSprite);
                break;
            case Suit.Diamonds:
                GetScaledSuitSprite(diamondSprite);
                break;
            case Suit.Clubs:
                GetScaledSuitSprite(clubSprite);
                break;
        }
    }

    void SetRank(int rank)
    {
        if (rankRenderer == null)
        {
            Debug.LogWarning("rankRenderer is not assigned on the Card prefab!", this);
            return;
        }

        string rankString = rank switch
        {
            1 => "A",
            11 => "J",
            12 => "Q",
            13 => "K",
            _ => rank.ToString(),
        };
        rankRenderer.text = rankString;
    }

    void GetScaledSuitSprite(Sprite sprite, float scale = 0.5f)
    {
        if (sprite == null) return;
        suitRenderer.sprite = sprite;
        suitRenderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    // Flip the card by animating a local Y-axis rotation.
    // Update() will detect the facing direction and swap front/back automatically.
    public void Flip(bool showFront)
    {
        if (_isFaceUp == showFront) return;
        _isFaceUp = showFront;
        float targetY = showFront ? 0f : 180f;
        Vector3 current = transform.localEulerAngles;
        transform.DOLocalRotate(new Vector3(current.x, targetY, current.z), flipDuration)
            .SetEase(Ease.InOutQuad);
    }

    private void SetFaceVisibility(bool showFront)
    {
        if (front != null) front.SetActive(showFront);
        if (back != null) back.SetActive(!showFront);
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
        _savedSortingLayer = _sortingGroup.sortingLayerName;
        SetSortingLayer(LayerBase.Drag);
        SetSortingOrder(0);
        transform.DOKill();
        transform.DOScale(dragScale, scaleDuration).SetEase(Ease.OutQuad);
        OnDragStarted?.Invoke();
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

    public void SetSortingLayer(LayerBase layerName)
    {
        _sortingGroup.sortingLayerName = layerName.ToString();
    }

    // Sets the order within the current sorting layer.
    public void SetSortingOrder(int order)
    {
        _sortingGroup.sortingOrder = order;
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
}