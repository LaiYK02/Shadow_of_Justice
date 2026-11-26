using System;
using Unity.Cinemachine;
using UnityEngine;

public class OfficerInteraction : PlayerInteractable
{
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private MissionFlowController missionFlow;
    [SerializeField] private CinemachineBrain cameraBrain;
    [SerializeField] private CinemachineCamera grabCarCamera;
    [SerializeField]
    private DialogueLine[] firstConversation =
    {
        new DialogueLine { speakerName = "Fellow Officer", dialogueText = "Welcome to the district. Since this is your first day, I'll brief you on your first assignment." },
        new DialogueLine { speakerName = "Fellow Officer", dialogueText = "Your task is to begin investigating a report and gather the facts before drawing conclusions. Follow the evidence and remember that the law applies to everyone." },
        new DialogueLine { speakerName = "Fellow Officer", dialogueText = "When you're ready, start by checking the nearby area. I'll be here if you need a reminder." }
    };
    [SerializeField]
    private DialogueLine[] repeatConversation =
    {
        new DialogueLine { speakerName = "Fellow Officer", dialogueText = "To recap: investigate the report, check the nearby area, and gather evidence before making any conclusions. Follow the law and let the facts guide you." }
    };

    private static readonly DialogueLine OldTownAssignment = OfficerLine(
        "Old Town has a difficult case on its hands. They need an officer to help with the investigation.");
    private static readonly DialogueLine GrabCarDirections = OfficerLine(
        "See that Grab Car Box? Use it to travel to Old Town.");
    private static readonly DialogueLine GoodLuck = OfficerLine(
        "Report in when you arrive. Good luck, officer.");

    private static readonly DialogueLine[] ArrestDebrief =
    {
        OfficerLine("You followed the evidence and stopped the suspect. Good work out there, officer."),
        OldTownAssignment,
        GrabCarDirections,
        GoodLuck
    };

    private static readonly DialogueLine[] ReleaseDebrief =
    {
        OfficerLine("You had enough evidence to bring the suspect in, and you let him walk."),
        OfficerLine("We don't get to bend the law for a payout. Next time, follow the evidence and do your duty."),
        OldTownAssignment,
        GrabCarDirections,
        GoodLuck
    };

    private static readonly DialogueLine[] OldTownReminder =
    {
        OfficerLine("Old Town is waiting for you. Use the Grab Car Box to get there and report in when you arrive.")
    };

    private bool firstConversationCompleted;
    private bool waitingForFirstConversationToEnd;
    private bool waitingForDebriefToEnd;
    private int cameraOverride = -1;
    public event Action FirstConversationCompleted;

    public override bool CanInteract => dialogueManager != null &&
        !dialogueManager.IsPlaying &&
        (missionFlow == null || missionFlow.CanTalkToOfficer);

    private static DialogueLine OfficerLine(string text) =>
        new() { speakerName = "Fellow Officer", dialogueText = text };

    protected override void OnEnable()
    {
        base.OnEnable();
        if (dialogueManager != null)
        {
            dialogueManager.DialogueEnded += HandleDialogueEnded;
            dialogueManager.DialogueLineShown += HandleDialogueLineShown;
        }
    }

    protected override void OnDisable()
    {
        if (dialogueManager != null)
        {
            dialogueManager.DialogueEnded -= HandleDialogueEnded;
            dialogueManager.DialogueLineShown -= HandleDialogueLineShown;
        }

        waitingForDebriefToEnd = false;
        EndGrabCarShot();
        base.OnDisable();
    }

    protected override void Start()
    {
        base.Start();
        if (dialogueManager == null) Debug.LogError("OfficerInteraction: Assign DialogueManager.", this);
        if (missionFlow == null) Debug.LogError("OfficerInteraction: Assign MissionFlowController.", this);
    }

    public override void Interact()
    {
        if (!CanInteract) return;
        CharacterCombat speaker = GetComponent<CharacterCombat>();
        if (missionFlow != null && missionFlow.IsDebriefAvailable)
        {
            waitingForDebriefToEnd = true;
            dialogueManager.StartDialogue(missionFlow.SuspectArrested ? ArrestDebrief : ReleaseDebrief, speaker);
            waitingForDebriefToEnd = dialogueManager.IsPlaying;
        }
        else if (missionFlow != null && missionFlow.HasCompletedDebrief)
        {
            dialogueManager.StartDialogue(OldTownReminder, speaker);
        }
        else if (firstConversationCompleted)
        {
            dialogueManager.StartDialogue(repeatConversation, speaker);
        }
        else
        {
            waitingForFirstConversationToEnd = true;
            dialogueManager.StartDialogue(firstConversation, speaker);
        }
    }

    private void HandleDialogueEnded()
    {
        if (waitingForDebriefToEnd)
        {
            waitingForDebriefToEnd = false;
            EndGrabCarShot();
            missionFlow.CompleteDebrief();
            return;
        }

        if (!waitingForFirstConversationToEnd) return;
        waitingForFirstConversationToEnd = false;
        firstConversationCompleted = true;
        FirstConversationCompleted?.Invoke();
    }

    private void HandleDialogueLineShown(DialogueLine line)
    {
        if (waitingForDebriefToEnd && line == GrabCarDirections && cameraBrain != null && grabCarCamera != null)
        {
            grabCarCamera.gameObject.SetActive(true);
            // Cut directly to the box across the district without moving through the buildings.
            cameraOverride = cameraBrain.SetCameraOverride(cameraOverride, 0, null, grabCarCamera, 1f, -1f);
        }
        else
        {
            EndGrabCarShot();
        }
    }

    private void EndGrabCarShot()
    {
        if (cameraOverride < 0)
            return;

        if (cameraBrain != null)
            cameraBrain.ReleaseCameraOverride(cameraOverride);
        if (grabCarCamera != null)
            grabCarCamera.gameObject.SetActive(false);
        cameraOverride = -1;
    }
}
