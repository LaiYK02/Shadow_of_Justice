using UnityEngine;

/// <summary>Animates the repeating street behind the main menu.</summary>
[DisallowMultipleComponent]
public sealed class MainMenuStreetLoop : MonoBehaviour
{
    private const int SectionCount = 4;

    [SerializeField] private Transform streetSection;
    [SerializeField] private Transform leftLane;
    [SerializeField] private Transform rightLane;
    [SerializeField, Min(1f)] private float sectionLength = 81.312f;
    [SerializeField] private float sectionStartOffset = -40.656f;
    [SerializeField, Min(0f)] private float walkingSpeed = 1.4f;
    [SerializeField, Min(0f)] private float leftLaneSpeed = 7f;
    [SerializeField, Min(0f)] private float rightLaneSpeed = 8f;

    private Vector3 streetForward;
    private Vector3 streetStart;
    private Vector3 cameraStart;
    private Transform[] leftCars;
    private Transform[] rightCars;
    private float cameraDistance;

    private void Start()
    {
        if (streetSection == null || leftLane == null || rightLane == null || sectionLength <= 0f)
        {
            Debug.LogError("Main Menu Street Loop needs a street section and both traffic lanes.", this);
            enabled = false;
            return;
        }

        streetForward = Vector3.ProjectOnPlane(streetSection.forward, Vector3.up).normalized;
        streetStart = streetSection.position + streetForward * sectionStartOffset;
        cameraStart = transform.position;

        for (int section = 1; section < SectionCount; section++)
        {
            Vector3 position = streetSection.position + streetForward * (section * sectionLength);
            Transform copy = Instantiate(streetSection, position, streetSection.rotation, streetSection.parent);
            copy.name = $"{streetSection.name} {section + 1}";
        }

        leftCars = PrepareTraffic(leftLane);
        rightCars = PrepareTraffic(rightLane);
    }

    private Transform[] PrepareTraffic(Transform lane)
    {
        int authoredCount = lane.childCount;
        var cars = new Transform[authoredCount * SectionCount];
        for (int index = 0; index < authoredCount; index++)
        {
            Transform original = lane.GetChild(index);
            foreach (Rigidbody body in original.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.useGravity = false;
                body.detectCollisions = false;
                body.interpolation = RigidbodyInterpolation.None;
            }

            cars[index] = original;
            for (int section = 1; section < SectionCount; section++)
            {
                Vector3 position = original.position + streetForward * (section * sectionLength);
                cars[section * authoredCount + index] = Instantiate(original, position, original.rotation, lane);
            }
        }
        return cars;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        cameraDistance = Mathf.Repeat(cameraDistance + walkingSpeed * deltaTime, sectionLength);

        // Each section has an identical traffic phase, so wrapping the camera preserves the view.
        transform.position = cameraStart + streetForward * cameraDistance;

        MoveTraffic(leftCars, leftLaneSpeed * deltaTime);
        MoveTraffic(rightCars, -rightLaneSpeed * deltaTime);
    }

    private void MoveTraffic(Transform[] cars, float displacement)
    {
        float loopLength = sectionLength * SectionCount;
        foreach (Transform car in cars)
        {
            float position = Vector3.Dot(car.position - streetStart, streetForward);
            float nextPosition = Mathf.Repeat(position + displacement, loopLength);
            car.position += streetForward * (nextPosition - position);
        }
    }
}
