using System.Collections.Generic;
using UnityEngine;

public class ComputeWorker : MonoBehaviour
{
    public List<ComputeUnit> computeUnits;
    public ComputeShader activeShader;

    public void Dispatch(int kernelIndex, int threadGroupsX, int threadGroupsY, int threadGroupsZ, RenderTexture textureIn, RenderTexture textureOut)
    {
        activeShader.SetTexture(kernelIndex, "InputTexture", textureIn);
        activeShader.SetTexture(kernelIndex, "OutputTexture", textureOut);

        activeShader.Dispatch(kernelIndex, threadGroupsX, threadGroupsY, 1);
    }


    public void SetData(float time)
    {
        activeShader.SetFloat("_Time", time);
    }
}
