using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class MissionFlowController : MonoBehaviour
{
    private const string TalkToOfficer = "Talk to the Fellow Officer";
    private const string ChaseSuspect = "Chase and hit the Suspect";
    private const string HiddenObjective = "......";

    [Header("References")]
    [SerializeField] private OfficerInteraction fellowOfficer;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private PlayerHUDController playerHUD;

    [Header("Mission Start Panel")]
    [SerializeField] private GameObject missionStartPanel;
    [SerializeField] private CanvasGroup missionStartCanvasGroup;
    [SerializeField] private TMP_Text missionNumberText;
    [SerializeField] private TMP_Text missionNameText;

    [Header("Mission Objectives Panel")]
    [SerializeField] private GameObject missionPanel;
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
    private MissionObjectiveRow sharedObjectiveRow;
    private bool[] objectivesCompleted;
    private float topRowY;
    private bool showMissionTitle;
    private bool missionStarted;
    private bool investigationCompleted;
    private bool evidenceDismissalRequested;
    private bool sequencePlaying;

    public bool CanInteract => missionStarted && !investigationCompleted;
    public bool IsMissionStartSequencePlaying => sequencePlaying;
    public bool HasPendingChaseCompletion => sharedObjectiveRow?.IsDismissing == true;
    public bool HasPendingEvidenceCompletion
    {
        get
        {
            if (!investigationCompleted)
                return false;

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
            foreach (var row in evidenceRows)
                hasRows |= AdvanceRow(row, deltaTime, ref nextRowY);

            hasRows |= AdvanceRow(sharedObjectiveRow, deltaTime, ref nextRowY);
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

        // Give every completed item the same time on screen before clearing the checklist.
        foreach (var row in evidenceRows)
        {
            if (row != null && !row.IsShowingCompletion)
                return;
        }

        evidenceDismissalRequested = true;
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
        {
            playerHUD?.SetMissionStartActive(true);
            missionStartPanel.SetActive(true);

            if (missionStartCanvasGroup != null)
            {
                missionStartCanvasGroup.alpha = 0f;
                missionStartCanvasGroup.interactable = false;
                missionStartCanvasGroup.blocksRaycasts = false;

                yield return Fade(0f, 1f, fadeInDuration);
                yield return new WaitForSecondsRealtime(holdDuration);
                yield return Fade(1f, 0f, fadeOutDuration);
            }
            else
            {
                Debug.LogWarning("MissionFlowController: Assign the Mission Start CanvasGroup.", this);
                yield return new WaitForSecondsRealtime(fadeInDuration + holdDuration + fadeOutDuration);
            }

            missionStartPanel.SetActive(false);
            playerHUD?.SetMissionStartActive(false);
        }

        showMissionTitle = true;
        foreach (var row in evidenceRows)
            row?.Show(HiddenObjective);

        missionStarted = true;
        sequencePlaying = false;
    }

    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            missionStartCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }

        missionStartCanvasGroup.alpha = endAlpha;
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
        MissionCompleted?.Invoke();
    }

    public void ActivateChaseObjective() => sharedObjectiveRow?.Show(ChaseSuspect);

    public void CompleteChaseObjective()
    {
        sharedObjectiveRow?.MarkComplete(ChaseSuspect);
        sharedObjectiveRow?.Dismiss();
    }
}
