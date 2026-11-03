using UnityEngine;

public class SuspectEscapeTarget : MonoBehaviour
{
    public bool IsEscaping { get; private set; }
    public bool IsStopped { get; private set; }

    public void BeginEscape()
    {
        IsEscaping = true;
        IsStopped = false;
    }

    public void StopEscaping()
    {
        if (IsEscaping)
            IsStopped = true;
    }
}
