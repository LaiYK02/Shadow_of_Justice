using UnityEngine;

/// <summary>Shows player HUD elements only during active gameplay.</summary>
public class PlayerHUDController : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private GameObject[] hudElements;

    private bool dialogueActive;
    private bool cutsceneActive;
    private bool choiceActive;
    private bool missionStartActive;

    private void OnEnable()
    {
        if (dialogueManager != null)
        {
            dialogueManager.DialogueStarted += HandleDialogueStarted;
            dialogueManager.DialogueEnded += HandleDialogueEnded;
        }

        RefreshVisibility();
    }

    private void OnDisable()
    {
        if (dialogueManager != null)
        {
            dialogueManager.DialogueStarted -= HandleDialogueStarted;
            dialogueManager.DialogueEnded -= HandleDialogueEnded;
        }
    }

    public void SetCutsceneActive(bool active)
    {
        cutsceneActive = active;
        RefreshVisibility();
    }

    public void SetChoiceActive(bool active)
    {
        choiceActive = active;
        RefreshVisibility();
    }

    public void SetMissionStartActive(bool active)
    {
        missionStartActive = active;
        RefreshVisibility();
    }

    private void HandleDialogueStarted()
    {
        dialogueActive = true;
        RefreshVisibility();
    }

    private void HandleDialogueEnded()
    {
        dialogueActive = false;
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        bool visible = !dialogueActive && !cutsceneActive && !choiceActive && !missionStartActive;
        if (hudElements == null)
            return;

        foreach (GameObject element in hudElements)
        {
            if (element != null)
                element.SetActive(visible);
        }
    }
}
