using TMPro;
using UnityEngine;

/// <summary>Animates an objective in its existing HUD row.</summary>
internal sealed class MissionObjectiveRow
{
    private const float SlideDistance = 120f;
    private const float SlideDuration = 0.3f;
    private const float CompletionHold = 0.45f;

    private enum Stage { Hidden, Entering, Visible, ShowingCompletion, Exiting }

    private readonly TMP_Text label;
    private readonly RectTransform row;
    private readonly CanvasGroup fade;
    private Vector2 restingPosition;
    private Stage stage;
    private string nextText;
    private string completedText;
    private bool dismissRequested;
    private float elapsed;

    public bool HasContent => stage != Stage.Hidden || nextText != null;
    public bool IsShowingCompletion => stage == Stage.ShowingCompletion;
    public bool IsDismissing => dismissRequested;
    public float ListPositionY => restingPosition.y;
    public float Height => row.rect.height;

    public MissionObjectiveRow(TMP_Text label, RectTransform row)
    {
        this.label = label;
        this.row = row;
        restingPosition = row.anchoredPosition;
        if (!row.TryGetComponent(out fade))
            fade = row.gameObject.AddComponent<CanvasGroup>();

        row.gameObject.SetActive(false);
    }

    public void Show(string text)
    {
        if (!HasContent)
            nextText = text;
    }

    public void MarkComplete(string text)
    {
        if (HasContent && completedText == null)
            completedText = text;
    }

    public void Dismiss()
    {
        if (completedText != null)
            dismissRequested = true;
    }

    public void SetListPosition(float y)
    {
        if (restingPosition.y == y)
            return;

        row.anchoredPosition += Vector2.up * (y - restingPosition.y);
        restingPosition.y = y;
    }

    // The owner advances the row only while its HUD panel is visible.
    public void Advance(float deltaTime)
    {
        switch (stage)
        {
            case Stage.Hidden:
                if (nextText == null)
                    return;

                SetText(nextText, false);
                nextText = null;
                SetAppearance(-SlideDistance, 0f);
                row.gameObject.SetActive(true);
                Begin(Stage.Entering);
                break;

            case Stage.Entering:
                float enter = SlideProgress(deltaTime);
                SetAppearance(Mathf.Lerp(-SlideDistance, 0f, enter), enter);
                if (elapsed >= SlideDuration)
                    Begin(Stage.Visible);
                break;

            case Stage.Visible:
                if (completedText != null)
                {
                    SetText(completedText, true);
                    Begin(Stage.ShowingCompletion);
                }
                break;

            case Stage.ShowingCompletion:
                if (!dismissRequested)
                    return;

                elapsed += deltaTime;
                if (elapsed >= CompletionHold)
                    Begin(Stage.Exiting);
                break;

            case Stage.Exiting:
                float exit = SlideProgress(deltaTime);
                SetAppearance(SlideDistance * exit, 1f - exit);
                if (elapsed >= SlideDuration)
                {
                    row.gameObject.SetActive(false);
                    SetAppearance(0f, 1f);
                    completedText = null;
                    dismissRequested = false;
                    Begin(Stage.Hidden);
                }
                break;
        }
    }

    private void Begin(Stage nextStage)
    {
        stage = nextStage;
        elapsed = 0f;
    }

    private float SlideProgress(float deltaTime)
    {
        elapsed += deltaTime;
        return Mathf.SmoothStep(0f, 1f, elapsed / SlideDuration);
    }

    private void SetText(string text, bool completed)
    {
        label.text = text;
        FontStyles style = label.fontStyle & ~FontStyles.Strikethrough;
        label.fontStyle = completed ? style | FontStyles.Strikethrough : style;
    }

    private void SetAppearance(float offset, float alpha)
    {
        row.anchoredPosition = restingPosition + Vector2.right * offset;
        fade.alpha = alpha;
    }
}
