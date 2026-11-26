using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TreasureHunt : MonoBehaviour
{
    private const int CompletionBonus = 100;
    private const float PopupDuration = 1.75f;
    private const float PopupFadeDuration = 0.25f;

    [SerializeField] private CanvasGroup progressPanel;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private MissionFlowController missionFlow;

    private int totalTreasures;
    private bool completionRewardPending;
    private Coroutine progressRoutine;

    private HashSet<string> Pickups
    {
        get
        {
            string district = gameObject.scene.path;
            if (!DistrictTravelSession.CollectedTreasures.TryGetValue(district, out HashSet<string> pickups))
            {
                pickups = new HashSet<string>();
                DistrictTravelSession.CollectedTreasures.Add(district, pickups);
            }
            return pickups;
        }
    }

    private void Awake()
    {
        totalTreasures = GetComponentsInChildren<TreasureInteractable>(true).Length;
        if (progressPanel != null)
            progressPanel.gameObject.SetActive(false);
    }

    internal bool WasCollected(string treasureName) => Pickups.Contains(treasureName);

    internal void RecordPickup(string treasureName, PlayerHUDController playerHUD)
    {
        if (!Pickups.Add(treasureName))
            return;

        int foundTreasures = Pickups.Count;
        if (progressPanel != null && progressText != null)
        {
            progressText.text = $"{foundTreasures}/{totalTreasures}";
            if (progressRoutine != null)
                StopCoroutine(progressRoutine);
            progressRoutine = StartCoroutine(ShowProgress(playerHUD));
        }

        if (foundTreasures == totalTreasures)
        {
            completionRewardPending = true;
            StartCoroutine(AwardCompletionBonus(playerHUD));
        }
    }

    private IEnumerator ShowProgress(PlayerHUDController playerHUD)
    {
        progressPanel.alpha = 1f;
        progressPanel.gameObject.SetActive(true);

        float remaining = PopupDuration;
        while (remaining > 0f)
        {
            yield return null;
            bool visible = playerHUD != null && playerHUD.IsVisible;
            if (visible)
                remaining -= Time.unscaledDeltaTime;
            progressPanel.alpha = visible ? Mathf.Clamp01(remaining / PopupFadeDuration) : 0f;
        }

        progressPanel.gameObject.SetActive(false);
        progressRoutine = null;
    }

    private IEnumerator AwardCompletionBonus(PlayerHUDController playerHUD)
    {
        if (missionFlow != null)
            yield return missionFlow.ShowTreasureHuntCompletion();

        SettleCompletionReward(playerHUD);
    }

    internal void SettleCompletionReward(PlayerHUDController playerHUD)
    {
        if (!completionRewardPending || playerHUD == null)
            return;
        completionRewardPending = false;
        playerHUD.AddRewards(CompletionBonus, 0);
    }
}
