using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[Serializable]
public class DialogueLine
{
    public string speakerName;

    [TextArea(2, 5)]
    public string dialogueText;

    public Sprite portrait;
    public CharacterCombat speaker;
}

public class DialogueManager : MonoBehaviour
{
    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private GameObject missionPanel;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text spacePromptText;

    [Header("Opening Dialogue")]
    [SerializeField] private DialogueLine[] openingDialogue;
    [SerializeField] private CharacterCombat openingSpeaker;

    [Header("Dialogue Facing")]
    [SerializeField] private CharacterCombat player;
    [SerializeField, Min(1f)] private float dialogueTurnSpeed = 540f;

    private readonly Dictionary<Transform, Quaternion> originalModelRotations = new();
    private DialogueLine[] activeDialogue;
    private CharacterCombat defaultSpeaker;
    private CharacterCombat activeSpeaker;
    private CharacterCombat dialoguePartner;
    private int currentLine;
    private bool isPlaying;
    private bool isOpeningDialogue;
    private CursorLockMode cursorLockStateBeforeDialogue;
    private bool cursorVisibleBeforeDialogue;
    private bool cursorStateSaved;
    private bool missionPanelWasActive;
    private bool missionPanelStateSaved;

    public bool IsPlaying => isPlaying;

    public event Action DialogueStarted;
    public event Action DialogueEnded;
    public event Action OpeningDialogueEnded;

    private void Awake()
    {
        if (spacePromptText == null && dialoguePanel != null)
            spacePromptText = CreateSpacePrompt();

        if (spacePromptText != null)
        {
            spacePromptText.text = "PRESS SPACE TO CONTINUE";
            spacePromptText.gameObject.SetActive(false);
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private TMP_Text CreateSpacePrompt()
    {
        GameObject promptObject = new GameObject(
            "SpacePrompt",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        promptObject.transform.SetParent(dialoguePanel.transform, false);

        RectTransform rect = promptObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 16f);
        rect.sizeDelta = new Vector2(-32f, 40f);

        TextMeshProUGUI prompt = promptObject.GetComponent<TextMeshProUGUI>();
        if (dialogueText != null)
            prompt.font = dialogueText.font;
        prompt.fontSize = 22f;
        prompt.fontStyle = FontStyles.Bold;
        prompt.alignment = TextAlignmentOptions.Center;
        prompt.color = dialogueText != null ? dialogueText.color : Color.white;
        prompt.raycastTarget = false;
        prompt.text = "PRESS SPACE TO SKIP";
        promptObject.transform.SetAsLastSibling();
        return prompt;
    }

    private void Update()
    {
        if (isPlaying && SpacePressed())
            AdvanceDialogue();
    }

    private void LateUpdate()
    {
        if (!isPlaying || player == null || dialoguePartner == null)
            return;

        Transform playerModel = player.ModelTransform;
        Transform partnerModel = dialoguePartner.ModelTransform;
        if (playerModel == null || partnerModel == null || playerModel == partnerModel)
            return;

        Vector3 direction = Vector3.ProjectOnPlane(partnerModel.position - playerModel.position, Vector3.up);
        if (direction.sqrMagnitude < 0.0001f)
            return;

        TurnModel(playerModel, direction);
        TurnModel(partnerModel, -direction);
    }

    private void TurnModel(Transform model, Vector3 direction)
    {
        originalModelRotations.TryAdd(model, model.localRotation);
        Quaternion facing = Quaternion.LookRotation(direction, Vector3.up);
        model.rotation = Quaternion.RotateTowards(model.rotation, facing,
            dialogueTurnSpeed * Time.unscaledDeltaTime);
    }

    private void RestoreModelRotations()
    {
        foreach (var entry in originalModelRotations)
        {
            if (entry.Key != null)
                entry.Key.localRotation = entry.Value;
        }

        originalModelRotations.Clear();
    }

    public void StartOpeningDialogue()
    {
        StartDialogueInternal(openingDialogue, true, openingSpeaker);
    }

    public void StartDialogue(DialogueLine[] lines)
    {
        StartDialogue(lines, null);
    }

    public void StartDialogue(DialogueLine[] lines, CharacterCombat speaker)
    {
        StartDialogueInternal(lines, false, speaker);
    }

    private void StartDialogueInternal(DialogueLine[] lines, bool opening, CharacterCombat speaker)
    {
        if (isPlaying)
            return;

        if (dialoguePanel == null ||
            speakerNameText == null ||
            dialogueText == null)
        {
            Debug.LogError(
                "DialogueManager: Assign the dialogue panel, speaker name, and dialogue text."
            );
            return;
        }

        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("DialogueManager: No dialogue lines were provided.");
            return;
        }

        activeDialogue = lines;
        defaultSpeaker = speaker;
        dialoguePartner = speaker != player ? speaker : null;
        currentLine = 0;
        isPlaying = true;
        isOpeningDialogue = opening;

        cursorLockStateBeforeDialogue = Cursor.lockState;
        cursorVisibleBeforeDialogue = Cursor.visible;
        cursorStateSaved = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (missionPanel != null)
        {
            missionPanelWasActive = missionPanel.activeSelf;
            missionPanelStateSaved = true;
            missionPanel.SetActive(false);
        }

        dialoguePanel.SetActive(true);
        if (spacePromptText != null)
            spacePromptText.gameObject.SetActive(true);
        ShowCurrentLine();
        DialogueStarted?.Invoke();
    }

    public void AdvanceDialogue()
    {
        if (!isPlaying)
            return;

        currentLine++;

        if (currentLine >= activeDialogue.Length)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        DialogueLine line = activeDialogue[currentLine];
        SetActiveSpeaker(line.speaker != null ? line.speaker : defaultSpeaker);
        speakerNameText.text = line.speakerName;
        dialogueText.text = line.dialogueText;

        if (portraitImage != null)
        {
            portraitImage.sprite = line.portrait;
            portraitImage.gameObject.SetActive(line.portrait != null);
        }

    }

    private void EndDialogue()
    {
        bool wasOpeningDialogue = isOpeningDialogue;

        isPlaying = false;
        isOpeningDialogue = false;
        activeDialogue = null;
        defaultSpeaker = null;
        dialoguePartner = null;
        SetActiveSpeaker(null);
        RestoreModelRotations();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (missionPanelStateSaved && missionPanel != null)
            missionPanel.SetActive(missionPanelWasActive);
        missionPanelStateSaved = false;

        if (spacePromptText != null)
            spacePromptText.gameObject.SetActive(false);

        DialogueEnded?.Invoke();

        if (cursorStateSaved)
        {
            Cursor.lockState = cursorLockStateBeforeDialogue;
            Cursor.visible = cursorVisibleBeforeDialogue;
            cursorStateSaved = false;
        }

        if (wasOpeningDialogue)
            OpeningDialogueEnded?.Invoke();
    }

    private void OnDisable()
    {
        SetActiveSpeaker(null);
        RestoreModelRotations();
    }

    private void OnEnable()
    {
        if (!isPlaying)
            return;

        DialogueLine line = activeDialogue[currentLine];
        SetActiveSpeaker(line.speaker != null ? line.speaker : defaultSpeaker);
    }

    private void SetActiveSpeaker(CharacterCombat speaker)
    {
        if (activeSpeaker == speaker)
            return;

        if (activeSpeaker != null)
            activeSpeaker.SetTalking(false);

        activeSpeaker = speaker;
        if (activeSpeaker != null)
        {
            if (activeSpeaker != player)
                dialoguePartner = activeSpeaker;

            activeSpeaker.SetTalking(true);
        }
    }

    private bool SpacePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space))
            return true;
#endif
        return false;
    }
}
