using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class ChunkBehaviour : MonoBehaviour
{
    public void SetTexture(RenderTexture renderTexture)
    {
        var meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material.mainTexture = renderTexture;
    }
    //
    // public void UpdateCollider(List<List<Vector2>> paths)
    // {
    //     var polygonCollider = GetComponent<PolygonCollider2D>();
    //     
    //     for (var i = 0; i < paths.Count; i++)
    //     {
    //         polygonCollider.SetPath(i, paths[i]);
    //     }
    // }
}