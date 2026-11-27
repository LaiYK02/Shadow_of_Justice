using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MiraInteraction : PlayerInteractable
{
    private const string TalkToMira = "Talk to Mira outside the print shop";
    private const string CollectEvidence = "Collect the evidence";

    [SerializeField] private DialogueManager dialogue;
    [SerializeField] private CharacterCombat speaker;
    [SerializeField] private PlayerHUDController hud;
    [SerializeField] private MissionTargetMarker marker;
    [SerializeField] private RectTransform missionPanel;
    [SerializeField] private GameObject missionTitle;
    [SerializeField] private TMP_Text objectiveLabel;
    [SerializeField] private CanvasGroup missionStartPanel;
    [SerializeField, Min(0f)] private float fadeIn = 0.5f;
    [SerializeField, Min(0f)] private float hold = 2f;
    [SerializeField, Min(0f)] private float fadeOut = 0.5f;
    [SerializeField] private DialogueLine[] briefing;

    private MissionObjectiveRow objective;
    private MissionBanner banner;
    private bool briefingPlaying;
    private bool revealingMission;

    public override bool CanInteract => !briefingPlaying && !revealingMission &&
        !DistrictTravelSession.HasStartedMissionTwo && !dialogue.IsPlaying && hud.IsVisible;

    protected override void Awake()
    {
        base.Awake();
        banner = new MissionBanner(hud);
        missionStartPanel.gameObject.SetActive(false);
        missionPanel.gameObject.SetActive(false);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        dialogue.DialogueEnded += EndBriefing;
        objective = new MissionObjectiveRow(objectiveLabel, (RectTransform)objectiveLabel.transform.parent);
        ShowObjective(DistrictTravelSession.HasStartedMissionTwo);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (dialogue != null)
            dialogue.DialogueEnded -= EndBriefing;
        StopAllCoroutines();
        banner?.Close();
        if (marker != null)
            marker.SetRequested(false);
        if (missionPanel != null)
            missionPanel.gameObject.SetActive(false);
        briefingPlaying = false;
        revealingMission = false;
    }

    private void LateUpdate()
    {
        bool visible = hud.IsVisible && !dialogue.IsPlaying && objective.HasContent;
        missionPanel.gameObject.SetActive(visible);
        marker.SetRequested(CanInteract);
        if (visible)
            objective.Advance(Time.unscaledDeltaTime);
    }

    public override void Interact()
    {
        if (!CanInteract)
            return;

        dialogue.StartDialogue(briefing, speaker);
        briefingPlaying = dialogue.IsPlaying;
    }

    private void EndBriefing()
    {
        if (!briefingPlaying)
            return;

        briefingPlaying = false;
        revealingMission = true;
        StartCoroutine(RevealMission());
    }

    private IEnumerator RevealMission()
    {
        objective.MarkComplete(TalkToMira);
        objective.Dismiss();
        while (objective.IsDismissing)
            yield return null;

        yield return banner.Play(missionStartPanel.gameObject, missionStartPanel, fadeIn, hold, fadeOut);
        DistrictTravelSession.HasStartedMissionTwo = true;
        ShowObjective(true);
        revealingMission = false;
    }

    private void ShowObjective(bool missionStarted)
    {
        missionTitle.SetActive(missionStarted);
        missionPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, missionStarted ? 125f : 70f);
        objective.Show(missionStarted ? CollectEvidence : TalkToMira);
    }
}
