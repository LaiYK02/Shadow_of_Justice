using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Image))]
public sealed class MissionTargetMarker : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform targetAnchor;
    [SerializeField, Min(0f)] private float floatHeight = 0.12f;
    [SerializeField, Min(0.1f)] private float floatPeriod = 1.6f;

    private Image image;
    private RectTransform parentRect;
    private bool requested;

    private void Awake()
    {
        image = GetComponent<Image>();
        parentRect = (RectTransform)transform.parent;
        image.enabled = false;
    }

    public void SetRequested(bool visible)
    {
        requested = visible;
        if (!visible && image != null)
            image.enabled = false;
    }

    private void LateUpdate()
    {
        if (!requested || !worldCamera || !targetAnchor || !targetAnchor.gameObject.activeInHierarchy)
        {
            image.enabled = false;
            return;
        }

        float phase = Time.time * (2f * Mathf.PI / floatPeriod);
        float offset = floatHeight * (0.5f - 0.5f * Mathf.Cos(phase));
        Vector3 screenPoint = worldCamera.WorldToScreenPoint(targetAnchor.position + Vector3.up * offset);

        // Project into the HUD overlay so opaque world geometry cannot cover the marker.
        if (screenPoint.z <= worldCamera.nearClipPlane || !worldCamera.pixelRect.Contains(screenPoint) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, null, out Vector2 point))
        {
            image.enabled = false;
            return;
        }

        image.rectTransform.localPosition = point;
        image.enabled = true;
    }
}
