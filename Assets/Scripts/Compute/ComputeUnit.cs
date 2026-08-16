using UnityEngine;

public enum ComputeActionType
{
    Single,
    Continuous
}

[CreateAssetMenu(fileName = "New Compute Unit", menuName = "Game/ComputeUnit")]
public class ComputeUnit : ScriptableObject
{
    public int ID;
    public ComputeShader computeShader;
    public string Name;
    public ComputeActionType actionType;
}