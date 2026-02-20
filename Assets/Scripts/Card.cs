using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Card : MonoBehaviour
{
    [SerializeField] private float riseHeight = 1f;
    [SerializeField] private float animDuration = 0.1f;

    private bool _isSelected = false;
    private Coroutine _anim;
    private SpriteRenderer _sr;
    private Vector3 _originalPosition;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    public void Click()
    {
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
        Debug.Log("Long pressed: " + gameObject.name);
        // your long press logic here
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