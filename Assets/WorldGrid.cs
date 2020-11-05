using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Drawing;
using Extensions;
using Sirenix.OdinInspector;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;
using static Unity.Mathematics.math;
using int2 = Unity.Mathematics.int2;

public struct ValueWithNeighbors<T>
{
    public T Value;
    public T North;
    public T NorthEast;
    public T East;
    public T SouthEast;
    public T South;
    public T SouthWest;
    public T West;
    public T NorthWest;

    public readonly T ChunkFromPosition(int2 position)
    {
        if (position.Equals(int2(0, 0))) return Value;
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
        
    public void SetChunkAtPosition(int2 position, T chunk)
    {
        if (position.Equals(int2(0, 0))) Value = chunk;
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

public class ChunkContainer
{
    public Chunk Chunk;
    public MeshRenderer Renderer;
    public ComputeBuffer CellsComputeBuffer;
    public RenderTexture OutputRenderTexture;
}

public class WorldGrid : MonoBehaviour
{
    public const int ChunkSize = 64;
    
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private float simulationStep;
    [SerializeField] private MeshRenderer chunkPrefab;
    [SerializeField] private float pixelsPerUnit;
    [SerializeField] private Vector2Int chunkAmountRadius = Vector2Int.one;

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
        
        for (var x = -chunkAmountRadius.x + 1; x < chunkAmountRadius.x; x++)
        {
            for (var y = -chunkAmountRadius.y + 1; y < chunkAmountRadius.y; y++)
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
                        chunk.SetCell(int2(cellX, cellY), (Random.value > 0.5f) ? Cell.EmptyCell : new Cell
                        {
                            type = CellType.Sand,
                            color = Color.yellow
                        });
                    }
                }

                var chunkContainer = new ChunkContainer
                {
                    Chunk = chunk,
                    Renderer = chunkRenderer,
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
        cellTypeColorBuffer.Dispose();
        
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
                            type = CellType.Water
                        });
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

                chunkContainer.CellsComputeBuffer.SetData(chunkContainer.Chunk.Cells);

                computeShader.SetInt("chunk_size", ChunkSize);
                computeShader.SetBuffer(renderChunkKernelIndex, "cells", chunkContainer.CellsComputeBuffer);
                computeShader.SetTexture(renderChunkKernelIndex, "texture_out", chunkContainer.OutputRenderTexture);
                
                computeShader.Dispatch(renderChunkKernelIndex, ChunkSize / 8, ChunkSize / 8, 1);
            }
        }
    }
    
    [BurstCompile]
    private struct UpdateChunkJob : IJob
    {
        private ValueWithNeighbors<Chunk> chunkWithNeighbors;
        private readonly uint frameCount;
        private Unity.Mathematics.Random random;
        private CommandBuilder drawingCommands;
        private float2 chunkPosition;
        private float chunkScale;
        
        public UpdateChunkJob(ValueWithNeighbors<Chunk> chunkWithNeighbors, uint frameCount, Unity.Mathematics.Random random, CommandBuilder drawingCommands, float2 chunkPosition, float chunkScale)
        {
            this.chunkWithNeighbors = chunkWithNeighbors;
            this.frameCount = frameCount;
            this.random = random;
            this.drawingCommands = drawingCommands;
            this.chunkPosition = chunkPosition;
            this.chunkScale = chunkScale;
        }

        public void Execute()
        {
            var sandCellImplementation = new SandCellImplementation();
            var waterCellImplementation = new WaterCellImplementation();

            var shouldGoReverse = frameCount % 2 == 0;
            // var shouldGoReverse2 = frameCount % 2 == 0;
            // var shouldGoReverse = random.NextBool();
            // var shouldGoReverse2 = random.NextBool();

            var hasDirtyRect = false;
            int? minCellPositionX = null;
            int? minCellPositionY = null;
            int? maxCellPositionX = null;
            int? maxCellPositionY = null;

            void CheckForDirtyRect(Cell cell, int2 cellPosition)
            {
                if (cell.isStale) return;
                
                hasDirtyRect = true;

                if (!minCellPositionX.HasValue || cellPosition.x < minCellPositionX.Value)
                {
                    minCellPositionX = cellPosition.x;
                }
                    
                if (!minCellPositionY.HasValue || cellPosition.y < minCellPositionY.Value)
                {
                    minCellPositionY = cellPosition.y;
                }
                    
                if (!maxCellPositionX.HasValue || cellPosition.x > maxCellPositionX.Value)
                {
                    maxCellPositionX = cellPosition.x;
                }
                    
                if (!maxCellPositionY.HasValue || cellPosition.y < maxCellPositionY.Value)
                {
                    maxCellPositionY = cellPosition.y;
                }
            }

            for (var chunkX = -1; chunkX < 2; chunkX++)
            {
                for (var chunkY = -1; chunkY < 2; chunkY++)
                {
                    if (chunkX == 0 && chunkY == 0) continue;
                    
                    var chunkPosition = int2(chunkX, chunkY);
                    var chunk = chunkWithNeighbors.ChunkFromPosition(chunkPosition);
                    
                    if (chunk.IsOutOfBounds) continue;

                    if (chunkX == 1 && chunkY == 1)
                    {
                        var cell = chunk.GetCell(int2(0, 0));
                        CheckForDirtyRect(cell , int2(0, 0));
                    }
                    else if (chunkX == 1 && chunkY == -1)
                    {
                        var cell = chunk.GetCell(int2(0, ChunkSize - 1));
                        CheckForDirtyRect(cell , int2(0, ChunkSize - 1));
                    }
                    else if (chunkX == -1 && chunkY == 1)
                    {
                        var cell = chunk.GetCell(int2(ChunkSize - 1, 0));
                        CheckForDirtyRect(cell , int2(ChunkSize - 1, 0));
                    }
                    else if (chunkX == -1 && chunkY == -1)
                    {
                        var cell = chunk.GetCell(int2(ChunkSize - 1, ChunkSize - 1));
                        CheckForDirtyRect(cell , int2(ChunkSize - 1, ChunkSize - 1));
                    } 
                    else if (chunkY == -1)
                    {
                        for (var x = 0; x < ChunkSize; x++)
                        {
                            var cell = chunk.GetCell(int2(x, ChunkSize - 1));
                            CheckForDirtyRect(cell , int2(x, ChunkSize - 1));
                        }
                    }
                    else if (chunkY == 1)
                    {
                        for (var x = 0; x < ChunkSize; x++)
                        {
                            var cell = chunk.GetCell(int2(x, 0));
                            CheckForDirtyRect(cell , int2(x, 0));
                        }
                    }
                    else if (chunkX == -1)
                    {
                        for (var y = 0; y < ChunkSize; y++)
                        {
                            var cell = chunk.GetCell(int2(ChunkSize - 1, y));
                            CheckForDirtyRect(cell , int2(ChunkSize - 1, y));
                        }
                    }
                    else if (chunkX == 1)
                    {
                        for (var y = 0; y < ChunkSize; y++)
                        {
                            var cell = chunk.GetCell(int2(0, y));
                            CheckForDirtyRect(cell , int2(0, y));
                        }
                    }
                }
            }

            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    var cellPosition = new int2(x, y);
                    var cell = chunkWithNeighbors.Value.GetCell(cellPosition);
                    
                    if (cell.isStale) continue;
                    hasDirtyRect = true;

                    if (!minCellPositionX.HasValue || cellPosition.x < minCellPositionX.Value)
                    {
                        minCellPositionX = cellPosition.x;
                    }
                    
                    if (!minCellPositionY.HasValue || cellPosition.y < minCellPositionY.Value)
                    {
                        minCellPositionY = cellPosition.y;
                    }
                    
                    if (!maxCellPositionX.HasValue || cellPosition.x > maxCellPositionX.Value)
                    {
                        maxCellPositionX = cellPosition.x;
                    }
                    
                    if (!maxCellPositionY.HasValue || cellPosition.y > maxCellPositionY.Value)
                    {
                        maxCellPositionY = cellPosition.y;
                    }
                }
            }


            if (!hasDirtyRect) return;

            var minCellPosition = max(int2(minCellPositionX.Value, minCellPositionY.Value) - int2(1), int2(0));
            var maxCellPosition = min(int2(maxCellPositionX.Value, maxCellPositionY.Value) + int2(1), int2(ChunkSize));

            var minCellPercentage = (float2)minCellPosition / ChunkSize;
            var maxCellPercentage = (float2)maxCellPosition / ChunkSize;

            if (chunkPosition.Equals(float2(0f)))
            {
                Debug.Log(minCellPosition);
                Debug.Log(maxCellPosition);
            }
            
            var a = new Rect(
                chunkPosition.x - (chunkScale / 2) + (chunkScale * minCellPercentage.x), 
                chunkPosition.y - (chunkScale / 2) + (chunkScale * minCellPercentage.y), 
                chunkScale - (chunkScale * maxCellPercentage.x) - (chunkScale * minCellPercentage.x), 
                chunkScale - (chunkScale * maxCellPercentage.y) - (chunkScale * minCellPercentage.y)
            );
            
            drawingCommands.WireRectangle(a, Color.red);
            
            // for (var x = shouldGoReverse ? ChunkSize - 1 : 0; shouldGoReverse ? x >= 0 : x < ChunkSize; x += (shouldGoReverse ? -1 : 1))
            for (var x = shouldGoReverse ? maxCellPosition.x - 1 : minCellPosition.x; shouldGoReverse ? x >= minCellPosition.x : x < maxCellPosition.x - 1; x += (shouldGoReverse ? -1 : 1))
            {
                // for (var y = 0; y < ChunkSize; y++)
                for (var y = maxCellPosition.y - 1; y >= minCellPosition.y; y--)
                // for (var y = ChunkSize - 1; y >= 0; y--)
                    // for (var y = shouldGoReverse2 ? ChunkSize - 1 : 0; shouldGoReverse2 ? y >= 0 : y < ChunkSize; y += (shouldGoReverse2 ? -1 : 1))
                {
                    var cellPosition = new int2(x, y);

                    var cell = chunkWithNeighbors.Value.GetCell(cellPosition);
                    if (cell.type == CellType.None) continue;
                    if (cell.clock == frameCount) continue;
                    
                    cell.clock = frameCount;
                    chunkWithNeighbors.Value.SetCell(cellPosition, cell);
                    
                    if (cell.type == CellType.Sand)
                    {
                        cell.isStale = false;
                        
                        if (sandCellImplementation.Update(chunkWithNeighbors, cellPosition, random))
                        {
                        }
                        else
                        {
                            // cell.isStale = true;
                            chunkWithNeighbors.Value.SetCell(cellPosition, cell);
                        }
                    }
                    else if (cell.type == CellType.Water)
                    {
                        cell.isStale = false;

                        if (waterCellImplementation.Update(chunkWithNeighbors, cellPosition, random))
                        {
                        }
                        else
                        {
                            // cell.isStale = true;
                            chunkWithNeighbors.Value.SetCell(cellPosition, cell);
                        }
                    }
                }
            }
        }
    }
    
    private readonly Chunk outOfBoundsChunk = Chunk.CreateOutOfBoundsChunk(Allocator.Persistent);
    
    private IEnumerator UpdateWorldCoroutine()
     {
         using var drawingCommands = DrawingManager.GetBuilder();

         var random = new System.Random();

         using (drawingCommands.InLocalSpace(transform))
         {
             while (true)
             {
                 var minChunkX = chunkContainers.Min(a => a.Key.x);
                 var minChunkY = chunkContainers.Min(a => a.Key.y);
                 var maxChunkX = chunkContainers.Max(a => a.Key.x);
                 var maxChunkY = chunkContainers.Max(a => a.Key.y);


                 for (int i = 0; i < 2; i++)
                 {
                     for (int j = 0; j < 2; j++)
                     {
                         JobHandle? jobHandle = null;
                         
                         for (var chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
                         {
                             for (var chunkY = minChunkY; chunkY <= maxChunkY; chunkY++)
                             {
                                 if (((chunkX % 2) == i) && ((chunkY % 2) == j)) continue;

                                 var chunkPosition = int2(chunkX, chunkY);
                                 var chunkContainer = chunkContainers[chunkPosition];

                                 var chunkContainersWithNeighbors = new ValueWithNeighbors<ChunkContainer?>
                                 {
                                     Value = chunkContainer,
                                     North = chunkContainers.TryGetValue(chunkPosition + int2(0, 1)),
                                     NorthEast = chunkContainers.TryGetValue(chunkPosition + int2(1, 1)),
                                     East = chunkContainers.TryGetValue(chunkPosition + int2(1, 0)),
                                     SouthEast = chunkContainers.TryGetValue(chunkPosition + int2(1, -1)),
                                     South = chunkContainers.TryGetValue(chunkPosition + int2(0, -1)),
                                     SouthWest = chunkContainers.TryGetValue(chunkPosition + int2(-1, -1)),
                                     West = chunkContainers.TryGetValue(chunkPosition + int2(-1, 0)),
                                     NorthWest = chunkContainers.TryGetValue(chunkPosition + int2(-1, 1)),
                                 };
                                 
                                 var a = new ValueWithNeighbors<Chunk?>
                                 {
                                     Value = null,
                                     North = 
                                 };

                                 var chunkWithNeighbors = new ValueWithNeighbors<Chunk>
                                 {
                                     Value = chunkContainer.Chunk,
                                     North = chunkContainersWithNeighbors.North?.Chunk ??
                                             Chunk.CreateOutOfBoundsChunk(Allocator.TempJob),
                                     NorthEast = chunkContainersWithNeighbors.NorthEast?.Chunk ??
                                                 Chunk.CreateOutOfBoundsChunk(Allocator.TempJob),
                                     East = chunkContainersWithNeighbors.East?.Chunk ??
                                            Chunk.CreateOutOfBoundsChunk(Allocator.TempJob),
                                     SouthEast = chunkContainersWithNeighbors.SouthEast?.Chunk ??
                                                 Chunk.CreateOutOfBoundsChunk(Allocator.TempJob),
                                     South = chunkContainersWithNeighbors.South?.Chunk ??
                                             Chunk.CreateOutOfBoundsChunk(Allocator.TempJob),
                                     SouthWest = chunkContainersWithNeighbors.SouthWest?.Chunk ??
                                                 Chunk.CreateOutOfBoundsChunk(Allocator.TempJob),
                                     West = chunkContainersWithNeighbors.West?.Chunk ??
                                            Chunk.CreateOutOfBoundsChunk(Allocator.TempJob),
                                     NorthWest = chunkContainersWithNeighbors.NorthWest?.Chunk ??
                                                 Chunk.CreateOutOfBoundsChunk(Allocator.TempJob)
                                 };
                                 
                                 var job = new UpdateChunkJob(chunkWithNeighbors, (uint) Time.frameCount,
                                     new Unity.Mathematics.Random((uint) random.Next()), drawingCommands,
                                     (float2) chunkPosition * ChunkScale, ChunkScale);
                                 var newJobHandle = job.Schedule();

                                 if (jobHandle == null) jobHandle = newJobHandle;
                                 else jobHandle = JobHandle.CombineDependencies(jobHandle.Value, newJobHandle);
                             }
                         }

                         if (jobHandle != null) jobHandle.Value.Complete();

                         yield return new WaitForSeconds(simulationStep);
                     }
                 }
             }
         }
     }

    private void OnDrawGizmos()
    {
        for (var x = -chunkAmountRadius.x + 1; x < chunkAmountRadius.x; x++)
        {
            for (var y = -chunkAmountRadius.y + 1; y < chunkAmountRadius.y; y++)
            {
                Gizmos.DrawWireCube(new Vector3(x, y) * ChunkScale, new Vector3(ChunkScale, ChunkScale));
            }
        }
    }
}