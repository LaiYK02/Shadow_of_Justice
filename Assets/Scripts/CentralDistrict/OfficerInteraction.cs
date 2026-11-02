using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class OfficerInteraction : PlayerInteractable
{
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private MissionFlowController missionFlow;
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

    private bool firstConversationCompleted;
    private bool waitingForFirstConversationToEnd;
    public event Action FirstConversationCompleted;

    public override bool CanInteract => dialogueManager != null &&
        !dialogueManager.IsPlaying &&
        (missionFlow == null || !missionFlow.IsMissionStartSequencePlaying);

    protected override void OnEnable()
    {
        base.OnEnable();
        if (dialogueManager != null) dialogueManager.DialogueEnded += HandleDialogueEnded;
    }

    protected override void OnDisable()
    {
        if (dialogueManager != null) dialogueManager.DialogueEnded -= HandleDialogueEnded;
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
        if (firstConversationCompleted)
        {
            dialogueManager.StartDialogue(repeatConversation);
        }
        else
        {
            waitingForFirstConversationToEnd = true;
            dialogueManager.StartDialogue(firstConversation);
        }
    }

    private void HandleDialogueEnded()
    {
        if (!waitingForFirstConversationToEnd) return;
        waitingForFirstConversationToEnd = false;
        firstConversationCompleted = true;
        FirstConversationCompleted?.Invoke();
    }
}
