using System.Collections;
using Unity.Cinemachine;
using TMPro;
using UnityEngine;

/// <summary>Plays the first mission's suspect reveal and escape sequence.</summary>
public class MissionEndingSequence : MonoBehaviour
{
    [Header("Sequence References")]
    [SerializeField] private MissionFlowController missionFlow;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private TutorialPanelController tutorialPanelController;
    [SerializeField] private PlayerHUDController playerHUD;
    [SerializeField] private CinemachineBrain cameraBrain;
    [SerializeField] private CinemachineCamera playerFollowCamera;
    [SerializeField] private CinemachineCamera suspectStaticCamera;
    [SerializeField] private Transform suspectToMove;
    [SerializeField] private SuspectEscapeTarget suspectEscapeTarget;
    [SerializeField] private Transform escapeDestination;
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private TMP_Text arrestChoiceText;
    [SerializeField] private TMP_Text letGoChoiceText;
    [SerializeField] private string arrestChoiceLabel = "Arrest him";
    [SerializeField] private string letGoChoiceLabel = "Let him go";

    [Header("Player Controls")]
    [Tooltip("Assign PlayerMovement and ThirdPersonCameraLook so controls stay locked for the full cutscene.")]
    [SerializeField] private Behaviour[] playerControlsToLock;

    [Header("Timing and Movement")]
    [SerializeField, Min(0f)] private float delayAfterMission = 1f;
    [SerializeField, Min(0.1f)] private float cameraBlendDuration = 1.5f;
    [Tooltip("Movement speed in Unity world units per second.")]
    [SerializeField, Min(0.1f)] private float suspectRunSpeed = 3f;
    [SerializeField, Min(0f)] private float escapeCameraHoldDuration = 2f;
    [SerializeField, Min(0.1f)] private float turnSpeed = 540f;
    [SerializeField, Min(0.001f)] private float arrivalDistance = 0.05f;

    [Header("Suspect Dialogue")]
    [SerializeField] private DialogueLine[] suspectDialogue =
    {
        new DialogueLine
        {
            speakerName = "Suspicious Person",
            dialogueText = "Oh no! He discovered the evidence of my crime!"
        },
        new DialogueLine
        {
            speakerName = "Suspicious Person",
            dialogueText = "I have to run... now!"
        }
    };

    [SerializeField] private DialogueLine[] caughtSuspectDialogue =
    {
        new DialogueLine
        {
            speakerName = "Suspicious Person",
            dialogueText = "Please, let me go. I can pay you well if you look the other way."
        },
        new DialogueLine
        {
            speakerName = "Suspicious Person",
            dialogueText = "Take the money and forget you ever saw me."
        }
    };

    private bool sequenceStarted;
    private CinemachineBlendDefinition originalBlend;
    private int originalPlayerCameraPriority;
    private int originalSuspectCameraPriority;
    private bool[] controlsWereEnabled;

    private void Awake()
    {
        if (choicePanel != null)
            choicePanel.SetActive(false);

        if (cameraBrain != null)
            originalBlend = cameraBrain.DefaultBlend;

        if (playerFollowCamera != null)
            originalPlayerCameraPriority = playerFollowCamera.Priority;

        if (suspectStaticCamera != null)
            originalSuspectCameraPriority = suspectStaticCamera.Priority;

        if (playerControlsToLock != null)
        {
            controlsWereEnabled = new bool[playerControlsToLock.Length];
            for (int i = 0; i < playerControlsToLock.Length; i++)
                controlsWereEnabled[i] = playerControlsToLock[i] != null && playerControlsToLock[i].enabled;
        }
    }

    private void OnEnable()
    {
        if (missionFlow != null)
            missionFlow.MissionCompleted += HandleMissionCompleted;
    }

    private void OnDisable()
    {
        if (missionFlow != null)
            missionFlow.MissionCompleted -= HandleMissionCompleted;

        RestorePlayerControls();
        RestoreCameras();
    }

    private void HandleMissionCompleted()
    {
        if (sequenceStarted)
            return;

        if (!ReferencesAreAssigned())
            return;

        sequenceStarted = true;
        playerHUD?.SetCutsceneActive(true);
        StartCoroutine(PlaySequence());
    }

    private bool ReferencesAreAssigned()
    {
        bool valid = missionFlow != null && dialogueManager != null &&
                     cameraBrain != null && playerFollowCamera != null &&
                     suspectStaticCamera != null && suspectToMove != null &&
                     suspectEscapeTarget != null && escapeDestination != null &&
                     choicePanel != null && arrestChoiceText != null &&
                     letGoChoiceText != null;

        if (!valid)
            Debug.LogError("MissionEndingSequence: Assign all sequence references in the Inspector.", this);

        return valid;
    }

    private IEnumerator PlaySequence()
    {
        if (delayAfterMission > 0f)
            yield return new WaitForSecondsRealtime(delayAfterMission);

        LockPlayerControls();

        cameraBrain.DefaultBlend = new CinemachineBlendDefinition(
            CinemachineBlendDefinition.Styles.EaseInOut,
            cameraBlendDuration);

        playerFollowCamera.Priority = 20;
        suspectStaticCamera.Priority = 30;

        // Let Cinemachine finish the move before showing the first dialogue line.
        yield return new WaitForSecondsRealtime(cameraBlendDuration);

        missionFlow.HideEvidenceObjectives();
        dialogueManager.StartDialogue(suspectDialogue);
        while (dialogueManager.IsPlaying)
            yield return null;

        missionFlow.ActivateChaseObjective();

        // Start the escape, then return to the normal camera after the requested
        // two seconds while the suspect continues running to the marker.
        suspectEscapeTarget.BeginEscape();
        StartCoroutine(MoveSuspectToEscapePoint());
        StartCoroutine(WaitForSuspectToBeCaught());
        if (escapeCameraHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(escapeCameraHoldDuration);

        suspectStaticCamera.Priority = originalSuspectCameraPriority;
        playerFollowCamera.Priority = Mathf.Max(20, originalPlayerCameraPriority);

        yield return new WaitForSecondsRealtime(cameraBlendDuration);

        cameraBrain.DefaultBlend = originalBlend;
        RestorePlayerControls();
        playerHUD?.SetCutsceneActive(false);
        tutorialPanelController?.ShowChaseTutorial();
    }

    private IEnumerator WaitForSuspectToBeCaught()
    {
        while (!suspectEscapeTarget.IsStopped)
            yield return null;

        LockPlayerControls();
        dialogueManager.StartDialogue(caughtSuspectDialogue);
        while (dialogueManager.IsPlaying)
            yield return null;

        ShowChoiceButtons();
    }

    private void ShowChoiceButtons()
    {
        if (choicePanel == null || arrestChoiceText == null || letGoChoiceText == null)
            return;

        arrestChoiceText.text = arrestChoiceLabel;
        letGoChoiceText.text = letGoChoiceLabel;
        missionFlow.HideMissionPanel();
        playerHUD?.SetChoiceActive(true);
        choicePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private IEnumerator MoveSuspectToEscapePoint()
    {
        Vector3 destination = escapeDestination.position;
        // Treat the marker as a horizontal destination so the suspect keeps its ground height.
        destination.y = suspectToMove.position.y;

        while (!suspectEscapeTarget.IsStopped &&
               Vector3.Distance(suspectToMove.position, destination) > arrivalDistance)
        {
            Vector3 toDestination = destination - suspectToMove.position;
            Vector3 flatDirection = Vector3.ProjectOnPlane(toDestination, Vector3.up);

            if (flatDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion facing = Quaternion.LookRotation(flatDirection.normalized);
                suspectToMove.rotation = Quaternion.RotateTowards(
                    suspectToMove.rotation,
                    facing,
                    turnSpeed * Time.deltaTime);
            }

            suspectToMove.position = Vector3.MoveTowards(
                suspectToMove.position,
                destination,
                suspectRunSpeed * Time.deltaTime);

            yield return null;
        }

        if (!suspectEscapeTarget.IsStopped)
            suspectToMove.position = destination;
    }

    private void LockPlayerControls()
    {
        if (playerControlsToLock == null)
            return;

        for (int i = 0; i < playerControlsToLock.Length; i++)
        {
            if (playerControlsToLock[i] != null)
                playerControlsToLock[i].enabled = false;
        }
    }

    private void RestorePlayerControls()
    {
        if (playerControlsToLock == null || controlsWereEnabled == null)
            return;

        for (int i = 0; i < playerControlsToLock.Length; i++)
        {
            if (playerControlsToLock[i] != null)
                playerControlsToLock[i].enabled = controlsWereEnabled[i];
        }
    }

    private void RestoreCameras()
    {
        if (suspectStaticCamera != null)
            suspectStaticCamera.Priority = originalSuspectCameraPriority;

        if (playerFollowCamera != null)
            playerFollowCamera.Priority = originalPlayerCameraPriority;

        if (cameraBrain != null)
            cameraBrain.DefaultBlend = originalBlend;
    }
}
