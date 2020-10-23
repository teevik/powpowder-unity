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
    
    public const int ChunkSize = 128;
    
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private float simulationStep;
    [SerializeField] private MeshRenderer chunkPrefab;
    [SerializeField] private float pixelsPerUnit;

    private readonly Dictionary<int2, ChunkContainer> chunkContainers = new Dictionary<int2, ChunkContainer>();
    private int renderChunkKernelIndex;
    private ComputeBuffer cellTypeColorBuffer;
    
    private float ChunkScale => ChunkSize / pixelsPerUnit;

    private unsafe int SizeOfCell => sizeof(Cell);
    private unsafe int SizeOfColor => sizeof(Color);

    private void Start()
    {
        renderChunkKernelIndex = computeShader.FindKernel("render_chunk");

        cellTypeColorBuffer = new ComputeBuffer(4, SizeOfColor);
        cellTypeColorBuffer.SetData(new[]
        {
            Color.black,
            Color.gray, 
            Color.yellow, 
            Color.blue,  
        });
        
        computeShader.SetBuffer(renderChunkKernelIndex, "cell_type_colors", cellTypeColorBuffer);
        
        for (var x = -1; x < 2; x++)
        {
            for (var y = -1; y < 2; y++)
            {
                var chunkRenderer = Instantiate(chunkPrefab, transform, true);
                chunkRenderer.transform.position = ChunkScale * new Vector3(x, y);
                chunkRenderer.transform.localScale = new Vector3(ChunkScale, ChunkScale, 1);
                
                var outputRenderTexture = new RenderTexture(ChunkSize, ChunkSize, 32)
                {
                    enableRandomWrite = true,
                    useMipMap = false,
                    filterMode = FilterMode.Point
                };
                
                outputRenderTexture.Create();
                
                var chunk = new Chunk(Allocator.Persistent);

                var cellsComputeBuffer = new ComputeBuffer(chunk.Cells.Length, SizeOfCell);
                chunkRenderer.material.mainTexture = outputRenderTexture;
                

                for (var cellX = 0; cellX < ChunkSize; cellX++)
                {
                    for (var cellY = 0; cellY < ChunkSize; cellY++)
                    {
                        chunk.SetCell(int2(cellX, cellY), new Cell
                        {
                            type = (Random.value > 0.5) ? CellType.None : CellType.Sand
                        });
                    }
                }

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
        foreach (var chunkContainerPair in chunkContainers)
        {
            var chunkContainer = chunkContainerPair.Value;
            
            chunkContainer.Chunk.Dispose();
            chunkContainer.CellsComputeBuffer.Dispose();
        }
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            var worldCursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            var absoluteCellCursorPositionVector = Vector2Int.RoundToInt(transform.InverseTransformPoint(worldCursorPosition) * pixelsPerUnit);
            var absoluteCellCursorPosition = int2(absoluteCellCursorPositionVector.x, absoluteCellCursorPositionVector.y) + (ChunkSize / 2);
            
            for (var x = -2; x < 3; x++)
            {
                for (var y = -2; y < 3; y++)
                {
                    var offsetedCursorPosition = absoluteCellCursorPosition + int2(x, y);
                        
                    var chunkPosition = int2(floor(offsetedCursorPosition / (float2) ChunkSize));
                    var cellPosition = offsetedCursorPosition - (chunkPosition * ChunkSize);

                    if (chunkContainers.TryGetValue(chunkPosition, out var chunkContainer))
                    {
                        chunkContainer.Chunk.SetCell(cellPosition, new Cell
                        {
                            type = CellType.Sand
                        });
                        chunkContainer.RequiresRedraw = true;
                    }
                }
            }
        }
        
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
                    chunkContainer.CellsComputeBuffer.SetData(chunkContainer.Chunk.Cells);

                    computeShader.SetInt("chunk_size", ChunkSize);
                    computeShader.SetBuffer(renderChunkKernelIndex, "cells", chunkContainer.CellsComputeBuffer);
                    computeShader.SetTexture(renderChunkKernelIndex, "texture_out", chunkContainer.OutputRenderTexture);
                    
                    computeShader.Dispatch(renderChunkKernelIndex, ChunkSize / 8, ChunkSize / 8, 1);

                    chunkContainer.RequiresRedraw = false;
                }
            }
        }
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
        private ChunkWithNeighbors ChunkWithNeighbors;
        public NativeArray<bool> NeedsRedraw;
        private Unity.Mathematics.Random random;
        
        public SomeJob(ChunkWithNeighbors chunkWithNeighbors, NativeArray<bool> needsRedraw, Unity.Mathematics.Random random)
        {
            ChunkWithNeighbors = chunkWithNeighbors;
            NeedsRedraw = needsRedraw;
            this.random = random;
        }

        public void Execute()
        {
            var chunkWithNeighbors = ChunkWithNeighbors;
            var needsRedraw = false;
            
            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    var cellPosition = new int2(x, y);

                    // var cachedChunkContainers = new ChunkContainer[9];

                    bool GetNeighbor(int2 offset, out int2 neighborChunkPosition,
                        out int2 neighborCellPosition)
                    {
                        var offsetedCellPosition = cellPosition + offset;
                        var chunkOffset = int2(floor(float2(offsetedCellPosition) / float2(ChunkSize, ChunkSize)));
                        var relativeTargetCell = (offsetedCellPosition - (chunkOffset * ChunkSize));

                        var neighborChunk = chunkWithNeighbors.ChunkFromPosition(chunkOffset);

                        if (neighborChunk.IsOutOfBounds)
                        {
                            neighborChunkPosition = default;
                            neighborCellPosition = default;
                            return false;
                        }
                        
                        neighborChunkPosition = chunkOffset;
                        neighborCellPosition = relativeTargetCell;
                        return true;
                    }

                    var cell = chunkWithNeighbors.Chunk.GetCell(cellPosition);

                    if (cell.type == CellType.Sand)
                    {
                        bool TryMoveTo(int2 offset)
                        {
                            if (GetNeighbor(offset, out var neighborChunkPosition, out var neighborCellPosition))
                            {
                                var neighborChunk = chunkWithNeighbors.ChunkFromPosition(neighborChunkPosition);
                                var neighborCell = neighborChunk.GetCell(neighborCellPosition);
                    
                                if (neighborCell.type == CellType.None)
                                {
                                    needsRedraw = true;

                                    chunkWithNeighbors.Chunk.SetCell(cellPosition, default);
                                    neighborChunk.SetCell(neighborCellPosition, cell);
                                
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
                    }
                }
            }

            NeedsRedraw[0] = needsRedraw;
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

                     var chunkWithNeighbors = new ChunkWithNeighbors
                     {
                         Chunk = chunkContainer.Chunk,
                         North = northChunkContainer?.Chunk ?? Chunk.CreateOutOfBoundsChunk(Allocator.TempJob),
                         NorthEast = northEastChunkContainer?.Chunk ?? Chunk.CreateOutOfBoundsChunk(Allocator.TempJob),
                         East = eastChunkContainer?.Chunk ?? Chunk.CreateOutOfBoundsChunk(Allocator.TempJob),
                         SouthEast = southEastChunkContainer?.Chunk ?? Chunk.CreateOutOfBoundsChunk(Allocator.TempJob),
                         South = southChunkContainer?.Chunk ?? Chunk.CreateOutOfBoundsChunk(Allocator.TempJob),
                         SouthWest = southWestChunkContainer?.Chunk ?? Chunk.CreateOutOfBoundsChunk(Allocator.TempJob),
                         West = westChunkContainer?.Chunk ?? Chunk.CreateOutOfBoundsChunk(Allocator.TempJob),
                         NorthWest = northWestChunkContainer?.Chunk ?? Chunk.CreateOutOfBoundsChunk(Allocator.TempJob)
                     };
                     
                     var resultNeedsRedraw = new NativeArray<bool>(1, Allocator.TempJob);
                     
                     var job = new SomeJob(chunkWithNeighbors, resultNeedsRedraw, new Unity.Mathematics.Random((uint)random.Next()));
                     var jobHandle = job.Schedule();
                     jobHandle.Complete();
                     
                     
                     if (resultNeedsRedraw[0])
                     {
                         chunkContainer.RequiresRedraw = true;

                         if (northChunkContainer != null)
                         {
                             northChunkContainer.RequiresRedraw = true;
                         }

                         if (northEastChunkContainer != null)
                         {
                             northEastChunkContainer.RequiresRedraw = true;
                         }

                         if (eastChunkContainer != null)
                         {
                             eastChunkContainer.RequiresRedraw = true;
                         }
                         
                         if (southEastChunkContainer != null)
                         {
                             southEastChunkContainer.RequiresRedraw = true;
                         }

                         if (southChunkContainer != null)
                         {
                             southChunkContainer.RequiresRedraw = true;
                         }

                         if (southWestChunkContainer != null)
                         {
                             southWestChunkContainer.RequiresRedraw = true;
                         }

                         if (westChunkContainer != null)
                         {
                             westChunkContainer.RequiresRedraw = true;
                         }

                         if (northWestChunkContainer != null)
                         {
                             northWestChunkContainer.RequiresRedraw = true;
                         }

                     }

                     if (northChunkContainer == null)
                     {
                         chunkWithNeighbors.North.Dispose();
                     }

                     if (northEastChunkContainer == null)
                     {
                         chunkWithNeighbors.NorthEast.Dispose();
                     }

                     if (eastChunkContainer == null)
                     {
                         chunkWithNeighbors.East.Dispose();
                     }

                     if (southEastChunkContainer == null)
                     {
                         chunkWithNeighbors.SouthEast.Dispose();
                     }

                     if (southChunkContainer == null)
                     {
                         chunkWithNeighbors.South.Dispose();
                     }

                     if (southWestChunkContainer == null)
                     {
                         chunkWithNeighbors.SouthWest.Dispose();
                     }

                     if (westChunkContainer == null)
                     {
                         chunkWithNeighbors.West.Dispose();
                     }

                     if (northWestChunkContainer == null)
                     {
                         chunkWithNeighbors.NorthWest.Dispose();
                     }

                     resultNeedsRedraw.Dispose();
                 }
             }
             
             yield return new WaitForSeconds(simulationStep);
         }
     }

    private void OnDrawGizmos()
    {
        foreach (var chunk in chunkContainers)
        {
            var chunkPosition = chunk.Key;
            
            Gizmos.DrawWireCube(new Vector3(chunkPosition.x, chunkPosition.y) * ChunkScale, new Vector3(ChunkScale, ChunkScale));
        }
    }
}