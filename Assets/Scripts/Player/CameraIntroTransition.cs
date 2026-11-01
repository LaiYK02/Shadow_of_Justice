using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Events;

public class CameraIntroTransition : MonoBehaviour
{
    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineCamera introCamera;
    [SerializeField] private CinemachineCamera playerFollowCamera;

    [Header("Camera Position Markers")]
    [SerializeField] private Transform introStart;
    [SerializeField] private Transform introEnd;

    [Header("Transition Settings")]
    [SerializeField, Min(0.1f)] private float moveDuration = 3f;
    [SerializeField]
    private AnimationCurve movementCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Camera Priorities")]
    [SerializeField] private int introPriority = 20;
    [SerializeField] private int followPriority = 10;

    [Header("Events")]
    [SerializeField] private UnityEvent onTransitionFinished;

    private bool hasStarted;

    private void Awake()
    {
        if (introCamera == null || playerFollowCamera == null ||
            introStart == null || introEnd == null)
        {
            Debug.LogError(
                "CameraIntroTransition: Assign all camera references and markers.");
            enabled = false;
            return;
        }

        // Keep the intro camera active until the transition finishes.
        introCamera.Priority = introPriority;
        playerFollowCamera.Priority = followPriority;

        // Place the intro camera at the starting marker.
        introCamera.transform.SetPositionAndRotation(
            introStart.position,
            introStart.rotation
        );
    }

    public void BeginTransition()
    {
        if (hasStarted || !enabled)
            return;

        hasStarted = true;
        StartCoroutine(PlayCameraTransition());
    }

    private IEnumerator PlayCameraTransition()
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, moveDuration);

        Vector3 startPosition = introStart.position;
        Quaternion startRotation = introStart.rotation;

        Vector3 endPosition = introEnd.position;
        Quaternion endRotation = introEnd.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = movementCurve.Evaluate(progress);

            introCamera.transform.position = Vector3.LerpUnclamped(
                startPosition,
                endPosition,
                easedProgress
            );

            introCamera.transform.rotation = Quaternion.Slerp(
                startRotation,
                endRotation,
                easedProgress
            );

            yield return null;
        }

        // Finish precisely at the end marker.
        introCamera.transform.SetPositionAndRotation(
            endPosition,
            endRotation
        );

        // Switch to the third-person camera.
        introCamera.Priority = 0;
        playerFollowCamera.Priority = 20;

        // Start the sheriff's dialogue.
        onTransitionFinished?.Invoke();
    }
}
