using System.Collections;
using UnityEngine;

public class TutorialPanelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Tutorial Content")]
    [Tooltip("The first tutorial content, e.g. WASD movement and mouse camera controls.")]
    [SerializeField] private GameObject firstTutorialContent;
    [Tooltip("The second tutorial content, e.g. E to interact with the environment.")]
    [SerializeField] private GameObject secondTutorialContent;

    [Header("Display Settings")]
    [SerializeField, Min(0f)] private float firstDisplayDuration = 10f;
    [SerializeField, Min(0f)] private float delayBeforeSecondTutorial = 1f;
    [SerializeField, Min(0f)] private float secondDisplayDuration = 5f;

    [Header("Transition Settings")]
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.4f;
    [SerializeField] private float slideDistance = 40f;
    [SerializeField, Range(0.1f, 1f)] private float startingScale = 0.95f;

    private RectTransform panelRect;
    private Vector2 originalPosition;
    private Vector3 originalScale;
    private Coroutine tutorialCoroutine;

    private void Awake()
    {
        if (tutorialPanel == null || canvasGroup == null)
        {
            Debug.LogError(
                "TutorialPanelController: Assign Tutorial Panel and Canvas Group."
            );
            enabled = false;
            return;
        }

        panelRect = tutorialPanel.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            Debug.LogError("TutorialPanelController: Tutorial Panel needs a RectTransform.");
            enabled = false;
            return;
        }

        originalPosition = panelRect.anchoredPosition;
        originalScale = panelRect.localScale;

        canvasGroup.alpha = 0f;
        tutorialPanel.SetActive(false);

        if (firstTutorialContent != null)
            firstTutorialContent.SetActive(true);

        if (secondTutorialContent != null)
            secondTutorialContent.SetActive(false);
    }

    private void OnEnable()
    {
        if (dialogueManager != null)
            dialogueManager.DialogueEnded += ShowTutorial;
    }

    private void OnDisable()
    {
        if (dialogueManager != null)
            dialogueManager.DialogueEnded -= ShowTutorial;

        if (tutorialCoroutine != null)
        {
            StopCoroutine(tutorialCoroutine);
            tutorialCoroutine = null;
        }
    }

    private void ShowTutorial()
    {
        if (tutorialCoroutine != null)
            StopCoroutine(tutorialCoroutine);

        tutorialCoroutine = StartCoroutine(PlayTutorialSequence());
    }

    private IEnumerator PlayTutorialSequence()
    {
        // First tutorial: movement and camera.
        SetTutorialContent(firstTutorialContent, secondTutorialContent, true);
        yield return ShowPanel();

        yield return new WaitForSecondsRealtime(firstDisplayDuration);
        yield return HidePanel();

        tutorialPanel.SetActive(false);

        // Wait one second after the first tutorial has disappeared.
        yield return new WaitForSecondsRealtime(delayBeforeSecondTutorial);

        // Second tutorial: interaction.
        SetTutorialContent(firstTutorialContent, secondTutorialContent, false);
        yield return ShowPanel();

        yield return new WaitForSecondsRealtime(secondDisplayDuration);
        yield return HidePanel();

        tutorialPanel.SetActive(false);
        panelRect.anchoredPosition = originalPosition;
        panelRect.localScale = originalScale;
        canvasGroup.alpha = 0f;
        tutorialCoroutine = null;
    }

    private void SetTutorialContent(
        GameObject firstContent,
        GameObject secondContent,
        bool showFirst)
    {
        if (firstContent != null)
            firstContent.SetActive(showFirst);

        if (secondContent != null)
            secondContent.SetActive(!showFirst);
    }

    private IEnumerator ShowPanel()
    {
        tutorialPanel.SetActive(true);

        panelRect.anchoredPosition =
            originalPosition + Vector2.up * slideDistance;
        panelRect.localScale = originalScale * startingScale;
        canvasGroup.alpha = 0f;

        yield return AnimatePanel(
            0f, 1f,
            originalPosition + Vector2.up * slideDistance,
            originalPosition,
            startingScale, 1f
        );
    }

    private IEnumerator HidePanel()
    {
        yield return AnimatePanel(
            1f, 0f,
            originalPosition,
            originalPosition + Vector2.up * slideDistance,
            1f, startingScale
        );
    }

    private IEnumerator AnimatePanel(
        float startAlpha,
        float endAlpha,
        Vector2 startPosition,
        Vector2 endPosition,
        float startScale,
        float endScale)
    {
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / transitionDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, easedProgress);
            panelRect.anchoredPosition =
                Vector2.Lerp(startPosition, endPosition, easedProgress);
            panelRect.localScale =
                originalScale * Mathf.Lerp(startScale, endScale, easedProgress);

            yield return null;
        }

        canvasGroup.alpha = endAlpha;
        panelRect.anchoredPosition = endPosition;
        panelRect.localScale = originalScale * endScale;
    }
}
