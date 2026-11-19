using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class MainMenuIntro : MonoBehaviour
{
    [SerializeField] private CanvasGroup overlay;
    [SerializeField] private CanvasGroup logoCard;
    [SerializeField] private CanvasGroup projectCard;
    [SerializeField] private GameObject skipHint;
    [SerializeField] private CanvasGroup menuContent;

    [SerializeField, Min(0f)] private float cardFadeDuration = 0.5f;
    [SerializeField, Min(0f)] private float logoHoldDuration = 2f;
    [SerializeField, Min(0f)] private float projectHoldDuration = 6f;
    [SerializeField, Min(0f)] private float menuFadeDuration = 0.75f;
    [SerializeField, Min(0f)] private float hintDuration = 3f;

    private Action onFinished;
    private bool canSkip;
    private float hideHintAt;

    public void Play(Action finished)
    {
        if (overlay == null || logoCard == null || projectCard == null || skipHint == null || menuContent == null)
        {
            Debug.LogError("MainMenuIntro: Assign the intro UI and menu content.", this);
            finished?.Invoke();
            return;
        }

        onFinished = finished;
        menuContent.alpha = 0f;
        menuContent.interactable = false;
        menuContent.blocksRaycasts = false;
        overlay.alpha = 1f;
        logoCard.alpha = 0f;
        projectCard.alpha = 0f;
        skipHint.SetActive(false);
        overlay.gameObject.SetActive(true);
        canSkip = true;
        StartCoroutine(PlayCards());
    }

    private void Update()
    {
        if (!canSkip)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
        {
            StopAllCoroutines();
            StartCoroutine(RevealMenu());
            return;
        }

        Mouse mouse = Mouse.current;
        bool pointerActivity = mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 0f ||
            mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame);
        if (pointerActivity || (keyboard != null && keyboard.anyKey.wasPressedThisFrame))
        {
            hideHintAt = Time.unscaledTime + hintDuration;
            skipHint.SetActive(true);
        }
        else if (skipHint.activeSelf && Time.unscaledTime >= hideHintAt)
        {
            skipHint.SetActive(false);
        }
    }

    private IEnumerator PlayCards()
    {
        yield return ShowCard(logoCard, logoHoldDuration);
        yield return ShowCard(projectCard, projectHoldDuration);
        yield return RevealMenu();
    }

    private IEnumerator ShowCard(CanvasGroup card, float holdDuration)
    {
        yield return Fade(card, 1f, cardFadeDuration);
        yield return new WaitForSecondsRealtime(holdDuration);
        yield return Fade(card, 0f, cardFadeDuration);
    }

    private IEnumerator RevealMenu()
    {
        canSkip = false;
        skipHint.SetActive(false);
        menuContent.alpha = 1f;
        yield return Fade(overlay, 0f, menuFadeDuration);

        overlay.gameObject.SetActive(false);
        menuContent.interactable = true;
        menuContent.blocksRaycasts = true;
        onFinished?.Invoke();
        onFinished = null;
    }

    private static IEnumerator Fade(CanvasGroup group, float target, float duration)
    {
        float start = group.alpha;
        for (float elapsed = 0f; elapsed < duration;)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / duration));
        }
        group.alpha = target;
    }
}
