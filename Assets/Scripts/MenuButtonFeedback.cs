
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class MenuButtonFeedback : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private Image targetImage;

    [Header("Hover Effect")]
    [SerializeField, Range(0.3f, 1f)]
    private float normalBrightness = 0.9f;

    [Header("Press Effect")]
    [SerializeField, Range(0.5f, 1f)]
    private float pressedScale = 0.9f;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Color originalColor;

    private bool isHovered;
    private bool isPressed;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (targetImage == null)
            targetImage = GetComponent<Image>();

        originalScale = rectTransform.localScale;

        if (targetImage != null)
            originalColor = targetImage.color;

        UpdateVisuals();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UpdateVisuals();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        UpdateVisuals();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        isPressed = true;
        UpdateVisuals();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (rectTransform != null)
        {
            rectTransform.localScale =
                originalScale * (isPressed ? pressedScale : 1f);
        }

        if (targetImage != null)
        {
            targetImage.color = isHovered
                ? originalColor
                : originalColor * normalBrightness;
        }
    }

    private void OnDisable()
    {
        isHovered = false;
        isPressed = false;

        if (rectTransform != null)
            rectTransform.localScale = originalScale;

        if (targetImage != null)
            targetImage.color = originalColor;
    }

    private void OnEnable()
    {
        if (rectTransform != null)
            UpdateVisuals();
    }
}
