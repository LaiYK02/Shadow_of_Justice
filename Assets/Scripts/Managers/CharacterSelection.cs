using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CharacterSelection : MonoBehaviour
{
    private static readonly int Idle = Animator.StringToHash("Base Layer.Idle");
    private static readonly int Melee = Animator.StringToHash("Base Layer.Standing Melee Attack");
    private static readonly int Talking = Animator.StringToHash("IsTalking");
    private static readonly int Attacking = Animator.StringToHash("IsAttacking");
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private static readonly Color RegularPlate = new(0.06f, 0.11f, 0.16f, 0.96f);
    private static readonly Color FocusedPlate = new(0.20f, 0.29f, 0.36f, 0.98f);

    [Serializable]
    private sealed class Preview
    {
        public Animator animator;
        public Renderer body;
        public Image plate;
        public Image accent;

        public void Focus(bool focused, bool talk)
        {
            animator.SetBool(Talking, focused && talk);
            plate.color = focused ? FocusedPlate : RegularPlate;
            accent.enabled = focused;

            Material material = body.sharedMaterial;
            int colorId = material.HasProperty(BaseColor) ? BaseColor : ColorProperty;
            if (!material.HasProperty(colorId)) return;

            var properties = new MaterialPropertyBlock();
            body.GetPropertyBlock(properties);
            Color original = material.GetColor(colorId);
            properties.SetColor(colorId, focused ? Color.Lerp(original, Color.white, 0.22f) : original);
            body.SetPropertyBlock(properties);
        }
    }

    [SerializeField] private GameObject menuContent;
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private CanvasGroup blackScreen;
    [SerializeField] private Camera menuCamera;
    [SerializeField] private Camera selectionCamera;
    [SerializeField] private AudioListener menuListener;
    [SerializeField] private AudioListener selectionListener;
    [SerializeField] private Preview male;
    [SerializeField] private Preview female;
    [SerializeField, Min(0.1f)] private float confirmationDuration = 1.05f;
    [SerializeField, Min(0.1f)] private float fadeDuration = 0.45f;

    private Action loadGame;
    private PlayableCharacter? hovered;
    private Vector2 lastPointerPosition;
    private bool choosing;

    private void Awake()
    {
        selectionCamera.enabled = false;
        selectionListener.enabled = false;
        selectionPanel.SetActive(false);
        blackScreen.alpha = 0f;
        blackScreen.gameObject.SetActive(false);
        male.animator.gameObject.SetActive(false);
        female.animator.gameObject.SetActive(false);
    }

    public void Show(Action onConfirmed)
    {
        if (loadGame != null)
            return;

        loadGame = onConfirmed;
        StartCoroutine(RevealSelection());
    }

    private IEnumerator RevealSelection()
    {
        yield return FadeBlackScreen(1f);

        menuContent.SetActive(false);
        menuCamera.enabled = false;
        menuListener.enabled = false;
        selectionCamera.enabled = true;
        selectionListener.enabled = true;
        selectionPanel.SetActive(true);
        ResetPreview(male);
        ResetPreview(female);

        yield return null;
        yield return FadeBlackScreen(0f);
        blackScreen.gameObject.SetActive(false);
        lastPointerPosition = Mouse.current?.position.ReadValue() ?? Vector2.zero;
        choosing = true;
    }

    private static void ResetPreview(Preview preview)
    {
        preview.animator.gameObject.SetActive(true);
        preview.animator.Play(Idle, 0, 0f);
        preview.Focus(false, false);
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (!choosing || mouse == null)
            return;

        Vector2 position = mouse.position.ReadValue();
        bool clicked = mouse.leftButton.wasPressedThisFrame;
        if (position == lastPointerPosition && !clicked)
            return;

        lastPointerPosition = position;
        Rect viewport = selectionCamera.pixelRect;
        PlayableCharacter? next = viewport.Contains(position)
            ? position.x < viewport.center.x ? PlayableCharacter.Male : PlayableCharacter.Female
            : null;
        if (hovered != next)
        {
            hovered = next;
            male.Focus(hovered == PlayableCharacter.Male, true);
            female.Focus(hovered == PlayableCharacter.Female, true);
            if (hovered.HasValue)
                MainMenuAudioController.Instance?.PlayHover();
        }

        if (clicked && next.HasValue)
            StartCoroutine(Confirm(next.Value));
    }

    private IEnumerator Confirm(PlayableCharacter choice)
    {
        choosing = false;
        MainMenuAudioController.Instance?.PlayPress();
        bool isFemale = choice == PlayableCharacter.Female;
        male.Focus(!isFemale, false);
        female.Focus(isFemale, false);
        Animator animator = isFemale ? female.animator : male.animator;
        animator.SetBool(Attacking, true);
        animator.CrossFadeInFixedTime(Melee, 0.08f, 0, 0f);
        PlayerCharacterAppearance.SelectedCharacter = choice;

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, confirmationDuration - fadeDuration));
        yield return FadeBlackScreen(1f);
        yield return null;
        loadGame?.Invoke();
    }

    private IEnumerator FadeBlackScreen(float targetAlpha)
    {
        float startAlpha = blackScreen.alpha;
        blackScreen.gameObject.SetActive(true);
        for (float elapsed = 0; elapsed < fadeDuration; elapsed += Time.unscaledDeltaTime)
        {
            blackScreen.alpha = Mathf.SmoothStep(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        blackScreen.alpha = targetAlpha;
    }
}
