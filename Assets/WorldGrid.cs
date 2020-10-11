using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class WorldGrid : MonoBehaviour
{
    private struct ChunkContainer
    {
        public Chunk Chunk;
        public MeshRenderer Renderer;
        public ComputeBuffer CellsComputeBuffer;
        public RenderTexture OutputRenderTexture;
        public bool RequiresRedraw;
    }
    
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private float simulationStep;
    [SerializeField] private MeshRenderer chunkPrefab;
    [SerializeField] private float chunkScale;
    

    private Dictionary<Vector2Int, ChunkContainer> chunkContainers = new Dictionary<Vector2Int, ChunkContainer>();
    private int renderChunkKernelIndex;

    private void Start()
    {
        renderChunkKernelIndex = computeShader.FindKernel("render_chunk");

        for (var x = -1; x < 2; x++)
        {
            for (var y = -1; y < 2; y++)
            {
                var chunkRenderer = Instantiate(chunkPrefab, transform, true);
                chunkRenderer.transform.position = chunkScale * new Vector3(x, y);
                chunkRenderer.transform.localScale = new Vector3(chunkScale, chunkScale, 1);
                
                var outputRenderTexture = new RenderTexture(64, 64, 32)
                {
                    enableRandomWrite = true,
                    useMipMap = false,
                    filterMode = FilterMode.Point
                };
                
                outputRenderTexture.Create();

                var cells = new Cell[64 * 64];

                var cellsComputeBuffer = new ComputeBuffer(cells.Length, 128);
                
                chunkRenderer.material.mainTexture = outputRenderTexture;
                
                
                
            // for (var i = 0; i < cells.Length; i++)
            //  {
            //      var cell = cells[i];
            //      cell.type = (Random.value > 0.5) ? CellType.Stone : CellType.Sand;
            //      cells[i] = cell;
            //  }
            //                  cellsBuffer.SetData(cells);
            //     computeShader.SetBuffer(renderChunkKernelIndex, "cells", cellsBuffer);
            //     computeShader.SetTexture(renderChunkKernelIndex, "texture_out", outputRenderTexture);
            //
            //     computeShader.Dispatch(renderChunkKernelIndex, 8, 8, 1);
                
                
                var chunk = new Chunk
                {
                    Cells = cells
                };
                
                var chunkContainer = new ChunkContainer
                {
                    Chunk = chunk,
                    Renderer = chunkRenderer,
                    RequiresRedraw = false,
                    CellsComputeBuffer = cellsComputeBuffer,
                    OutputRenderTexture = outputRenderTexture
                };
                
                chunkContainers.Add(new Vector2Int(x, y), chunkContainer);
            }
        }

        StartCoroutine(UpdateWorldCoroutine());
    }

    private void Update()
    {
        var minX = chunkContainers.Min(a => a.Key.x);
        var minY = chunkContainers.Min(a => a.Key.y);
        var maxX = chunkContainers.Max(a => a.Key.x);
        var maxY = chunkContainers.Max(a => a.Key.y);

        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++)
            {
                var chunkPosition = new Vector2Int(x, y);
                var chunkContainer = chunkContainers[chunkPosition];

                if (chunkContainer.RequiresRedraw)
                {
                    chunkContainer.CellsComputeBuffer.SetData(chunkContainer.Chunk.Cells);
                    computeShader.SetBuffer(renderChunkKernelIndex, "cells", chunkContainer.CellsComputeBuffer);
                    computeShader.SetTexture(renderChunkKernelIndex, "texture_out", chunkContainer.OutputRenderTexture);
                    
                    computeShader.Dispatch(renderChunkKernelIndex, 8, 8, 1);

                    chunkContainer.RequiresRedraw = false;
                    chunkContainers[chunkPosition] = chunkContainer;
                }
            }
        }
        
        // foreach (ref var chunkContainerKeyValuePair in chunkContainers)
        // {
        //     var chunkPosition = chunkContainerKeyValuePair.Key;
        //     var chunkContainer = chunkContainerKeyValuePair.Value;
        //     
        //     
        // }
    }

    private IEnumerator UpdateWorldCoroutine()
     {
         while (true)
         {
             // for (var i = 0; i < cells.Length; i++)
             // {
             //     var cell = cells[i];
             //     cell.type = (Random.value > 0.5) ? CellType.Stone : CellType.Sand;
             //     cells[i] = cell;
             // }
             //
             // requiresRender = true;
             
             var minX = chunkContainers.Min(a => a.Key.x);
             var minY = chunkContainers.Min(a => a.Key.y);
             var maxX = chunkContainers.Max(a => a.Key.x);
             var maxY = chunkContainers.Max(a => a.Key.y);

             for (var x = minX; x <= maxX; x++)
             {
                 for (var y = minY; y <= maxY; y++)
                 {
                     var chunkPosition = new Vector2Int(x, y);
                     var chunkContainer = chunkContainers[chunkPosition];

                     chunkContainer.RequiresRedraw = true;

                     var cells = chunkContainer.Chunk.Cells;
                     
                     for (var i = 0; i < cells.Length; i++)
                     {
                         var cell = cells[i];
                         cell.type = (Random.value > 0.5) ? CellType.Stone : CellType.Sand;
                         cells[i] = cell;
                     }

                     chunkContainer.Chunk.Cells = cells;
                     chunkContainers[chunkPosition] = chunkContainer;
                 }
             }
             
             yield return new WaitForSeconds(simulationStep);
         }
     }
}