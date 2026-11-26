using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class DistrictNavigationPanel : MonoBehaviour
{
    [Serializable]
    private sealed class Destination
    {
        public District district;
        public TMP_Text status;
        public Image mapMarker;
        public Button mapButton;
        public Button button;
    }

    [SerializeField] private District currentDistrict;
    [SerializeField] private GameObject overlay;
    [SerializeField] private Button close;
    [SerializeField] private Destination[] destinations;
    [SerializeField] private PlayerHUDController hud;
    [SerializeField] private PlayerInteractionManager interaction;
    [SerializeField] private CharacterCombat combat;
    [SerializeField] private PlayerTravelProgress progress;
    [SerializeField] private TreasureHunt treasureHunt;

    private static readonly Color Current = new Color32(249, 198, 111, 255);
    private static readonly Color Available = new Color32(133, 220, 208, 255);
    private static readonly Color Locked = new Color32(182, 199, 209, 255);

    private bool open;
    private bool closeRequested;
    private bool loading;
    private float previousTimeScale;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisibility;

    internal bool CanOpen => isActiveAndEnabled && !open && !loading && Time.timeScale > 0f &&
        hud.IsVisible && interaction.isActiveAndEnabled && combat.CanAct && !combat.IsAttacking && !combat.IsBlocking;

    private void Awake()
    {
        overlay.SetActive(false);
        close.onClick.AddListener(RequestClose);
        foreach (Destination destination in destinations)
        {
            destination.button.onClick.AddListener(() => Travel(destination.district));
            destination.mapButton.onClick.AddListener(() => Travel(destination.district));
        }
    }

    internal void Open()
    {
        if (!CanOpen)
            return;

        open = true;
        previousTimeScale = Time.timeScale;
        previousCursorLock = Cursor.lockState;
        previousCursorVisibility = Cursor.visible;
        Time.timeScale = 0f;
        interaction.enabled = false;
        hud.SetTravelMenuActive(true);
        ShowCursor();
        overlay.SetActive(true);

        Button first = close;
        foreach (Destination destination in destinations)
        {
            bool here = destination.district == currentDistrict;
            bool available = !here && destination.district == DistrictTravelSession.NextDestination;
            Color color = here ? Current : available ? Available : Locked;
            destination.status.text = here ? "You are here" : available ? "Travel available" : "Locked";
            destination.status.color = color;
            destination.mapMarker.color = color;
            destination.mapButton.interactable = available;
            destination.button.interactable = available;
            if (available)
                first = destination.button;
        }
        first.Select();
    }

    private void Update()
    {
        if (open && !loading && EscapePressed())
            RequestClose();
    }

    private void LateUpdate()
    {
        // Keep the world paused until all gameplay input has been read for this frame.
        if (closeRequested)
            RestoreGameplay();
    }

    private void OnDisable() => RestoreGameplay();

    private void OnApplicationFocus(bool focused)
    {
        if (focused && open)
            ShowCursor();
    }

    private void RequestClose() => closeRequested = open && !loading;

    private void RestoreGameplay()
    {
        if (!open)
            return;

        open = false;
        closeRequested = false;
        Time.timeScale = previousTimeScale;
        if (!loading)
        {
            interaction.enabled = true;
            hud.SetTravelMenuActive(false);
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisibility;
            overlay.SetActive(false);
        }
    }

    private void Travel(District district)
    {
        if (!open || loading || closeRequested || district == currentDistrict ||
            district != DistrictTravelSession.NextDestination)
            return;

        string scene = district.ToString();
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError($"Add {scene} to the active build profile to enable travel.", this);
            return;
        }

        loading = true;
        if (treasureHunt != null)
            treasureHunt.SettleCompletionReward(hud);
        DistrictTravelSession.Depart(district, progress.Capture());
        SceneManager.LoadScene(scene);
    }

    private static void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }
}
