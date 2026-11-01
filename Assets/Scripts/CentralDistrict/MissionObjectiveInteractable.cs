using UnityEngine;

public class MissionObjectiveInteractable : PlayerInteractable
{
    [SerializeField] private MissionFlowController missionFlow;
    [SerializeField] private string objectiveId = "victim";
    private bool completed;

    public override bool CanInteract => !completed && missionFlow != null && missionFlow.CanInteract;

    protected override void Start()
    {
        base.Start();
        if (missionFlow == null) Debug.LogError($"{name}: Assign MissionFlowController.", this);
    }

    public override void Interact()
    {
        if (!CanInteract) return;
        completed = true;
        missionFlow.CompleteObjective(objectiveId);
        Debug.Log("Evidence inspected: " + objectiveId, this);
    }
}
