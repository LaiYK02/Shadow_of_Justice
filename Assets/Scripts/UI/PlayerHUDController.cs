using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shows player HUD elements only during active gameplay.</summary>
public class PlayerHUDController : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private GameObject[] hudElements;

    [Header("Rewards")]
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private Slider reputationBar;
    [SerializeField, Min(0)] private int startingCoins = 10;
    [SerializeField, Range(0, 100)] private int startingReputation = 50;

    private const int MaxReputation = 100;
    private const float RewardDuration = 1.5f;
    private const float ReputationZoomDuration = 0.25f;
    private const float ReputationScale = 1.15f;

    private bool dialogueActive;
    private bool cutsceneActive;
    private bool choiceActive;
    private bool missionBannerActive;
    private int displayedCoins = -1;
    private float displayedReputation;
    private Coroutine rewardAnimation;
    private Vector3 normalReputationScale;

    public int Coins { get; private set; }
    public int Reputation { get; private set; }

    public bool IsVisible => isActiveAndEnabled && !dialogueActive && !cutsceneActive &&
        !choiceActive && !missionBannerActive;

    private void Awake()
    {
        Coins = Mathf.Max(0, startingCoins);
        Reputation = Mathf.Clamp(startingReputation, 0, MaxReputation);
        if (reputationBar != null)
            normalReputationScale = reputationBar.transform.localScale;
        ShowRewardValues(Coins, Reputation, 1f);
    }

    private void OnEnable()
    {
        if (dialogueManager != null)
        {
            dialogueActive = dialogueManager.IsPlaying;
            dialogueManager.DialogueStarted += HandleDialogueStarted;
            dialogueManager.DialogueEnded += HandleDialogueEnded;
        }

        RefreshVisibility();
    }

    private void OnDisable()
    {
        if (dialogueManager != null)
        {
            dialogueManager.DialogueStarted -= HandleDialogueStarted;
            dialogueManager.DialogueEnded -= HandleDialogueEnded;
        }

        if (rewardAnimation != null)
        {
            StopCoroutine(rewardAnimation);
            rewardAnimation = null;
            ShowRewardValues(Coins, Reputation, 1f);
        }
    }

    public void SetCutsceneActive(bool active)
    {
        cutsceneActive = active;
        RefreshVisibility();
    }

    public void SetChoiceActive(bool active)
    {
        choiceActive = active;
        RefreshVisibility();
    }

    public void SetMissionBannerActive(bool active)
    {
        missionBannerActive = active;
        RefreshVisibility();
    }

    public void AddRewards(int coinDelta, int reputationDelta)
    {
        int nextCoins = (int)Math.Clamp((long)Coins + coinDelta, 0L, int.MaxValue);
        int nextReputation = (int)Math.Clamp((long)Reputation + reputationDelta, 0L, MaxReputation);
        if (nextCoins == Coins && nextReputation == Reputation)
            return;

        Coins = nextCoins;
        Reputation = nextReputation;
        if (rewardAnimation != null)
            StopCoroutine(rewardAnimation);
        if (isActiveAndEnabled)
            rewardAnimation = StartCoroutine(AnimateRewards(displayedCoins, displayedReputation));
        else
            ShowRewardValues(Coins, Reputation, 1f);
    }

    private IEnumerator AnimateRewards(int fromCoins, float fromReputation)
    {
        bool reputationChanged = !Mathf.Approximately(fromReputation, Reputation);
        for (float elapsed = 0f; elapsed < RewardDuration;)
        {
            yield return null;
            if (!IsVisible)
                continue;

            elapsed = Mathf.Min(elapsed + Time.unscaledDeltaTime, RewardDuration);
            float coinProgress = Mathf.SmoothStep(0f, 1f, elapsed / RewardDuration);
            float reputationProgress = (elapsed - ReputationZoomDuration) /
                (RewardDuration - 2f * ReputationZoomDuration);
            float zoom = reputationChanged
                ? Mathf.SmoothStep(1f, ReputationScale,
                    Mathf.Min(elapsed, RewardDuration - elapsed) / ReputationZoomDuration)
                : 1f;

            int coins = (int)Math.Round(fromCoins + ((double)Coins - fromCoins) * coinProgress);
            float reputation = Mathf.SmoothStep(fromReputation, Reputation, reputationProgress);
            ShowRewardValues(coins, reputation, zoom);
        }

        ShowRewardValues(Coins, Reputation, 1f);
        rewardAnimation = null;
    }

    private void ShowRewardValues(int coins, float reputation, float zoom)
    {
        if (coins != displayedCoins && coinText != null)
            coinText.text = coins.ToString();
        displayedCoins = coins;
        displayedReputation = reputation;

        if (reputationBar != null)
        {
            reputationBar.SetValueWithoutNotify(Mathf.Lerp(reputationBar.minValue, reputationBar.maxValue,
                reputation / MaxReputation));
            reputationBar.transform.localScale = normalReputationScale * zoom;
        }
    }

    private void HandleDialogueStarted()
    {
        dialogueActive = true;
        RefreshVisibility();
    }

    private void HandleDialogueEnded()
    {
        dialogueActive = false;
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        bool visible = IsVisible;
        if (hudElements == null)
            return;

        foreach (GameObject element in hudElements)
        {
            if (element != null)
                element.SetActive(visible);
        }
    }
}
