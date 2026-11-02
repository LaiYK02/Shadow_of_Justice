using System;
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

    private DialogueLine[] activeDialogue;
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

    public void StartOpeningDialogue()
    {
        StartDialogueInternal(openingDialogue, true);
    }

    public void StartDialogue(DialogueLine[] lines)
    {
        StartDialogueInternal(lines, false);
    }

    private void StartDialogueInternal(DialogueLine[] lines, bool opening)
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
