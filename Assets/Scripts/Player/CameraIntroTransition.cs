using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class CameraIntroTransition : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private CinemachineExternalCamera introStartCamera;
    [SerializeField] private CinemachineCamera playerFollowCamera;
    [SerializeField] private CinemachineBrain cameraBrain;
    [SerializeField, Min(0.1f)] private float blendDuration = 3f;

    [Header("Cutscene")]
    [SerializeField] private PlayerInteractionManager playerInteraction;
    [SerializeField] private PlayerHUDController playerHUD;
    [SerializeField] private UnityEvent onTransitionFinished;

    private bool hasStarted;
    private bool restorePlayerInteraction;
    private bool blendSettingsSaved;
    private CinemachineBlendDefinition originalBlend;
    private bool originalIgnoreTimeScale;

    private void Awake()
    {
        if (DistrictTravelSession.HasVisitedOldTown)
        {
            if (introStartCamera != null)
                introStartCamera.enabled = false;
            enabled = false;
            return;
        }

        if (introStartCamera == null || playerFollowCamera == null || cameraBrain == null)
        {
            Debug.LogError("CameraIntroTransition: Assign the intro camera, follow camera, and camera brain.", this);
            enabled = false;
            return;
        }

        // The player's movement and camera controls already wait for the opening dialogue.
        if (playerInteraction != null && playerInteraction.enabled)
        {
            restorePlayerInteraction = true;
            playerInteraction.enabled = false;
        }

        introStartCamera.Priority = playerFollowCamera.Priority.Value + 1;
        introStartCamera.enabled = true;
    }

    public void BeginTransition()
    {
        if (hasStarted || !isActiveAndEnabled)
            return;

        hasStarted = true;
        StartCoroutine(BlendToPlayerCamera());
    }

    private IEnumerator BlendToPlayerCamera()
    {
        originalBlend = cameraBrain.DefaultBlend;
        originalIgnoreTimeScale = cameraBrain.IgnoreTimeScale;
        blendSettingsSaved = true;
        cameraBrain.DefaultBlend = new CinemachineBlendDefinition(
            CinemachineBlendDefinition.Styles.EaseInOut, blendDuration);
        cameraBrain.IgnoreTimeScale = true;

        // Leave the source active until Cinemachine finishes using it.
        introStartCamera.Priority = playerFollowCamera.Priority.Value - 1;
        introStartCamera.Prioritize();
        yield return null;
        while (cameraBrain.ActiveVirtualCamera != (ICinemachineCamera)playerFollowCamera || cameraBrain.IsBlending)
            yield return null;

        ReleaseIntroCamera();
        onTransitionFinished?.Invoke();
        RestoreInteraction();
        if (playerHUD != null)
            playerHUD.SetCutsceneActive(false);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        ReleaseIntroCamera();
        RestoreInteraction();
    }

    private void ReleaseIntroCamera()
    {
        if (introStartCamera != null)
            introStartCamera.enabled = false;

        if (!blendSettingsSaved || cameraBrain == null)
            return;

        cameraBrain.DefaultBlend = originalBlend;
        cameraBrain.IgnoreTimeScale = originalIgnoreTimeScale;
        blendSettingsSaved = false;
    }

    private void RestoreInteraction()
    {
        if (restorePlayerInteraction && playerInteraction != null)
            playerInteraction.enabled = true;
        restorePlayerInteraction = false;
    }
}
