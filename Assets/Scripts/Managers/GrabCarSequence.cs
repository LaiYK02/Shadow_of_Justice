using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class GrabCarSequence : MonoBehaviour
{
    [SerializeField] private District district;
    [SerializeField] private GameObject carPrefab;
    [SerializeField] private Transform laneStop;
    [SerializeField] private Transform passengerDoor;
    [SerializeField] private Transform curb;
    [Tooltip("Leave empty to end the arrival at the scene's authored Player position.")]
    [SerializeField] private Transform arrivalPoint;
    [SerializeField, Min(1f)] private float laneDistance = 20f;

    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private CinemachineVirtualCameraBase travelCamera;
    [SerializeField] private CinemachineCamera followCamera;
    [SerializeField] private CanvasGroup fade;
    [SerializeField] private TMP_Text travelLabel;
    [SerializeField] private CharacterCombat player;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private ThirdPersonCameraLook look;
    [SerializeField] private PlayerInteractionManager interaction;
    [SerializeField] private PlayerHUDController hud;

    private const float CameraBlendTime = 0.95f;
    private const float DriveTime = 2.2f;
    private const float FadeTime = 0.9f;
    private const float WalkingSpeed = 2.6f;
    private bool arriving;
    private bool playing;
    private bool changingScene;
    private int cameraOverride = -1;
    private Transform car;
    private Transform playerModel;
    private CharacterController controller;
    private TutorialPanelController tutorial;
    private bool controllerWasEnabled;
    private bool interactionWasEnabled;
    private float gameplayTimeScale;

    private Vector3 LaneOffset => laneStop.forward * laneDistance;
    private Vector3 Seat => passengerDoor.position +
        (laneStop.position - passengerDoor.position).normalized * 0.75f - Vector3.up * 0.45f;

    private void Awake()
    {
        arriving = DistrictTravelSession.IsArriving(district);
        fade.alpha = arriving ? 1f : 0f;
        fade.gameObject.SetActive(arriving);
        travelLabel.gameObject.SetActive(arriving);
        if (arriving)
            travelLabel.text = TravelText(district);
    }

    private void Start()
    {
        if (arriving)
            StartCoroutine(Arrive());
    }

    internal bool TryDepart(District destination, PlayerTravelProgress.State progress, float timeScale)
    {
        if (playing || !isActiveAndEnabled)
            return false;

        // The map already owns the interaction lock. Restore it after the ride if the sequence is interrupted.
        LockGameplay(timeScale, restoreInteraction: true);
        StartCoroutine(Depart(destination, progress));
        return true;
    }

    private IEnumerator Depart(District destination, PlayerTravelProgress.State progress)
    {
        travelCamera.enabled = true;
        SpawnCar(laneStop.position - LaneOffset);
        yield return Blend(followCamera, travelCamera);
        yield return Drive(arrivingAtStop: true);

        yield return Walk(curb.position);
        yield return Walk(passengerDoor.position);
        yield return StepIntoCar(true);
        playerModel.gameObject.SetActive(false);
        player.SetLocomotion(false, false);
        travelLabel.text = TravelText(destination);
        fade.gameObject.SetActive(true);
        Coroutine departure = StartCoroutine(Drive(arrivingAtStop: false));
        yield return new WaitForSecondsRealtime(0.55f);
        yield return FadeTo(1f);
        travelLabel.gameObject.SetActive(true);
        yield return departure;

        changingScene = true;
        DistrictTravelSession.Depart(destination, progress);
        SceneManager.LoadSceneAsync(destination.ToString());
    }

    private IEnumerator Arrive()
    {
        LockGameplay(Time.timeScale, interaction.enabled);
        travelCamera.enabled = true;
        SetCamera(null, travelCamera, 1f);
        Vector3 spawn = arrivalPoint != null ? arrivalPoint.position : player.transform.position;
        Quaternion facing = arrivalPoint != null ? arrivalPoint.rotation : player.transform.rotation;
        playerModel.gameObject.SetActive(false);
        player.transform.position = Seat;
        SpawnCar(laneStop.position - LaneOffset);
        yield return new WaitForSecondsRealtime(0.4f);
        travelLabel.gameObject.SetActive(false);
        yield return FadeTo(0f);
        fade.gameObject.SetActive(false);

        yield return Drive(arrivingAtStop: true);
        playerModel.gameObject.SetActive(true);
        yield return StepIntoCar(false);
        yield return Walk(curb.position);
        Coroutine departure = StartCoroutine(Drive(arrivingAtStop: false));
        yield return Walk(spawn);
        Face(facing * Vector3.forward);
        player.SetLocomotion(false, false);
        yield return Blend(travelCamera, followCamera);
        yield return departure;
        UnlockGameplay();
    }

    private void LockGameplay(float timeScale, bool restoreInteraction)
    {
        playing = true;
        gameplayTimeScale = timeScale;
        interactionWasEnabled = restoreInteraction;
        controller = player.GetComponent<CharacterController>();
        tutorial = hud.GetComponentInParent<TutorialPanelController>();
        controllerWasEnabled = controller.enabled;
        playerModel = player.ModelTransform;
        interaction.enabled = false;
        movement.enabled = false;
        look.enabled = false;
        controller.enabled = false;
        hud.SetTravelMenuActive(true);
        if (tutorial != null)
            tutorial.SetTravelActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = timeScale;
    }

    private void UnlockGameplay()
    {
        if (!playing)
            return;
        playing = false;
        if (cameraOverride >= 0)
        {
            if (brain != null)
                brain.ReleaseCameraOverride(cameraOverride);
            cameraOverride = -1;
        }
        if (travelCamera != null)
            travelCamera.enabled = false;
        if (fade != null)
            fade.gameObject.SetActive(false);
        if (car != null)
            Destroy(car.gameObject);
        if (changingScene)
            return;

        playerModel.gameObject.SetActive(true);
        player.SetLocomotion(false, false);
        controller.enabled = controllerWasEnabled;
        movement.enabled = true;
        look.enabled = true;
        interaction.enabled = interactionWasEnabled;
        hud.SetTravelMenuActive(false);
        if (tutorial != null)
            tutorial.SetTravelActive(false);
        Time.timeScale = gameplayTimeScale;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        UnlockGameplay();
    }

    private void SpawnCar(Vector3 start)
    {
        // The prefab faces local -X and its model is offset from the prefab pivot.
        Quaternion rotation = laneStop.rotation * Quaternion.Euler(0f, 90f, 0f);
        car = Instantiate(carPrefab, start, rotation).transform;
        BoxCollider bounds = car.GetComponent<BoxCollider>();
        Vector3 wheelLevel = bounds.center - Vector3.up * (bounds.size.y * 0.5f);
        car.position -= car.TransformPoint(wheelLevel) - car.position;
        bounds.enabled = false;
    }

    private IEnumerator Drive(bool arrivingAtStop)
    {
        Vector3 from = car.position;
        Vector3 to = from + LaneOffset;
        float elapsed = 0f;
        while (elapsed < DriveTime)
        {
            elapsed = Mathf.Min(elapsed + Time.unscaledDeltaTime, DriveTime);
            float progress = elapsed / DriveTime;
            float distance = arrivingAtStop ? 1f - Mathf.Pow(1f - progress, 2f) : progress * progress;
            car.position = Vector3.Lerp(from, to, distance);
            yield return null;
        }
        if (!arrivingAtStop)
            Destroy(car.gameObject);
    }

    private IEnumerator Walk(Vector3 target)
    {
        Vector3 direction = Vector3.ProjectOnPlane(target - player.transform.position, Vector3.up);
        if (direction.sqrMagnitude < 0.0025f)
            yield break;

        player.SetLocomotion(true, false);
        while ((target - player.transform.position).sqrMagnitude > 0.001f)
        {
            Quaternion turn = Quaternion.RotateTowards(
                player.transform.rotation, Quaternion.LookRotation(direction), 600f * Time.unscaledDeltaTime);
            Face(turn * Vector3.forward);
            player.transform.position = Vector3.MoveTowards(
                player.transform.position, target, WalkingSpeed * Time.unscaledDeltaTime);
            yield return null;
        }
        player.transform.position = target;
        player.SetLocomotion(false, false);
    }

    private IEnumerator StepIntoCar(bool enter)
    {
        Vector3 from = enter ? passengerDoor.position : Seat;
        Vector3 to = enter ? Seat : passengerDoor.position;
        Face(Vector3.ProjectOnPlane(to - from, Vector3.up));
        float progress = 0f;
        while (progress < 1f)
        {
            progress = Mathf.Min(progress + Time.unscaledDeltaTime / 0.32f, 1f);
            player.transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }
    }

    private void Face(Vector3 forward) => movement.FaceForCutscene(forward);

    private IEnumerator Blend(ICinemachineCamera from, ICinemachineCamera to)
    {
        float progress = 0f;
        while (progress < 1f)
        {
            SetCamera(from, to, Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
            progress = Mathf.Min(progress + Time.unscaledDeltaTime / CameraBlendTime, 1f);
        }
        SetCamera(null, to, 1f);
        yield return null;
    }

    private void SetCamera(ICinemachineCamera from, ICinemachineCamera to, float weight)
    {
        cameraOverride = brain.SetCameraOverride(cameraOverride, 10, from, to, weight, Time.unscaledDeltaTime);
    }

    private IEnumerator FadeTo(float alpha)
    {
        while (!Mathf.Approximately(fade.alpha, alpha))
        {
            fade.alpha = Mathf.MoveTowards(fade.alpha, alpha, Time.unscaledDeltaTime / FadeTime);
            yield return null;
        }
    }

    private static string TravelText(District destination) => destination switch
    {
        District.OldTown => "Travelling to Old Town...",
        District.HarbourDistrict => "Travelling to Harbour District...",
        _ => "Travelling to Central District..."
    };
}
