using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class ChunkBehaviour : MonoBehaviour
{
    public void SetTexture(RenderTexture renderTexture)
    {
        var meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material.mainTexture = renderTexture;
    }
}