using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private TMP_Text coinChangeText;
    [SerializeField] private Slider reputationBar;
    [SerializeField, Min(0)] private int startingCoins = 10;
    [SerializeField, Range(0, 100)] private int startingReputation = 50;

    private const int MaxReputation = 100;
    private const float RewardDuration = 1.5f;
    private const float CoinTravelDuration = 0.65f;
    private const float CoinCountDuration = 0.8f;
    private const float CoinTravelDistance = 50f;
    private const float ReputationZoomDuration = 0.25f;
    private const float ReputationScale = 1.15f;
    private static readonly Color CoinGainColor = new Color32(255, 213, 92, 255);
    private static readonly Color CoinLossColor = new Color32(255, 96, 87, 255);

    private bool dialogueActive;
    private bool cutsceneActive;
    private bool choiceActive;
    private bool missionBannerActive;
    private bool shopActive;
    private bool pauseMenuActive;
    private bool travelMenuActive;
    private int displayedCoins = -1;
    private float displayedReputation;
    private readonly Queue<int> pendingCoinBalances = new();
    private Coroutine coinAnimation;
    private Coroutine reputationAnimation;
    private Vector2 coinChangeDestination;
    private Vector3 normalReputationScale;

    public int Coins { get; private set; }
    public int Reputation { get; private set; }

    public bool IsVisible => isActiveAndEnabled && !dialogueActive && !cutsceneActive &&
        !choiceActive && !missionBannerActive && !shopActive && !pauseMenuActive && !travelMenuActive;

    private void Awake()
    {
        Coins = Mathf.Max(0, startingCoins);
        Reputation = Mathf.Clamp(startingReputation, 0, MaxReputation);
        if (reputationBar != null)
            normalReputationScale = reputationBar.transform.localScale;
        if (coinChangeText != null)
        {
            coinChangeDestination = coinChangeText.rectTransform.anchoredPosition;
            coinChangeText.gameObject.SetActive(false);
        }
        ShowCoins(Coins);
        ShowReputation(Reputation, 1f);
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

        if (coinAnimation != null)
        {
            StopCoroutine(coinAnimation);
            coinAnimation = null;
            pendingCoinBalances.Clear();
            if (coinChangeText != null)
                coinChangeText.gameObject.SetActive(false);
            ShowCoins(Coins);
        }

        if (reputationAnimation != null)
        {
            StopCoroutine(reputationAnimation);
            reputationAnimation = null;
            ShowReputation(Reputation, 1f);
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

    internal void SetShopActive(bool active)
    {
        shopActive = active;
        RefreshVisibility();
    }

    internal void SetPauseMenuActive(bool active)
    {
        pauseMenuActive = active;
        RefreshVisibility();
    }

    internal bool TrySpendCoins(int price)
    {
        if (price <= 0 || Coins < price)
            return false;

        AddRewards(-price, 0);
        return true;
    }

    internal void SetTravelMenuActive(bool active)
    {
        travelMenuActive = active;
        RefreshVisibility();
    }

    internal void RestoreTravelBalances(int coins, int reputation)
    {
        Coins = coins;
        Reputation = reputation;
        ShowCoins(Coins);
        ShowReputation(Reputation, 1f);
    }

    public void AddRewards(int coinDelta, int reputationDelta)
    {
        int nextCoins = (int)Math.Clamp((long)Coins + coinDelta, 0L, int.MaxValue);
        int nextReputation = (int)Math.Clamp((long)Reputation + reputationDelta, 0L, MaxReputation);
        bool coinsChanged = nextCoins != Coins;
        bool reputationChanged = nextReputation != Reputation;
        if (!coinsChanged && !reputationChanged)
            return;

        Coins = nextCoins;
        Reputation = nextReputation;
        if (!isActiveAndEnabled)
        {
            ShowCoins(Coins);
            ShowReputation(Reputation, 1f);
            return;
        }

        if (coinsChanged)
        {
            pendingCoinBalances.Enqueue(Coins);
            if (coinAnimation == null)
                coinAnimation = StartCoroutine(AnimateCoinChanges());
        }

        if (reputationChanged)
        {
            if (reputationAnimation != null)
                StopCoroutine(reputationAnimation);
            reputationAnimation = StartCoroutine(AnimateReputation(displayedReputation));
        }
    }

    private IEnumerator AnimateCoinChanges()
    {
        // Keep each gain or loss visible even if several transactions arrive close together.
        while (pendingCoinBalances.Count > 0)
        {
            int from = displayedCoins;
            int to = pendingCoinBalances.Dequeue();
            if (coinChangeText != null)
                yield return FloatCoinChange(to - from);

            for (float elapsed = 0f; elapsed < CoinCountDuration;)
            {
                yield return null;
                if (!IsVisible)
                    continue;

                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / CoinCountDuration);
                ShowCoins((int)Math.Round(from + ((double)to - from) * progress));
            }

            ShowCoins(to);
        }

        coinAnimation = null;
    }

    private IEnumerator FloatCoinChange(int change)
    {
        while (!IsVisible)
            yield return null;

        RectTransform rect = coinChangeText.rectTransform;
        Vector2 origin = coinChangeDestination - Vector2.up * CoinTravelDistance;
        Color color = change > 0 ? CoinGainColor : CoinLossColor;
        coinChangeText.text = change.ToString("+0;-0");
        coinChangeText.color = color;
        rect.anchoredPosition = origin;
        coinChangeText.gameObject.SetActive(true);

        for (float elapsed = 0f; elapsed < CoinTravelDuration;)
        {
            yield return null;
            if (!IsVisible)
                continue;

            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / CoinTravelDuration);
            rect.anchoredPosition = Vector2.Lerp(origin, coinChangeDestination, Mathf.SmoothStep(0f, 1f, progress));
            color.a = Mathf.SmoothStep(1f, 0f, (progress - 0.45f) / 0.55f);
            coinChangeText.color = color;
        }

        coinChangeText.gameObject.SetActive(false);
    }

    private IEnumerator AnimateReputation(float fromReputation)
    {
        bool reputationChanged = !Mathf.Approximately(fromReputation, Reputation);
        for (float elapsed = 0f; elapsed < RewardDuration;)
        {
            yield return null;
            if (!IsVisible)
                continue;

            elapsed = Mathf.Min(elapsed + Time.unscaledDeltaTime, RewardDuration);
            float reputationProgress = (elapsed - ReputationZoomDuration) /
                (RewardDuration - 2f * ReputationZoomDuration);
            float zoom = reputationChanged
                ? Mathf.SmoothStep(1f, ReputationScale,
                    Mathf.Min(elapsed, RewardDuration - elapsed) / ReputationZoomDuration)
                : 1f;

            float reputation = Mathf.SmoothStep(fromReputation, Reputation, reputationProgress);
            ShowReputation(reputation, zoom);
        }

        ShowReputation(Reputation, 1f);
        reputationAnimation = null;
    }

    private void ShowCoins(int coins)
    {
        if (coins != displayedCoins && coinText != null)
            coinText.text = coins.ToString();
        displayedCoins = coins;
    }

    private void ShowReputation(float reputation, float zoom)
    {
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
