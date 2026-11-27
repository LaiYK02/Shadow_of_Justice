using System.Collections;
using UnityEngine;

internal sealed class MissionBanner
{
    private const float SlowMotion = 0.5f;
    private readonly PlayerHUDController hud;
    private GameObject activePanel;
    private float? previousTimeScale;
    private float previousFixedDeltaTime;

    public MissionBanner(PlayerHUDController hud) => this.hud = hud;

    public IEnumerator Play(GameObject panel, CanvasGroup fade, float fadeIn, float hold, float fadeOut)
    {
        activePanel = panel;
        previousTimeScale = Time.timeScale;
        previousFixedDeltaTime = Time.fixedDeltaTime;
        Time.timeScale *= SlowMotion;
        Time.fixedDeltaTime *= SlowMotion;
        if (hud != null)
            hud.SetMissionBannerActive(true);

        if (fade != null)
        {
            fade.alpha = 0f;
            fade.interactable = false;
            fade.blocksRaycasts = false;
            panel.SetActive(true);
            yield return Fade(fade, 0f, 1f, fadeIn);
            yield return new WaitForSecondsRealtime(hold);
            yield return Fade(fade, 1f, 0f, fadeOut);
        }
        else
        {
            panel.SetActive(true);
            yield return new WaitForSecondsRealtime(fadeIn + hold + fadeOut);
        }

        Close();
    }

    public void Close()
    {
        if (!previousTimeScale.HasValue)
            return;

        if (activePanel != null)
            activePanel.SetActive(false);
        activePanel = null;
        Time.timeScale = previousTimeScale.Value;
        Time.fixedDeltaTime = previousFixedDeltaTime;
        previousTimeScale = null;
        if (hud != null)
            hud.SetMissionBannerActive(false);
    }

    private static IEnumerator Fade(CanvasGroup fade, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fade.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        fade.alpha = to;
    }
}
