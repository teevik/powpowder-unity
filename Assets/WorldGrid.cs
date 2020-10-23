using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;
using static Unity.Mathematics.math;
using int2 = Unity.Mathematics.int2;

public class WorldGrid : MonoBehaviour
{
    private class ChunkContainer
    {
        public Chunk Chunk;
        public MeshRenderer Renderer;
        public ComputeBuffer CellsComputeBuffer;
        public RenderTexture OutputRenderTexture;
        public bool RequiresRedraw;
    }
    
    private const int chunkSize = 128;

    private static Chunk defaultChunk;
    
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private float simulationStep;
    [SerializeField] private MeshRenderer chunkPrefab;
    [SerializeField] private float chunkScale;
    
    
    private readonly Dictionary<int2, ChunkContainer> chunkContainers = new Dictionary<int2, ChunkContainer>();
    private int renderChunkKernelIndex;

    private void Awake()
    {
        defaultChunk = new Chunk {Cells = new NativeArray<Cell>(chunkSize * chunkSize, Allocator.Persistent)};
    }
    
    

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
                
                var outputRenderTexture = new RenderTexture(chunkSize, chunkSize, 32)
                {
                    enableRandomWrite = true,
                    useMipMap = false,
                    filterMode = FilterMode.Point
                };
                
                outputRenderTexture.Create();

                var cells = new NativeArray<Cell>(chunkSize * chunkSize, Allocator.Persistent);

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
                
                for (var i = 0; i < cells.Length; i++)
                {
                    var cell = cells[i];
                    cell.type = (Random.value > 0.5) ? CellType.None : CellType.Sand;
                    cells[i] = cell;
                }

                var chunk = new Chunk
                {
                    Cells = cells
                };

                var chunkContainer = new ChunkContainer
                {
                    Chunk = chunk,
                    Renderer = chunkRenderer,
                    RequiresRedraw = true,
                    CellsComputeBuffer = cellsComputeBuffer,
                    OutputRenderTexture = outputRenderTexture
                };
                
                chunkContainers.Add(int2(x, y), chunkContainer);
            }
        }

        StartCoroutine(UpdateWorldCoroutine());
    }

    private void OnDestroy()
    {
        defaultChunk.Cells.Dispose();
        
        foreach (var chunkContainerPair in chunkContainers)
        {
            var chunkPosition = chunkContainerPair.Key;
            var chunkContainer = chunkContainerPair.Value;
            
            chunkContainer.CellsComputeBuffer.Dispose();
        }
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
                var chunkPosition = int2(x, y);
                var chunkContainer = chunkContainers[chunkPosition];

                if (chunkContainer.RequiresRedraw)
                {
                    Debug.Log("a");

                    chunkContainer.CellsComputeBuffer.SetData(chunkContainer.Chunk.Cells);

                    computeShader.SetInt("chunk_size", chunkSize);
                    computeShader.SetBuffer(renderChunkKernelIndex, "cells", chunkContainer.CellsComputeBuffer);
                    computeShader.SetTexture(renderChunkKernelIndex, "texture_out", chunkContainer.OutputRenderTexture);
                    
                    computeShader.Dispatch(renderChunkKernelIndex, 8, 8, 1);

                    chunkContainer.RequiresRedraw = false;
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

    private struct ChunkWithNeighbors
    {
        public Chunk Chunk;
        public Chunk North;
        public Chunk NorthEast;
        public Chunk East;
        public Chunk SouthEast;
        public Chunk South;
        public Chunk SouthWest;
        public Chunk West;
        public Chunk NorthWest;

        public readonly Chunk ChunkFromPosition(int2 position)
        {
            if (position.Equals(int2(0, 0))) return Chunk;
            if (position.Equals(int2(0, 1))) return North;
            if (position.Equals(int2(1, 1))) return NorthEast;
            if (position.Equals(int2(1, 0))) return East;
            if (position.Equals(int2(1, -1))) return SouthEast;
            if (position.Equals(int2(0, -1))) return South;
            if (position.Equals(int2(-1, -1))) return SouthWest;
            if (position.Equals(int2(-1, 0))) return West;
            if (position.Equals(int2(-1, 1))) return NorthWest;
            
            throw new ArgumentException("Wrong argument bro", nameof(position));
        }
        
        public void SetChunkAtPosition(int2 position, Chunk chunk)
        {
            if (position.Equals(int2(0, 0))) Chunk = chunk;
            else if (position.Equals(int2(0, 1))) North = chunk;
            else if (position.Equals(int2(1, 1))) NorthEast = chunk;
            else if (position.Equals(int2(1, 0))) East = chunk;
            else if (position.Equals(int2(1, -1))) SouthEast = chunk;
            else if (position.Equals(int2(0, -1))) South = chunk;
            else if (position.Equals(int2(-1, -1))) SouthWest = chunk;
            else if (position.Equals(int2(-1, 0))) West = chunk;
            else if (position.Equals(int2(-1, 1))) NorthWest = chunk;
            else throw new ArgumentException("Wrong argument bro", nameof(position));
        }
    }

    [BurstCompile]
    private struct SomeJob : IJob
    {
        public ChunkWithNeighbors ChunkWithNeighbors;
        public bool NeedsRedraw;
        private Unity.Mathematics.Random random;
        
        public SomeJob(ChunkWithNeighbors chunkWithNeighbors, Unity.Mathematics.Random random)
        {
            ChunkWithNeighbors = chunkWithNeighbors;
            NeedsRedraw = false;
            this.random = random;
        }

        public void Execute()
        {
            var chunkWithNeighbors = ChunkWithNeighbors;
            var needsRedraw = false;
            
            Cell GetCellAtPosition(Chunk chunk, int2 cellPosition)
            {
                return chunk.Cells[cellPosition.x + (cellPosition.y * chunkSize)];
            }

            for (var x = 0; x < chunkSize; x++)
            {
                for (var y = 0; y < chunkSize; y++)
                {
                    var cellPosition = new int2(x, y);

                    // var cachedChunkContainers = new ChunkContainer[9];

                    bool GetNeighbor(int2 offset, out int2 neighborChunkPosition,
                        out int2 neighborCellPosition)
                    {
                        var offsetedCellPosition = cellPosition + offset;
                        var chunk_offset = int2(floor(float2(offsetedCellPosition) / float2(chunkSize, chunkSize)));
                            
                        
                        var relative_target_cell = (offsetedCellPosition - (chunk_offset * 64));
                        var b = chunkWithNeighbors.ChunkFromPosition(chunk_offset);
                        
                        neighborCellPosition = relative_target_cell;
                        neighborChunkPosition = chunk_offset;
                        return true;

                        // neighborCellPosition = default;
                        // neighborChunkPosition = default;
                        // return false;
                    }

                    var cell = chunkWithNeighbors.Chunk.Cells[x + (y * chunkSize)];

                    if (cell.type == CellType.Sand)
                    {
                        bool TryMoveTo(int2 offset)
                        {
                            if (GetNeighbor(offset, out var neighborChunkPosition, out var neighborCellPosition))
                            {
                                var neighborChunk = chunkWithNeighbors.ChunkFromPosition(neighborChunkPosition);
                                var neighborCell = GetCellAtPosition(neighborChunk, neighborCellPosition);
                        
                                if (neighborCell.type == CellType.None)
                                {
                                    needsRedraw = true;
                                    // neighborChunk.RequiresRedraw = true;
                        
                                    chunkWithNeighbors.Chunk.Cells[x + (y * chunkSize)] = default;
                                    neighborChunk.Cells[
                                        neighborCellPosition.x + (neighborCellPosition.y * chunkSize)] = cell;
                        
                                    chunkWithNeighbors.SetChunkAtPosition(neighborChunkPosition, neighborChunk);
                                    // chunkWithNeighbors.Chunk ;
                                    
                                    return true;
                                }
                            }
                        
                            return false;
                        }

                        var randomDirection = random.NextBool() ? -1 : 1;
                        if (TryMoveTo(int2(0, -1)))
                        {
                        }
                        else if (TryMoveTo(int2(randomDirection, -1)))
                        {
                        }
                        else if (TryMoveTo(int2(-randomDirection, -1)))
                        {
                        }

                        // if (GetNeighbor(new Vector2Int(0, -1), out var neighborChunkPosition, out var neighborCellPosition))
                        // {
                        //     var neighborChunk = chunkContainers[neighborChunkPosition];
                        //     var neighborCell = GetCellAtPosition(neighborChunk.Chunk, neighborCellPosition);
                        //         
                        //     if (neighborCell.type == CellType.None)
                        //     {
                        //         chunkContainer.RequiresRedraw = true;
                        //         neighborChunk.RequiresRedraw = true;
                        //         
                        //         cells[x + (y * 64)] = default;
                        //         neighborChunk.Chunk.Cells[neighborCellPosition.x + (neighborCellPosition.y * 64)] = cell;
                        //         
                        //         chunkContainers[neighborChunkPosition] = neighborChunk;
                        //     }
                        // }
                    }
                }
            }

            ChunkWithNeighbors = chunkWithNeighbors;
            NeedsRedraw = needsRedraw;
        }
    }
    
    private IEnumerator UpdateWorldCoroutine()
     {
         var random = new System.Random();

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
             
             // Could refactor to have one loop
             var minChunkX = chunkContainers.Min(a => a.Key.x);
             var minChunkY = chunkContainers.Min(a => a.Key.y);
             var maxChunkX = chunkContainers.Max(a => a.Key.x);
             var maxChunkY = chunkContainers.Max(a => a.Key.y);
             
             
             for (var chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
             {
                 for (var chunkY = minChunkY; chunkY <= maxChunkY; chunkY++)
                 {
                     var chunkPosition = int2(chunkX, chunkY);
                     var chunkContainer = chunkContainers[chunkPosition];

                     // chunkContainer.RequiresRedraw = true;

                     // var cells = chunkContainer.Chunk.Cells;
                     
                     var northChunkContainer = chunkContainers.TryGetValue(chunkPosition + int2(0, 1));
                     var northEastChunkContainer = chunkContainers.TryGetValue(chunkPosition + int2(1, 1));
                     var eastChunkContainer = chunkContainers.TryGetValue(chunkPosition + int2(1, 0));
                     var southEastChunkContainer = chunkContainers.TryGetValue(chunkPosition + int2(1, -1));
                     var southChunkContainer = chunkContainers.TryGetValue(chunkPosition + int2(0, -1));
                     var southWestChunkContainer = chunkContainers.TryGetValue(chunkPosition + int2(-1, -1));
                     var westChunkContainer = chunkContainers.TryGetValue(chunkPosition + int2(-1, 0));
                     var northWestChunkContainer = chunkContainers.TryGetValue(chunkPosition + int2(-1, 1));

                     // Debug.Log(southWestChunkContainer?.Chunk.Cells == chunkContainer.Chunk.Cells);
                     // Debug.Log(chunkContainer.Chunk.Cells);
                     //
                     var a = new ChunkWithNeighbors
                     {
                         Chunk = chunkContainer.Chunk,
                         North = northChunkContainer?.Chunk ?? defaultChunk.Clone(Allocator.TempJob),
                         NorthEast = northEastChunkContainer?.Chunk ?? defaultChunk.Clone(Allocator.TempJob),
                         East = eastChunkContainer?.Chunk ?? defaultChunk.Clone(Allocator.TempJob),
                         SouthEast = southEastChunkContainer?.Chunk ?? defaultChunk.Clone(Allocator.TempJob),
                         South = southChunkContainer?.Chunk ?? defaultChunk.Clone(Allocator.TempJob),
                         SouthWest = southWestChunkContainer?.Chunk ?? defaultChunk.Clone(Allocator.TempJob),
                         West = westChunkContainer?.Chunk ?? defaultChunk.Clone(Allocator.TempJob),
                         NorthWest = northWestChunkContainer?.Chunk ?? defaultChunk.Clone(Allocator.TempJob)
                     };
                     
                     var job = new SomeJob(a, new Unity.Mathematics.Random((uint)random.Next()));
                     var jobHandle = job.Schedule();
                     jobHandle.Complete();
                     
                     
                     // job.ChunkWithNeighbors
                     if (job.NeedsRedraw)
                     {
                         Debug.Log("redraw");
                         chunkContainer.Chunk = job.ChunkWithNeighbors.Chunk;
                         chunkContainer.RequiresRedraw = true;

                         if (northChunkContainer != null)
                         {
                             northChunkContainer.Chunk = job.ChunkWithNeighbors.North;
                             northChunkContainer.RequiresRedraw = true;
                         }

                         if (northEastChunkContainer != null)
                         {
                             northEastChunkContainer.Chunk = job.ChunkWithNeighbors.NorthEast;
                             northEastChunkContainer.RequiresRedraw = true;
                         }

                         if (eastChunkContainer != null)
                         {
                             eastChunkContainer.Chunk = job.ChunkWithNeighbors.East;
                             eastChunkContainer.RequiresRedraw = true;
                         }
                         
                         if (southEastChunkContainer != null)
                         {
                             southEastChunkContainer.Chunk = job.ChunkWithNeighbors.SouthEast;
                             southEastChunkContainer.RequiresRedraw = true;
                         }

                         if (southChunkContainer != null)
                         {
                             southChunkContainer.Chunk = job.ChunkWithNeighbors.South;
                             southChunkContainer.RequiresRedraw = true;
                         }

                         if (southWestChunkContainer != null)
                         {
                             southWestChunkContainer.Chunk = job.ChunkWithNeighbors.SouthWest;
                             southWestChunkContainer.RequiresRedraw = true;
                         }

                         if (westChunkContainer != null)
                         {
                             westChunkContainer.Chunk = job.ChunkWithNeighbors.West;
                             westChunkContainer.RequiresRedraw = true;
                         }

                         if (northWestChunkContainer != null)
                         {
                             northWestChunkContainer.Chunk = job.ChunkWithNeighbors.NorthWest;
                             northWestChunkContainer.RequiresRedraw = true;
                         }

                     }

                     if (northChunkContainer == null)
                     {
                         job.ChunkWithNeighbors.North.Cells.Dispose();
                     }

                     if (northEastChunkContainer == null)
                     {
                         job.ChunkWithNeighbors.NorthEast.Cells.Dispose();
                     }

                     if (eastChunkContainer == null)
                     {
                         job.ChunkWithNeighbors.East.Cells.Dispose();
                     }

                     if (southEastChunkContainer == null)
                     {
                         job.ChunkWithNeighbors.SouthEast.Cells.Dispose();
                     }

                     if (southChunkContainer == null)
                     {
                         job.ChunkWithNeighbors.South.Cells.Dispose();
                     }

                     if (southWestChunkContainer == null)
                     {
                         job.ChunkWithNeighbors.SouthWest.Cells.Dispose();
                     }

                     if (westChunkContainer == null)
                     {
                         job.ChunkWithNeighbors.West.Cells.Dispose();
                     }

                     if (northWestChunkContainer == null)
                     {
                         job.ChunkWithNeighbors.NorthWest.Cells.Dispose();
                     }


                     // if (northChunkContainer)

                     // for (var x = 0; x < chunkSize; x++)
                     // {
                     //     for (var y = 0; y < chunkSize; y++)
                     //     {
                     //         var cellPosition = new Vector2Int(x, y);
                     //
                     //         // var cachedChunkContainers = new ChunkContainer[9];
                     //         
                     //         bool GetNeighbor(Vector2Int offset, out Vector2Int neighborChunkPosition, out Vector2Int neighborCellPosition)
                     //         {
                     //            var offsetedCellPosition = cellPosition + offset;
                     //            var chunk_offset = Vector2Int.FloorToInt(((Vector2) offsetedCellPosition) / new Vector2(chunkSize, chunkSize));
                     //            
                     //            var relative_target_cell = (offsetedCellPosition - (chunk_offset * 64));
                     //            var absolute_chunk_position = chunkPosition + chunk_offset;
                     //
                     //            if (chunkContainers.ContainsKey(absolute_chunk_position))
                     //            {
                     //                // cachedChunkContainers[(chunk_offset.x + 1) * ((chunk_offset.y + 1)* 64)] = ;
                     //                neighborCellPosition = relative_target_cell;
                     //                neighborChunkPosition = absolute_chunk_position;
                     //
                     //                return true;
                     //            }
                     //
                     //            neighborCellPosition = default;
                     //            neighborChunkPosition = default;
                     //            return false;
                     //         }
                     //         
                     //         Cell GetCellAtPosition(Chunk chunk, Vector2Int cellPosition)
                     //         {
                     //             return chunk.Cells[cellPosition.x + (cellPosition.y * chunkSize)];
                     //         }
                     //
                     //         var cell = cells[x + (y * chunkSize)];
                     //
                     //         if (cell.type == CellType.Sand)
                     //         {
                     //             bool TryMoveTo(Vector2Int offset)
                     //             {
                     //                 if (GetNeighbor(offset, out var neighborChunkPosition, out var neighborCellPosition))
                     //                 {
                     //                     var neighborChunk = chunkContainers[neighborChunkPosition];
                     //                     var neighborCell = GetCellAtPosition(neighborChunk.Chunk, neighborCellPosition);
                     //                     
                     //                     if (neighborCell.type == CellType.None)
                     //                     {
                     //                         chunkContainer.RequiresRedraw = true;
                     //                         neighborChunk.RequiresRedraw = true;
                     //                     
                     //                         cells[x + (y * 64)] = default;
                     //                         neighborChunk.Chunk.Cells[neighborCellPosition.x + (neighborCellPosition.y * chunkSize)] = cell;
                     //                         
                     //                         return true;
                     //                     }
                     //                 }
                     //
                     //                 return false;
                     //             }
                     //
                     //             var randomDirection = Random.value >= 0.5 ? -1 : 1;
                     //             if (TryMoveTo(new Vector2Int(0, -1))) {}
                     //             else if (TryMoveTo(new Vector2Int(randomDirection, -1))) {}
                     //             else if (TryMoveTo(new Vector2Int(-randomDirection, -1))) {}
                     //
                     //             // if (GetNeighbor(new Vector2Int(0, -1), out var neighborChunkPosition, out var neighborCellPosition))
                     //             // {
                     //             //     var neighborChunk = chunkContainers[neighborChunkPosition];
                     //             //     var neighborCell = GetCellAtPosition(neighborChunk.Chunk, neighborCellPosition);
                     //             //         
                     //             //     if (neighborCell.type == CellType.None)
                     //             //     {
                     //             //         chunkContainer.RequiresRedraw = true;
                     //             //         neighborChunk.RequiresRedraw = true;
                     //             //         
                     //             //         cells[x + (y * 64)] = default;
                     //             //         neighborChunk.Chunk.Cells[neighborCellPosition.x + (neighborCellPosition.y * 64)] = cell;
                     //             //         
                     //             //         chunkContainers[neighborChunkPosition] = neighborChunk;
                     //             //     }
                     //             // }
                     //         }
                     //     } 
                     // }
                     //
                     // chunkContainer.Chunk.Cells = cells;
                 }
             }
             
             yield return new WaitForSeconds(simulationStep);
         }
     }
}