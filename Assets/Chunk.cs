using Unity.Collections;

public struct Chunk
{
    public NativeArray<Cell> Cells;

    public readonly Chunk Clone(Allocator allocator)
    {
        var clonedCells = new NativeArray<Cell>(Cells.Length, allocator);

        Cells.CopyTo(clonedCells);

        return new Chunk
        {
            Cells = clonedCells
        };
    }
}





// using System.Collections;
//
// public struct Chunk
// {
//     [SerializeField] private ComputeShader computeShader;
//     [SerializeField] private float simulationStep;
//
//     private MeshRenderer renderer;
//     private Cell[] cells;
//     private ComputeBuffer cellsBuffer;
//     private int kernelIndex;
//     private RenderTexture outputRenderTexture;
//
//     private bool requiresRender = false;
//     
//     private void Awake()
//     {
//         renderer = GetComponent<MeshRenderer>();
//     }
//
//     private void Start()
//     {
//         outputRenderTexture = new RenderTexture(64, 64, 32)
//         {
//             enableRandomWrite = true,
//             useMipMap = false,
//             filterMode = FilterMode.Point
//         };
//         
//         outputRenderTexture.Create();
//
//         cells = new Cell[64 * 64];
//         
//         cellsBuffer = new ComputeBuffer(cells.Length, 128);
//         
//         kernelIndex = computeShader.FindKernel("render_chunk");
//
//         renderer.material.mainTexture = outputRenderTexture;
//
//         StartCoroutine(UpdateWorldCoroutine());
//     }
//
//     private IEnumerator UpdateWorldCoroutine()
//     {
//         while (true)
//         {
//             for (var i = 0; i < cells.Length; i++)
//             {
//                 var cell = cells[i];
//                 cell.type = (Random.value > 0.5) ? CellType.Stone : CellType.Sand;
//                 cells[i] = cell;
//             }
//
//             requiresRender = true;
//
//             yield return new WaitForSeconds(simulationStep);
//         }
//     }
//     
//     private void Update()
//     {
//         if (requiresRender)
//         {
//             cellsBuffer.SetData(cells);
//             computeShader.SetBuffer(kernelIndex, "cells", cellsBuffer);
//             computeShader.SetTexture(kernelIndex, "texture_out", outputRenderTexture);
//
//             computeShader.Dispatch(kernelIndex, 8, 8, 1);
//
//             requiresRender = false;
//         }
//     }
// }
