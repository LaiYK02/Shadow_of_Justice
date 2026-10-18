using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class OpeningSequenceManager : MonoBehaviour,
    IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private GameObject openingPanel;
    [SerializeField] private TMP_Text openingText;
    [SerializeField] private TMP_Text skipHintText;
    [SerializeField] private TypewriterEffect typewriter;

    [Header("Timing")]
    [SerializeField, Min(0f)]
    private float paragraphDelay = 3f;

    [SerializeField, Min(0f)]
    private float hintDisplayDuration = 3f;

    [Header("Opening Story")]
    [SerializeField, TextArea(3, 6)]
    private string[] openingParagraphs =
    {
        "SERI SENTOSA\n\nA modern city of opportunity, ambition, and hidden secrets.",

        "You are a newly appointed rookie police officer, beginning your first assignment in Seri Sentosa. You have sworn to protect the people and uphold the law.",

        "Behind the city's busy streets and shining buildings, crime continues to grow. Some cases appear ordinary, but their connections may reveal something much bigger.",

        "Your duty is to investigate the truth, gather evidence, and bring criminals to justice. In this city, wealth and influence must never place anyone above the law.",

        "Your first day begins now. Welcome to SHADOW OF JUSTICE."
    };

    [Header("Events")]
    [SerializeField] private UnityEvent onOpeningFinished;

    private int currentParagraph;
    private bool sequenceFinished;

    private float paragraphTimer;
    private float hintTimer;

    private bool hintVisible;

    private void Start()
    {
        if (openingPanel == null ||
            openingText == null ||
            typewriter == null)
        {
            Debug.LogError(
                "OpeningSequenceManager: Assign all required UI references."
            );

            enabled = false;
            return;
        }

        if (openingParagraphs == null ||
            openingParagraphs.Length == 0)
        {
            Debug.LogError(
                "OpeningSequenceManager: No opening paragraphs assigned."
            );

            enabled = false;
            return;
        }

        openingPanel.SetActive(true);

        currentParagraph = 0;
        sequenceFinished = false;
        paragraphTimer = 0f;

        SetHintVisible(false);
        ShowCurrentParagraph();
    }

    private void Update()
    {
        if (sequenceFinished)
            return;

        // Space skips the entire introduction.
        if (SpacePressed())
        {
            FinishOpening();
            return;
        }

        // Any other keyboard key shows the skip hint.
        if (OtherKeyPressed())
        {
            ShowSkipHint();
        }

        UpdateSkipHint();
        UpdateParagraphProgression();
    }

    private void UpdateParagraphProgression()
    {
        // Start counting only after typing has finished.
        if (typewriter.IsTyping)
        {
            paragraphTimer = 0f;
            return;
        }

        paragraphTimer += Time.unscaledDeltaTime;

        if (paragraphTimer >= paragraphDelay)
        {
            AdvanceParagraph();
        }
    }

    private void AdvanceParagraph()
    {
        paragraphTimer = 0f;

        currentParagraph++;

        if (currentParagraph < openingParagraphs.Length)
        {
            ShowCurrentParagraph();
        }
        else
        {
            FinishOpening();
        }
    }

    private void ShowCurrentParagraph()
    {
        openingText.alignment = TextAlignmentOptions.Center;

        typewriter.StartTyping(
            openingParagraphs[currentParagraph]
        );
    }

    private void ShowSkipHint()
    {
        if (skipHintText == null)
            return;

        hintVisible = true;
        hintTimer = 0f;

        skipHintText.gameObject.SetActive(true);
    }

    private void UpdateSkipHint()
    {
        if (!hintVisible)
            return;

        hintTimer += Time.unscaledDeltaTime;

        if (hintTimer >= hintDisplayDuration)
        {
            SetHintVisible(false);
        }
    }

    private void SetHintVisible(bool visible)
    {
        hintVisible = visible;
        hintTimer = 0f;

        if (skipHintText != null)
        {
            skipHintText.gameObject.SetActive(visible);
        }
    }

    private bool SpacePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space))
        {
            return true;
        }
#endif

        return false;
    }

    private bool OtherKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            Keyboard.current.anyKey.wasPressedThisFrame &&
            !Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.anyKeyDown &&
            !Input.GetKeyDown(KeyCode.Space))
        {
            return true;
        }
#endif

        return false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (sequenceFinished)
            return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Clicking shows the hint but does not skip the story.
            ShowSkipHint();
        }
    }

    private void FinishOpening()
    {
        if (sequenceFinished)
            return;

        sequenceFinished = true;
        SetHintVisible(false);
        openingPanel.SetActive(false);

        onOpeningFinished?.Invoke();
    }
}
