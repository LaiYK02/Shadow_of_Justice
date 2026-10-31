
using System.Collections;
using TMPro;
using UnityEngine;

public class MissionFlowController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OfficerInteraction fellowOfficer;

    [Header("Mission Start Panel")]
    [SerializeField] private GameObject missionStartPanel;
    [SerializeField] private CanvasGroup missionStartCanvasGroup;
    [SerializeField] private TMP_Text missionNumberText;
    [SerializeField] private TMP_Text missionNameText;

    [Header("Mission Objectives Panel")]
    [SerializeField] private GameObject missionPanel;
    [SerializeField] private TMP_Text[] objectiveTexts;

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

        RefreshObjectiveTexts();
    }

    private void OnEnable()
    {
        if (fellowOfficer != null)
            fellowOfficer.FirstConversationCompleted += StartMissionSequence;
    }

    private void OnDisable()
    {
        if (fellowOfficer != null)
            fellowOfficer.FirstConversationCompleted -= StartMissionSequence;
    }

    public void StartMissionSequence()
    {
        if (sequencePlaying || missionStarted)
            return;

        StartCoroutine(MissionStartRoutine());
    }

    private IEnumerator MissionStartRoutine()
    {
        sequencePlaying = true;

        if (missionPanel != null)
            missionPanel.SetActive(false);

        if (missionStartPanel != null)
        {
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
        }

        if (missionPanel != null)
            missionPanel.SetActive(true);

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

            string prefix = objectivesCompleted != null &&
                            objectivesCompleted[i]
                ? "[X] "
                : "[ ] ";

            objectiveTexts[i].text = prefix + objectiveNames[i];
        }
    }
}
