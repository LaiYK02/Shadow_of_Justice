using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class MissionFlowController : MonoBehaviour
{
    private const string TalkToOfficer = "Talk to the Fellow Officer";
    private const string CollectEvidence = "Collect the evidence";
    private const string ChaseSuspect = "Chase and hit the Suspect";
    private const string DefeatSuspect = "Defeat the suspicious person";
    private const string ReturnToOfficer = "Talk to the Fellow Officer again";
    private const string TravelToOldTown = "Travel to Old Town via Grab Car Box";
    private const string FindTreasures = "Find all the treasures.";
    private const string HiddenObjective = "......";
    private const float CompletionBannerDuration = 3f;
    private const float TreasureHuntCompletionHold = 3f;
    private const int ArrestRewardCoins = 200;
    private const int ArrestRewardReputation = 10;
    private const int ReleaseRewardCoins = 100;
    private const int ReleaseRewardReputation = -10;
    private const int DebriefReputation = 5;
    private static readonly Vector2 BackdropPadding = new(16f, 12f);

    private enum Resolution { Pending, Completing, AwaitingDebrief, Debriefed }

    [Header("References")]
    [SerializeField] private OfficerInteraction fellowOfficer;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private PlayerHUDController playerHUD;
    [SerializeField] private MissionTargetMarker officerMarker;
    [SerializeField] private MissionTargetMarker grabCarMarker;

    [Header("Mission Start Panel")]
    [SerializeField] private GameObject missionStartPanel;
    [SerializeField] private CanvasGroup missionStartCanvasGroup;
    [SerializeField] private TMP_Text missionNumberText;
    [SerializeField] private TMP_Text missionNameText;

    [Header("Mission Completed Panel")]
    [SerializeField] private CanvasGroup missionCompletedCanvasGroup;
    [SerializeField] private TMP_Text completionReputationText;

    [Header("Mission Objectives Panel")]
    [SerializeField] private GameObject missionPanel;
    [SerializeField] private RectTransform missionBackdrop;
    [SerializeField] private GameObject missionTitle;
    [SerializeField] private GameObject missionTitleDivider;
    [SerializeField] private TMP_Text[] objectiveTexts;
    [SerializeField] private TMP_Text chaseObjectiveText;
    [SerializeField] private TMP_Text treasureHuntObjectiveText;

    [Header("Mission Details")]
    [SerializeField] private string missionNumber = "MISSION 1";
    [SerializeField] private string missionName = "A MURDERER'S TRAIL";

    [Header("Fade Timing")]
    [SerializeField, Min(0.01f)] private float fadeInDuration = 0.5f;
    [SerializeField, Min(0f)] private float holdDuration = 2f;
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.5f;

    private readonly string[] objectiveNames =
    {
        "Inspect the Victim",
        "Collect the Bullet Casing",
        "Inspect the Bloodstain",
        "Examine the Phone"
    };

    private readonly string[] objectiveIds =
    {
        "victim",
        "casing",
        "bloodstain",
        "phone"
    };

    private MissionObjectiveRow[] evidenceRows;
    private readonly Vector3[] backdropCorners = new Vector3[4];
    private MissionObjectiveRow sharedObjectiveRow;
    private MissionObjectiveRow treasureHuntRow;
    private bool[] objectivesCompleted;
    private float topRowY;
    private bool showMissionTitle;
    private bool officerObjectiveActive;
    private bool missionStarted;
    private bool investigationCompleted;
    private bool evidenceInstructionActive;
    private bool evidenceDismissalRequested;
    private bool sequencePlaying;
    private Resolution resolution;
    private bool suspectArrested;
    private bool travelObjectiveActive;
    private MissionBanner banner;

    public bool CanInteract => missionStarted && !investigationCompleted;
    internal bool CanTalkToOfficer => !sequencePlaying && (playerHUD == null || playerHUD.IsVisible) &&
        (!investigationCompleted || resolution is Resolution.AwaitingDebrief or Resolution.Debriefed);
    internal bool IsDebriefAvailable => resolution == Resolution.AwaitingDebrief;
    internal bool SuspectArrested => suspectArrested;
    internal bool HasCompletedDebrief => resolution == Resolution.Debriefed;
    public bool HasPendingChaseCompletion => sharedObjectiveRow?.IsDismissing == true;
    public bool HasPendingEvidenceCompletion
    {
        get
        {
            if (!investigationCompleted)
                return false;

            if (evidenceInstructionActive && sharedObjectiveRow?.HasContent == true)
                return true;

            foreach (var row in evidenceRows)
            {
                if (row?.HasContent == true)
                    return true;
            }

            return false;
        }
    }

    public event Action MissionCompleted;

    private void Awake()
    {
        banner = new MissionBanner(playerHUD);
        objectivesCompleted = new bool[objectiveIds.Length];
        evidenceRows = new MissionObjectiveRow[objectiveIds.Length];

        if (DistrictTravelSession.HasVisitedOldTown && DistrictTravelSession.ArrestedFirstSuspect is bool arrested)
        {
            Array.Fill(objectivesCompleted, true);
            missionStarted = true;
            investigationCompleted = true;
            resolution = Resolution.Debriefed;
            suspectArrested = arrested;
        }

        if (missionStartPanel != null)
            missionStartPanel.SetActive(false);
        if (missionCompletedCanvasGroup != null)
            missionCompletedCanvasGroup.gameObject.SetActive(false);

        if (missionNumberText != null)
            missionNumberText.text = missionNumber;
        if (missionNameText != null)
            missionNameText.text = missionName;

        if (missionPanel == null)
            return;

        missionPanel.SetActive(false);
        if (objectiveTexts != null)
        {
            for (int i = 0; i < Mathf.Min(objectiveTexts.Length, evidenceRows.Length); i++)
                evidenceRows[i] = CreateRow(objectiveTexts[i]);
        }
        sharedObjectiveRow = CreateRow(chaseObjectiveText);
        treasureHuntRow = CreateRow(treasureHuntObjectiveText);
        topRowY = (Array.Find(evidenceRows, row => row != null) ?? sharedObjectiveRow)?.ListPositionY ?? 0f;
    }

    private static MissionObjectiveRow CreateRow(TMP_Text label)
    {
        return label != null && label.transform.parent is RectTransform row
            ? new MissionObjectiveRow(label, row)
            : null;
    }

    private void OnEnable()
    {
        if (fellowOfficer != null)
            fellowOfficer.FirstConversationCompleted += StartMissionSequence;
        if (dialogueManager != null)
            dialogueManager.OpeningDialogueEnded += ShowTalkToOfficerObjective;
    }

    private void OnDisable()
    {
        if (fellowOfficer != null)
            fellowOfficer.FirstConversationCompleted -= StartMissionSequence;
        if (dialogueManager != null)
            dialogueManager.OpeningDialogueEnded -= ShowTalkToOfficerObjective;

        if (officerMarker != null)
            officerMarker.SetRequested(false);
        if (grabCarMarker != null)
            grabCarMarker.SetRequested(false);
        StopAllCoroutines();
        banner.Close();
        sequencePlaying = false;
    }

    private void LateUpdate()
    {
        if (missionPanel == null)
            return;

        bool canShow = (playerHUD == null || playerHUD.IsVisible) &&
            (dialogueManager == null || !dialogueManager.IsPlaying) &&
            (missionStartPanel == null || !missionStartPanel.activeInHierarchy) &&
            (missionPanel.transform.parent == null ||
                missionPanel.transform.parent.gameObject.activeInHierarchy);

        if (officerMarker != null)
            officerMarker.SetRequested(canShow && officerObjectiveActive &&
                fellowOfficer != null && fellowOfficer.isActiveAndEnabled);
        if (grabCarMarker != null)
            grabCarMarker.SetRequested(canShow && travelObjectiveActive);

        bool hasRows = false;
        if (canShow)
        {
            float deltaTime = Time.unscaledDeltaTime;
            float nextRowY = topRowY;
            bool sharedRowActive = AdvanceRow(sharedObjectiveRow, deltaTime, ref nextRowY);
            hasRows = sharedRowActive;
            evidenceInstructionActive &= sharedRowActive;
            foreach (var row in evidenceRows)
                hasRows |= AdvanceRow(row, deltaTime, ref nextRowY);

            DismissCompletedEvidence();

            bool showMainTitle = hasRows && showMissionTitle;
            if (missionTitle != null)
                missionTitle.SetActive(showMainTitle);
            if (missionTitleDivider != null)
                missionTitleDivider.SetActive(showMainTitle);

            hasRows |= AdvanceRow(treasureHuntRow, deltaTime, ref nextRowY);
        }

        bool showPanel = canShow && hasRows;
        if (missionPanel.activeSelf != showPanel)
            missionPanel.SetActive(showPanel);
        if (showPanel && missionBackdrop != null)
            FitMissionBackdrop();
    }

    private void FitMissionBackdrop()
    {
        Vector2 min = Vector2.positiveInfinity;
        Vector2 max = Vector2.negativeInfinity;
        Matrix4x4 worldToPanel = missionPanel.transform.worldToLocalMatrix;

        void IncludeCorners()
        {
            foreach (Vector3 corner in backdropCorners)
            {
                Vector2 point = worldToPanel.MultiplyPoint3x4(corner);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
        }

        void IncludeTitlePart(GameObject part)
        {
            if (part != null && part.activeSelf && part.transform is RectTransform rect)
            {
                rect.GetWorldCorners(backdropCorners);
                IncludeCorners();
            }
        }

        void IncludeRow(MissionObjectiveRow row)
        {
            if (row?.HasContent == true)
            {
                row.GetRestingWorldCorners(backdropCorners);
                IncludeCorners();
            }
        }

        IncludeTitlePart(missionTitle);
        IncludeTitlePart(missionTitleDivider);
        IncludeRow(sharedObjectiveRow);
        foreach (MissionObjectiveRow row in evidenceRows)
            IncludeRow(row);
        IncludeRow(treasureHuntRow);

        Vector2 topRight = max + BackdropPadding;
        Vector2 size = topRight - (min - BackdropPadding);
        if (missionBackdrop.anchoredPosition != topRight)
            missionBackdrop.anchoredPosition = topRight;
        if (missionBackdrop.sizeDelta != size)
            missionBackdrop.sizeDelta = size;
    }

    private static bool AdvanceRow(MissionObjectiveRow row, float deltaTime, ref float nextRowY)
    {
        if (row?.HasContent != true)
            return false;

        row.SetListPosition(nextRowY);
        row.Advance(deltaTime);
        if (!row.HasContent)
            return false;

        nextRowY -= row.Height;
        return true;
    }

    internal IEnumerator ShowTreasureHuntCompletion()
    {
        if (treasureHuntRow == null)
            yield break;

        treasureHuntRow.Show(FindTreasures, completed: true);

        float remaining = TreasureHuntCompletionHold;
        while (isActiveAndEnabled && remaining > 0f)
        {
            yield return null;
            if (missionPanel.activeInHierarchy && treasureHuntRow.IsShowingCompletion)
                remaining -= Time.unscaledDeltaTime;
        }

        treasureHuntRow.Dismiss();
        while (isActiveAndEnabled && treasureHuntRow.HasContent)
            yield return null;
    }

    private void DismissCompletedEvidence()
    {
        if (!investigationCompleted || evidenceDismissalRequested)
            return;

        if (evidenceInstructionActive && !sharedObjectiveRow.IsShowingCompletion)
            return;

        // Give every completed item the same time on screen before clearing the checklist.
        foreach (var row in evidenceRows)
        {
            if (row != null && !row.IsShowingCompletion)
                return;
        }

        evidenceDismissalRequested = true;
        if (evidenceInstructionActive)
            sharedObjectiveRow.Dismiss();
        foreach (var row in evidenceRows)
            row?.Dismiss();
    }

    private void ShowTalkToOfficerObjective()
    {
        if (missionStarted || sequencePlaying)
            return;

        officerObjectiveActive = true;
        sharedObjectiveRow?.Show(TalkToOfficer);
    }

    public void StartMissionSequence()
    {
        if (sequencePlaying || missionStarted)
            return;

        officerObjectiveActive = false;
        sequencePlaying = true;
        StartCoroutine(MissionStartRoutine());
    }

    private IEnumerator MissionStartRoutine()
    {
        sharedObjectiveRow?.MarkComplete(TalkToOfficer);
        sharedObjectiveRow?.Dismiss();
        while (sharedObjectiveRow?.IsDismissing == true)
            yield return null;

        if (missionStartPanel != null)
            yield return banner.Play(missionStartPanel, missionStartCanvasGroup, fadeInDuration, holdDuration, fadeOutDuration);

        showMissionTitle = true;
        evidenceInstructionActive = sharedObjectiveRow != null;
        sharedObjectiveRow?.Show(CollectEvidence);
        foreach (var row in evidenceRows)
            row?.Show(HiddenObjective);

        missionStarted = true;
        sequencePlaying = false;
    }

    internal void CompleteMission(bool arrested)
    {
        if (resolution != Resolution.Pending)
            return;

        resolution = Resolution.Completing;
        suspectArrested = arrested;
        StartCoroutine(PlayMissionCompletion());
    }

    private IEnumerator PlayMissionCompletion()
    {
        if (suspectArrested)
            yield return DismissSharedObjective(DefeatSuspect);

        int coins = suspectArrested ? ArrestRewardCoins : ReleaseRewardCoins;
        int reputation = suspectArrested ? ArrestRewardReputation : ReleaseRewardReputation;
        if (missionCompletedCanvasGroup != null)
        {
            if (completionReputationText != null)
                completionReputationText.text = reputation > 0 ? "REPUTATION+" : "REPUTATION-";

            float holdTime = Mathf.Max(0f, CompletionBannerDuration - fadeInDuration - fadeOutDuration);
            yield return banner.Play(missionCompletedCanvasGroup.gameObject, missionCompletedCanvasGroup, fadeInDuration, holdTime, fadeOutDuration);
        }

        if (playerHUD != null)
            playerHUD.AddRewards(coins, reputation);

        showMissionTitle = false;
        officerObjectiveActive = true;
        sharedObjectiveRow?.Show(ReturnToOfficer);
        resolution = Resolution.AwaitingDebrief;
    }

    internal void CompleteDebrief()
    {
        if (!IsDebriefAvailable)
            return;

        resolution = Resolution.Debriefed;
        DistrictTravelSession.ArrestedFirstSuspect = suspectArrested;
        DistrictTravelSession.NextDestination = District.OldTown;
        officerObjectiveActive = false;
        if (playerHUD != null)
            playerHUD.AddRewards(0, suspectArrested ? DebriefReputation : -DebriefReputation);

        StartCoroutine(BeginTravelObjective());
    }

    private IEnumerator BeginTravelObjective()
    {
        yield return DismissSharedObjective(ReturnToOfficer);
        sharedObjectiveRow?.Show(TravelToOldTown);
        travelObjectiveActive = true;
    }

    private IEnumerator DismissSharedObjective(string objective)
    {
        sharedObjectiveRow?.MarkComplete(objective);
        sharedObjectiveRow?.Dismiss();
        while (sharedObjectiveRow?.IsDismissing == true)
            yield return null;
    }

    public void CompleteObjective(string objectiveId)
    {
        if (!CanInteract)
            return;

        int index = Array.IndexOf(objectiveIds, objectiveId);
        if (index < 0)
        {
            Debug.LogWarning("Unknown mission objective ID: " + objectiveId, this);
            return;
        }

        if (objectivesCompleted[index])
            return;

        objectivesCompleted[index] = true;
        evidenceRows[index]?.MarkComplete(objectiveNames[index]);
        Debug.Log("Objective completed: " + objectiveNames[index]);

        foreach (bool completed in objectivesCompleted)
        {
            if (!completed)
                return;
        }

        investigationCompleted = true;
        if (evidenceInstructionActive)
            sharedObjectiveRow.MarkComplete(CollectEvidence);
        MissionCompleted?.Invoke();
    }

    public void ActivateChaseObjective() => sharedObjectiveRow?.Show(ChaseSuspect);
    internal void ActivateDefeatObjective() => sharedObjectiveRow?.Show(DefeatSuspect);

    public void CompleteChaseObjective()
    {
        sharedObjectiveRow?.MarkComplete(ChaseSuspect);
        sharedObjectiveRow?.Dismiss();
    }
}
