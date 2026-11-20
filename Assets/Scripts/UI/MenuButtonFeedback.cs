
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class MenuButtonFeedback : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Press Effect")]
    [SerializeField, Range(0.5f, 1f)]
    private float pressedScale = 0.9f;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        MainMenuAudioController.Instance?.PlayHover();
    }

    public void OnPointerExit(PointerEventData eventData) => ResetScale();

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        rectTransform.localScale = originalScale * pressedScale;
        MainMenuAudioController.Instance?.PlayPress();
    }

    public void OnPointerUp(PointerEventData eventData) => ResetScale();

    private void OnDisable() => ResetScale();

    private void ResetScale()
    {
        if (rectTransform != null)
            rectTransform.localScale = originalScale;
    }
}
