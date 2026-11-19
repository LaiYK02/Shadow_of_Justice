using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnergyHUDFeedback : MonoBehaviour
{
    private const float WarningDuration = 2f;
    private const float ShrinkDuration = 0.3f;
    private const float ShakeDuration = 0.28f;
    private const float BarShakeDistance = 8f;
    private const float HintShakeDistance = 14f;
    private static readonly int Exhaustion = Shader.PropertyToID("_Exhaustion");

    [SerializeField] private PlayerMovement player;
    [SerializeField] private UnityEngine.UI.Slider energyBar;
    [SerializeField] private TMP_Text warningHint;
    [SerializeField] private Shader energyFillShader;

    private RectTransform barRect;
    private UnityEngine.UI.Image fill;
    private Material originalMaterial;
    private Material fillMaterial;
    private Vector2 normalBarPosition;
    private Vector2 normalHintPosition;
    private Vector3 normalHintScale;
    private Color warningYellow;
    private Color warningRed;
    private float warningStartedAt;
    private float warningEndsAt;
    private float shakeEndsAt;

    private void Awake()
    {
        if (energyBar != null)
        {
            barRect = (RectTransform)energyBar.transform;
            normalBarPosition = barRect.anchoredPosition;
            if (energyBar.fillRect != null)
                fill = energyBar.fillRect.GetComponent<UnityEngine.UI.Image>();

            if (fill != null && energyFillShader != null)
            {
                originalMaterial = fill.material;
                fillMaterial = new Material(energyFillShader);
                fill.material = fillMaterial;
            }
        }

        if (warningHint != null)
        {
            normalHintPosition = warningHint.rectTransform.anchoredPosition;
            normalHintScale = warningHint.rectTransform.localScale;
            warningYellow = warningHint.color;
            warningRed = new Color(1f, 0.18f, 0.15f, warningYellow.a);
            warningHint.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (player != null)
            player.InsufficientEnergy += ShowWarning;
    }

    private void OnDisable()
    {
        if (player != null)
            player.InsufficientEnergy -= ShowWarning;
        ClearWarning();
    }

    private void OnDestroy()
    {
        if (fill != null && fillMaterial != null)
            fill.material = originalMaterial;
        if (fillMaterial != null)
            Destroy(fillMaterial);
    }

    private void ShowWarning()
    {
        if (energyBar == null || !energyBar.isActiveAndEnabled)
            return;

        float now = Time.unscaledTime;
        if (warningHint != null)
        {
            if (!warningHint.gameObject.activeSelf)
            {
                warningStartedAt = now;
                warningHint.gameObject.SetActive(true);
            }

            warningEndsAt = now + WarningDuration;
        }

        shakeEndsAt = now + ShakeDuration;
    }

    private void LateUpdate()
    {
        if (fillMaterial != null && player != null)
            fillMaterial.SetFloat(Exhaustion, player.EnergyLockoutFraction);

        if (energyBar == null || !energyBar.isActiveAndEnabled)
        {
            ClearWarning();
            return;
        }

        float now = Time.unscaledTime;
        if (warningHint != null && warningHint.gameObject.activeSelf)
        {
            float remaining = warningEndsAt - now;
            if (remaining <= 0f)
                HideHint();
            else
            {
                float blink = Mathf.PingPong((now - warningStartedAt) * 3f, 1f);
                warningHint.color = Color.Lerp(warningYellow, warningRed, blink);
                warningHint.rectTransform.localScale = normalHintScale *
                    Mathf.SmoothStep(0.65f, 1f, remaining / ShrinkDuration);
            }
        }

        if (shakeEndsAt > 0f)
        {
            float remaining = Mathf.Max(0f, shakeEndsAt - now);
            float progress = 1f - remaining / ShakeDuration;
            Vector2 shake = Vector2.right * (Mathf.Cos(progress * Mathf.PI * 6f) * (1f - progress));
            barRect.anchoredPosition = normalBarPosition + shake * BarShakeDistance;
            if (warningHint != null && warningHint.gameObject.activeSelf)
                warningHint.rectTransform.anchoredPosition = normalHintPosition + shake * HintShakeDistance;
            if (remaining == 0f)
                shakeEndsAt = 0f;
        }
    }

    private void ClearWarning()
    {
        HideHint();
        if (shakeEndsAt > 0f && barRect != null)
            barRect.anchoredPosition = normalBarPosition;
        shakeEndsAt = 0f;
    }

    private void HideHint()
    {
        if (warningHint == null || !warningHint.gameObject.activeSelf)
            return;

        warningHint.gameObject.SetActive(false);
        warningHint.color = warningYellow;
        warningHint.rectTransform.anchoredPosition = normalHintPosition;
        warningHint.rectTransform.localScale = normalHintScale;
    }
}
