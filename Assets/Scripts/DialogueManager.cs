
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
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button continueButton;

    [Header("Opening Dialogue")]
    [SerializeField] private DialogueLine[] openingDialogue;

    private DialogueLine[] activeDialogue;
    private int currentLine;
    private bool isPlaying;

    private void Awake()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(AdvanceDialogue);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(AdvanceDialogue);
    }

    private void Update()
    {
        if (isPlaying && SpacePressed())
            AdvanceDialogue();
    }

    public void StartOpeningDialogue()
    {
        StartDialogue(openingDialogue);
    }

    // Reuse this method for future NPC conversations.
    public void StartDialogue(DialogueLine[] lines)
    {
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

        dialoguePanel.SetActive(true);
        ShowCurrentLine();
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

        if (continueButton != null)
            continueButton.interactable = true;
    }

    private void EndDialogue()
    {
        isPlaying = false;
        activeDialogue = null;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
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
