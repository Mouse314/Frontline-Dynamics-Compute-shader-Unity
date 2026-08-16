using UnityEngine;

[CreateAssetMenu(fileName = "ComputeBufferWrap", menuName = "Game/ComputeBufferWrap")]
public class ComputeBufferWrap : ScriptableObject
{
    public ComputeBuffer buffer;
    public string Name;
}
