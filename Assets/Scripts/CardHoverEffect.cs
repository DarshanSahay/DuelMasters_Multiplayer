using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CardHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    public float hoverScale = 1.15f;
    public float scaleDuration = 0.15f;
    public float wobbleAmount = 6f;
    public float wobbleSpeed = 0.12f;

    private Vector3 baseScale;
    private Tween scaleTween;
    private Tween wobbleTween;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        scaleTween?.Kill();
        wobbleTween?.Kill();

        scaleTween = transform.DOScale(hoverScale, scaleDuration).SetEase(Ease.OutQuad);

        wobbleTween = DOTween.Sequence()
            .Append(transform.DORotate(new Vector3(0, 0, wobbleAmount), wobbleSpeed).SetEase(Ease.InOutSine))
            .Append(transform.DORotate(new Vector3(0, 0, -wobbleAmount), wobbleSpeed).SetEase(Ease.InOutSine))
            .SetLoops(-1, LoopType.Restart);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        scaleTween?.Kill();
        wobbleTween?.Kill();

        transform.localRotation = Quaternion.identity;

        scaleTween = transform.DOScale(baseScale, scaleDuration).SetEase(Ease.OutQuad);
    }
}