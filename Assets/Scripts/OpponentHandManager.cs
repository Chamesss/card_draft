using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Identifies which opponent seat this hand belongs to.
/// The seat determines the default spawn rotation if none is set in the Inspector.
/// </summary>
public enum PlayerSeat
{
    CenterRight,   // right side  — cards face player's right, back faces camera
    CenterLeft,    // left side   — cards face player's left,  back faces camera
    TopCenter      // top/opposite — cards face away,           back faces camera
}

/// <summary>
/// Manages an opponent's hand. Re-uses the same Card prefab as HandManager.
/// Cards are spawned face-down (back toward camera) because the spawnPoint
/// transform is pre-rotated 180° on Y (or as appropriate for the seat).
/// Draw and drop animations take the card's world-space orientation into account.
/// </summary>
public class OpponentHandManager : MonoBehaviour
{
    [Header("Seat")]
    [Tooltip("Which opponent seat this manager represents.")]
    [SerializeField] private PlayerSeat seat;

    [Header("References")]
    [SerializeField] private int maxHandSize = 7;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private SplineContainer splineContainer;

    [Tooltip("Where cards spawn from (position + rotation). " +
             "Rotate Y 180° for TopCenter, Y -90° for CenterLeft, Y 90° for CenterRight " +
             "so the back of the card faces the camera at spawn.")]
    [SerializeField] private Transform spawnPoint;

    [Header("Animation")]
    [SerializeField] private float drawDuration = 0.35f;
    [SerializeField] private float dropDuration = 0.25f;
    [Tooltip("Final resting scale for opponent cards. Use a value < 1 to make them smaller than the player's cards.")]
    [SerializeField] private float cardScale = 0.5f;
    [SerializeField] private float cardZOffset = 0f;

    private readonly List<GameObject> _handCards = new();

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Draw one card into this opponent's hand.</summary>
    public void DrawCard()
    {
        if (_handCards.Count >= maxHandSize) return;

        // Spawn at spawnPoint — the rotation here IS the "flipped" orientation.
        // Because spawnPoint.rotation has Y rotated 180° (or seat-appropriate angle),
        // Card.Awake()'s dot-product check will evaluate the card as face-down.
        GameObject g = Instantiate(cardPrefab, spawnPoint.position, spawnPoint.rotation);
        _handCards.Add(g);

        Card card = g.GetComponent<Card>();

        // Assign random data (or pass in specific data for a real game)
        Suit randomSuit = (Suit)Random.Range(0, System.Enum.GetValues(typeof(Suit)).Length);
        card.Setup(randomSuit, Random.Range(1, 14));

        // Disable interactivity — opponent cards can't be clicked or dragged by the local player
        card.SetInteractable(false);

        RefreshHandLayout(animateLast: true);
    }

    /// <summary>Remove a card from this opponent's hand (e.g. they played it).</summary>
    public void DropCard(GameObject card)
    {
        if (!_handCards.Contains(card)) return;
        _handCards.Remove(card);
        RefreshHandLayout(animateLast: false);
    }

    // ── Private ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Repositions all cards along the spline.
    /// <paramref name="animateLast"/> — when true, the last card tweens FROM the spawn point
    /// (draw animation). All others just settle into new positions.
    /// </summary>
    private void RefreshHandLayout(bool animateLast)
    {
        if (_handCards.Count == 0) return;

        float cardSpacing = 1f / maxHandSize;
        float firstCardPos = 0.5f - (_handCards.Count - 1) * cardSpacing / 2f;
        Spline spline = splineContainer.Spline;

        for (int i = 0; i < _handCards.Count; i++)
        {
            float t = firstCardPos + i * cardSpacing;
            Vector3 splinePos = spline.EvaluatePosition(t);
            splinePos.z = -i * cardZOffset;

            // Build the rotation from spline tangent/up — same as HandManager.
            // The spline itself should be oriented for this seat (rotated in the scene).
            Vector3 localPos = spline.EvaluatePosition(t);
            Vector3 worldPos = splineContainer.transform.TransformPoint(localPos);
            worldPos.z = -i * cardZOffset;

            // Evaluate the spline tangent in world space, then clamp to the XY plane.
            // We never use EvaluateUpVector because baked knot rotations (X=270, Y=180)
            // cause it to return a Z-direction vector that pollutes LookRotation.
            Vector3 localTangent = spline.EvaluateTangent(t);
            Vector3 worldTangent = splineContainer.transform.TransformDirection(localTangent);
            worldTangent.z = 0f;
            if (worldTangent.sqrMagnitude < 0.0001f) worldTangent = Vector3.up;
            else worldTangent.Normalize();

            // Build the final rotation in two steps:
            //   1. LookRotation(back, tangent) — pins forward to -Z so the back always faces
            //      the camera (dot=-1 → _isFaceUp=false), and uses the spline tangent as
            //      the card's up-axis to produce the natural fan tilt along the arc.
            //   2. GetSeatBaseRotation() — multiplies a seat-specific Z rotation (in the
            //      card's local space) so each opponent's cards appear to be held at the
            //      correct angle for their position at the table:
            //        CenterRight → 90° CW  (opponent on the right holds cards sideways)
            //        CenterLeft  → 90° CCW (opponent on the left, mirrored)
            //        TopCenter   → 180°    (opponent opposite holds cards upside-down)
            Quaternion faceDownRot = Quaternion.LookRotation(Vector3.back, worldTangent) * GetSeatBaseRotation();

            GameObject go = _handCards[i];
            bool isNewCard = animateLast && (i == _handCards.Count - 1);

            go.transform.DOKill();

            if (isNewCard)
            {
                // ── Draw animation ──────────────────────────────────────────
                // Card starts at spawnPoint (already correct rotation from Instantiate),
                // so we only need to tween position and final rotation.
                go.transform
                  .DOMove(splinePos, drawDuration)
                  .SetEase(Ease.OutCubic);

                go.transform
                  .DORotateQuaternion(faceDownRot, drawDuration)
                  .SetEase(Ease.OutCubic);

                go.transform
                  .DOScale(cardScale, drawDuration)
                  .SetEase(Ease.OutBack);
            }
            else
            {
                // ── Settle / reposition animation ───────────────────────────
                go.transform.DOMove(splinePos, dropDuration).SetEase(Ease.OutCubic);
                go.transform.DORotateQuaternion(faceDownRot, dropDuration).SetEase(Ease.OutCubic);
                go.transform.DOScale(cardScale, dropDuration).SetEase(Ease.OutCubic);
            }

            go.GetComponent<Card>().SetSortingOrder(i);
        }
    }


    /// <summary>
    /// Per-seat Z rotation (applied in card-local space) that orients the card
    /// as though it is being held by the opponent sitting at that seat.
    /// </summary>
    private Quaternion GetSeatBaseRotation() => seat switch
    {
        PlayerSeat.CenterRight => Quaternion.Euler(0f, 0f, -90f),  // rotated 90° CW
        PlayerSeat.CenterLeft => Quaternion.Euler(0f, 0f, 90f),  // rotated 90° CCW
        PlayerSeat.TopCenter => Quaternion.Euler(0f, 0f, 180f),  // upside-down
        _ => Quaternion.identity
    };

#if UNITY_EDITOR
    // Quick test hook — press D in play mode to deal one card per opponent
    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current.dKey.wasPressedThisFrame)
            DrawCard();
    }
#endif
}