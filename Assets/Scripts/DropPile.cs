using DG.Tweening;
using UnityEngine;

public class DropPile : MonoBehaviour, ICardDropArea
{
    [SerializeField] private float dropDuration = 0.2f;
    [SerializeField] private float stackOffset = 0.01f;

    private int _pileCount = 0;

    public void OnCardDrop(Card card)
    {
        card.SetOrigin(this);
        card.SetSortingLayer(LayerBase.Pile);
        card.SetSortingOrder(_pileCount);

        float offset = Mathf.Abs(stackOffset) * _pileCount;
        Vector3 targetPos = new(
            transform.position.x - offset,
            transform.position.y + offset,
            -_pileCount * 0.1f);
        _pileCount++;

        card.transform.DOKill();
        card.transform.DOMove(targetPos, dropDuration).SetEase(Ease.OutCubic);
        card.transform.DOScale(0.75f, dropDuration).SetEase(Ease.OutCubic);
        card.transform.DORotateQuaternion(Quaternion.identity, dropDuration).SetEase(Ease.OutCubic);
    }
}