using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class MissionFlowController : MonoBehaviour
{
    private const string TalkToOfficer = "Talk to the Fellow Officer";
    private const string CollectEvidence = "Collect the evidence";
    private const string ChaseSuspect = "Chase and hit the Suspect";
    private const string HiddenObjective = "......";
    private const float BannerTimeMultiplier = 0.5f;
    private const float CompletionBannerDuration = 3f;
    private const int ArrestRewardCoins = 200;
    private const int ArrestRewardReputation = 10;
    private const int ReleaseRewardCoins = 100;
    private const int ReleaseRewardReputation = -10;
    private static readonly Vector2 BackdropPadding = new(16f, 12f);

    [Header("References")]
    [SerializeField] private OfficerInteraction fellowOfficer;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private PlayerHUDController playerHUD;

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
    private bool[] objectivesCompleted;
    private float topRowY;
    private bool showMissionTitle;
    private bool missionStarted;
    private bool investigationCompleted;
    private bool evidenceInstructionActive;
    private bool evidenceDismissalRequested;
    private bool sequencePlaying;
    private bool missionResolved;
    private GameObject activeBanner;
    private float? timeScaleBeforeBanner;
    private float fixedDeltaTimeBeforeBanner;

    public bool CanInteract => missionStarted && !investigationCompleted;
    public bool IsMissionStartSequencePlaying => sequencePlaying;
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
        objectivesCompleted = new bool[objectiveIds.Length];
        evidenceRows = new MissionObjectiveRow[objectiveIds.Length];

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

        StopAllCoroutines();
        CloseBanner();
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

            if (hasRows)
            {
                if (missionTitle != null)
                    missionTitle.SetActive(showMissionTitle);
                if (missionTitleDivider != null)
                    missionTitleDivider.SetActive(showMissionTitle);
            }
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
        if (!missionStarted && !sequencePlaying)
            sharedObjectiveRow?.Show(TalkToOfficer);
    }

    public void StartMissionSequence()
    {
        if (sequencePlaying || missionStarted)
            return;

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
            yield return PlayBanner(missionStartPanel, missionStartCanvasGroup, holdDuration);

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
        if (missionResolved)
            return;

        missionResolved = true;
        int coins = arrested ? ArrestRewardCoins : ReleaseRewardCoins;
        int reputation = arrested ? ArrestRewardReputation : ReleaseRewardReputation;
        StartCoroutine(PlayMissionCompletion(coins, reputation));
    }

    private IEnumerator PlayMissionCompletion(int coins, int reputation)
    {
        if (missionCompletedCanvasGroup != null)
        {
            if (completionReputationText != null)
                completionReputationText.text = reputation > 0 ? "REPUTATION+" : "REPUTATION-";

            float holdTime = Mathf.Max(0f, CompletionBannerDuration - fadeInDuration - fadeOutDuration);
            yield return StartCoroutine(PlayBanner(missionCompletedCanvasGroup.gameObject, missionCompletedCanvasGroup, holdTime));
        }

        if (playerHUD != null)
            playerHUD.AddRewards(coins, reputation);
    }

    private IEnumerator PlayBanner(GameObject panel, CanvasGroup fade, float holdTime)
    {
        activeBanner = panel;
        timeScaleBeforeBanner = Time.timeScale;
        fixedDeltaTimeBeforeBanner = Time.fixedDeltaTime;
        Time.timeScale *= BannerTimeMultiplier;
        Time.fixedDeltaTime *= BannerTimeMultiplier;
        if (playerHUD != null)
            playerHUD.SetMissionBannerActive(true);

        if (fade != null)
        {
            fade.alpha = 0f;
            fade.interactable = false;
            fade.blocksRaycasts = false;
            panel.SetActive(true);

            yield return Fade(fade, 0f, 1f, fadeInDuration);
            yield return new WaitForSecondsRealtime(holdTime);
            yield return Fade(fade, 1f, 0f, fadeOutDuration);
        }
        else
        {
            panel.SetActive(true);
            yield return new WaitForSecondsRealtime(fadeInDuration + holdTime + fadeOutDuration);
        }

        CloseBanner();
    }

    private void CloseBanner()
    {
        if (!timeScaleBeforeBanner.HasValue)
            return;

        if (activeBanner != null)
            activeBanner.SetActive(false);
        activeBanner = null;
        Time.timeScale = timeScaleBeforeBanner.Value;
        Time.fixedDeltaTime = fixedDeltaTimeBeforeBanner;
        timeScaleBeforeBanner = null;
        if (playerHUD != null)
            playerHUD.SetMissionBannerActive(false);
    }

    private static IEnumerator Fade(CanvasGroup fade, float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fade.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }

        fade.alpha = endAlpha;
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

    public void CompleteChaseObjective()
    {
        sharedObjectiveRow?.MarkComplete(ChaseSuspect);
        sharedObjectiveRow?.Dismiss();
    }
}
