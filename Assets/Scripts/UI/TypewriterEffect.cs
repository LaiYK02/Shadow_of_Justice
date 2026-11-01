
using System.Collections;
using TMPro;
using UnityEngine;

public class TypewriterEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text targetText;

    [Header("Typing Settings")]
    [SerializeField, Min(0.001f)]
    private float characterDelay = 0.035f;

    private Coroutine typingCoroutine;
    private bool isTyping;

    public bool IsTyping => isTyping;

    private void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();
    }

    public void StartTyping(string message)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (targetText == null)
        {
            Debug.LogError(
                "TypewriterEffect: Target Text is missing."
            );
            return;
        }

        typingCoroutine = StartCoroutine(TypeText(message));
    }

    private IEnumerator TypeText(string message)
    {
        isTyping = true;

        targetText.text = message;
        targetText.maxVisibleCharacters = 0;
        targetText.ForceMeshUpdate();

        int totalCharacters = targetText.textInfo.characterCount;

        for (int i = 0; i < totalCharacters; i++)
        {
            targetText.maxVisibleCharacters = i + 1;

            yield return new WaitForSecondsRealtime(characterDelay);
        }

        targetText.maxVisibleCharacters = totalCharacters;
        isTyping = false;
        typingCoroutine = null;
    }

    public void CompleteTyping()
    {
        if (!isTyping || targetText == null)
            return;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        targetText.maxVisibleCharacters = int.MaxValue;

        isTyping = false;
        typingCoroutine = null;
    }

    private void OnDisable()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = null;
        isTyping = false;
    }
}
