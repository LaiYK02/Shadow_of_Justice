
using System.Collections;
using System;
using TMPro;
using UnityEngine;

public class MissionFlowController : MonoBehaviour
{
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

    private bool[] objectivesCompleted;
    private bool missionStarted;
    private bool missionCompleted;
    private bool sequencePlaying;

    public bool CanInteract => missionStarted && !missionCompleted;
    public bool IsMissionStartSequencePlaying => sequencePlaying;
    public event Action MissionCompleted;

    private void Awake()
    {
        objectivesCompleted = new bool[objectiveIds.Length];

        if (missionStartPanel != null)
            missionStartPanel.SetActive(false);

        if (missionPanel != null)
            missionPanel.SetActive(false);

        if (missionNumberText != null)
            missionNumberText.text = missionNumber;

        if (missionNameText != null)
            missionNameText.text = missionName;

        if (chaseObjectiveText != null)
        {
            chaseObjectiveText.text = "......";
            chaseObjectiveText.fontStyle &= ~FontStyles.Strikethrough;
        }

        RefreshObjectiveTexts();
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

    private void ShowTalkToOfficerObjective()
    {
        if (missionStarted || sequencePlaying || missionPanel == null || chaseObjectiveText == null)
            return;

        HideEvidenceObjectives();
        if (missionTitle != null)
            missionTitle.SetActive(false);
        if (missionTitleDivider != null)
            missionTitleDivider.SetActive(false);
        SetChaseObjectiveRowPositionAtTop();
        chaseObjectiveText.text = "Talk to the Fellow Officer";
        chaseObjectiveText.fontStyle &= ~FontStyles.Strikethrough;
        chaseObjectiveText.transform.parent.gameObject.SetActive(true);
        missionPanel.SetActive(true);
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
        if (missionPanel != null)
            missionPanel.SetActive(false);

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
                Debug.LogWarning(
                    "MissionFlowController: Assign the Mission Start CanvasGroup.",
                    this);

                yield return new WaitForSecondsRealtime(
                    fadeInDuration + holdDuration + fadeOutDuration);
            }

            missionStartPanel.SetActive(false);
            playerHUD?.SetMissionStartActive(false);
        }

        if (missionPanel != null)
            missionPanel.SetActive(true);

        if (missionTitle != null)
            missionTitle.SetActive(true);
        if (missionTitleDivider != null)
            missionTitleDivider.SetActive(true);
        ShowEvidenceObjectives();

        missionStarted = true;
        sequencePlaying = false;
    }

    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        if (missionStartCanvasGroup == null)
            yield break;

        if (duration <= 0f)
        {
            missionStartCanvasGroup.alpha = endAlpha;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            missionStartCanvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                endAlpha,
                Mathf.Clamp01(elapsed / duration));

            yield return null;
        }

        missionStartCanvasGroup.alpha = endAlpha;
    }

    public void CompleteObjective(string objectiveId)
    {
        if (!CanInteract)
            return;

        int index = System.Array.IndexOf(objectiveIds, objectiveId);

        if (index < 0)
        {
            Debug.LogWarning("Unknown mission objective ID: " + objectiveId);
            return;
        }

        if (objectivesCompleted[index])
            return;

        objectivesCompleted[index] = true;
        RefreshObjectiveTexts();

        Debug.Log("Objective completed: " + objectiveNames[index]);

        bool allCompleted = true;

        foreach (bool completed in objectivesCompleted)
        {
            if (!completed)
            {
                allCompleted = false;
                break;
            }
        }

        if (allCompleted)
        {
            missionCompleted = true;
            Debug.Log("Mission 1 completed!");
            MissionCompleted?.Invoke();
        }
    }

    public void ActivateChaseObjective()
    {
        if (chaseObjectiveText == null)
        {
            Debug.LogWarning("MissionFlowController: Assign the chase objective text.", this);
            return;
        }

        Transform chaseRow = chaseObjectiveText.transform.parent;
        if (chaseRow != null)
        {
            chaseRow.gameObject.SetActive(true);
            SetChaseObjectiveRowPositionAtTop();
        }

        chaseObjectiveText.text = "Chase and hit the Suspect";
        chaseObjectiveText.fontStyle &= ~FontStyles.Strikethrough;
    }

    public void HideEvidenceObjectives()
    {
        if (objectiveTexts == null)
            return;

        foreach (TMP_Text objectiveText in objectiveTexts)
        {
            if (objectiveText != null && objectiveText.transform.parent != null)
                objectiveText.transform.parent.gameObject.SetActive(false);
        }
    }

    public void HideMissionPanel()
    {
        if (missionPanel != null)
            missionPanel.SetActive(false);
    }

    private void ShowEvidenceObjectives()
    {
        if (objectiveTexts != null)
        {
            foreach (TMP_Text objectiveText in objectiveTexts)
            {
                if (objectiveText != null && objectiveText.transform.parent != null)
                    objectiveText.transform.parent.gameObject.SetActive(true);
            }
        }

        if (chaseObjectiveText != null && chaseObjectiveText.transform.parent != null)
        {
            chaseObjectiveText.text = "......";
            chaseObjectiveText.transform.parent.gameObject.SetActive(false);
        }
    }

    private void SetChaseObjectiveRowPositionAtTop()
    {
        if (chaseObjectiveText == null || objectiveTexts == null || objectiveTexts.Length == 0 ||
            objectiveTexts[0] == null)
            return;

        if (chaseObjectiveText.transform.parent is RectTransform chaseRect &&
            objectiveTexts[0].transform.parent is RectTransform firstRowRect)
        {
            chaseRect.anchoredPosition = firstRowRect.anchoredPosition;
        }
    }

    private void RefreshObjectiveTexts()
    {
        if (objectiveTexts == null)
            return;

        for (int i = 0; i < objectiveTexts.Length; i++)
        {
            if (objectiveTexts[i] == null)
                continue;

            if (i >= objectiveNames.Length)
                continue;

            bool completed = objectivesCompleted != null &&
                             objectivesCompleted[i];

            // Preserve the label's other styles (such as bold), but make
            // strikethrough depend only on whether this objective is complete.
            FontStyles style = objectiveTexts[i].fontStyle;
            style &= ~FontStyles.Strikethrough;
            if (completed)
                style |= FontStyles.Strikethrough;

            objectiveTexts[i].fontStyle = style;
            objectiveTexts[i].text = completed ? objectiveNames[i] : "......";
        }
    }
}
