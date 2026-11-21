using UnityEngine;

public enum PlayableCharacter
{
    Male,
    Female
}

// Spawn the visual before CharacterCombat finds the player's Animator in Awake.
[DefaultExecutionOrder(-100)]
public sealed class PlayerCharacterAppearance : MonoBehaviour
{
    [SerializeField] private Animator maleModel;
    [SerializeField] private Animator femaleModel;

    public static PlayableCharacter SelectedCharacter { get; internal set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSelection() => SelectedCharacter = PlayableCharacter.Male;

    private void Awake()
    {
        Animator model = SelectedCharacter == PlayableCharacter.Female ? femaleModel : maleModel;
        if (model != null)
            Instantiate(model, transform, false);
        else
            Debug.LogError("Assign the playable character models on the player.", this);
    }
}
