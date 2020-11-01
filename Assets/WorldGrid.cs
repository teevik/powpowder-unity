using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

public struct ChunkCollectionWithNeighbors
{
    public ChunkContainer Chunk;
    public ChunkContainer? North;
    public ChunkContainer? NorthEast;
    public ChunkContainer? East;
    public ChunkContainer? SouthEast;
    public ChunkContainer? South;
    public ChunkContainer? SouthWest;
    public ChunkContainer? West;
    public ChunkContainer? NorthWest;

    public readonly ChunkContainer? ChunkFromPosition(int2 position)
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
        
    public void SetChunkAtPosition(int2 position, ChunkContainer chunk)
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

public struct ChunkWithNeighbors
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

public class ChunkContainer
{
    public Chunk Chunk;
    public MeshRenderer Renderer;
    public ComputeBuffer CellsComputeBuffer;
    public RenderTexture OutputRenderTexture;
    public bool RequiresRedraw;
}

public class WorldGrid : MonoBehaviour
{
    public const int ChunkSize = 128;
    
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private float simulationStep;
    [SerializeField] private MeshRenderer chunkPrefab;
    [SerializeField] private float pixelsPerUnit;
    [Min(1)]
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
    
    [BurstCompile]
    private struct SomeJob : IJob
    {
        private ChunkWithNeighbors chunkWithNeighbors;
        private NativeArray<bool> needsRedraw;
        // private bool2 isEvenOnAxis;
        private readonly uint frameCount;
        private Unity.Mathematics.Random random;
        
        public SomeJob(ChunkWithNeighbors chunkWithNeighbors, NativeArray<bool> needsRedraw, /*bool2 isEvenOnAxis,*/ uint frameCount, Unity.Mathematics.Random random)
        {
            this.chunkWithNeighbors = chunkWithNeighbors;
            this.needsRedraw = needsRedraw;
            // this.isEvenOnAxis = isEvenOnAxis;
            this.frameCount = frameCount;
            this.random = random;
        }

        public void Execute()
        {
            var needsRedraw = false;

            var sandCellImplementation = new SandCellImplementation();
            var waterCellImplementation = new WaterCellImplementation();

            var shouldGoReverse = frameCount % 2 == 0;
            // var shouldGoReverse2 = frameCount % 2 == 0;
            // var shouldGoReverse = random.NextBool();
            // var shouldGoReverse2 = random.NextBool();

            for (var x = shouldGoReverse ? ChunkSize - 1 : 0; shouldGoReverse ? x >= 0 : x < ChunkSize; x += (shouldGoReverse ? -1 : 1))
            {
                // for (var y = 0; y < ChunkSize; y++)
                for (var y = ChunkSize - 1; y >= 0; y--)
                // for (var y = shouldGoReverse2 ? ChunkSize - 1 : 0; shouldGoReverse2 ? y >= 0 : y < ChunkSize; y += (shouldGoReverse2 ? -1 : 1))
                {
                    var cellPosition = new int2(x, y);

                    var cell = chunkWithNeighbors.Chunk.GetCell(cellPosition);
                    if (cell.clock == frameCount) continue;
                    
                    cell.clock = frameCount;
                    chunkWithNeighbors.Chunk.SetCell(cellPosition, cell);
                    
                    if (cell.type == CellType.Sand)
                    {
                        if (sandCellImplementation.Update(chunkWithNeighbors, cellPosition, random))
                        {
                            needsRedraw = true;
                        }
                    }
                    else if (cell.type == CellType.Water)
                    {
                        if (waterCellImplementation.Update(chunkWithNeighbors, cellPosition, random))
                        {
                            needsRedraw = true;
                        }
                    }
                }
            }

            this.needsRedraw[0] = needsRedraw;
        }
    }
    
    private IEnumerator UpdateWorldCoroutine()
     {
         var random = new System.Random();

         while (true)
         {
            // Could refactor to have one loop
             var minChunkX = chunkContainers.Min(a => a.Key.x);
             var minChunkY = chunkContainers.Min(a => a.Key.y);
             var maxChunkX = chunkContainers.Max(a => a.Key.x);
             var maxChunkY = chunkContainers.Max(a => a.Key.y);


             for (int i = 0; i < 2; i++)
             {
                 for (int j = 0; j < 2; j++)
                 {
                     // JobHandle? jobHandle = null;
                     var jobHandles = new List<JobHandle>();
                     var 
                     
                     for (var chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
                     {
                         for (var chunkY = minChunkY; chunkY <= maxChunkY; chunkY++)
                         {
                             if (((chunkX % 2) == i) && ((chunkY % 2) == j)) continue;
                             
                             var chunkPosition = int2(chunkX, chunkY);
                             var chunkContainer = chunkContainers[chunkPosition];
                             
                             var chunkContainersWithNeighbors = new ChunkCollectionWithNeighbors
                             {
                                 Chunk = chunkContainer,
                                 North = chunkContainers.TryGetValue(chunkPosition + int2(0, 1)),
                                 NorthEast = chunkContainers.TryGetValue(chunkPosition + int2(1, 1)),
                                 East = chunkContainers.TryGetValue(chunkPosition + int2(1, 0)),
                                 SouthEast = chunkContainers.TryGetValue(chunkPosition + int2(1, -1)),
                                 South = chunkContainers.TryGetValue(chunkPosition + int2(0, -1)),
                                 SouthWest = chunkContainers.TryGetValue(chunkPosition + int2(-1, -1)),
                                 West = chunkContainers.TryGetValue(chunkPosition + int2(-1, 0)),
                                 NorthWest = chunkContainers.TryGetValue(chunkPosition + int2(-1, 1)),
                             };

                             var chunkWithNeighbors = new ChunkWithNeighbors
                             {
                                 Chunk = chunkContainer.Chunk,
                                 North = chunkContainersWithNeighbors.North?.Chunk ?? Chunk.CreateOutOfBoundsChunk(Allocator.TempJob),
                                 NorthEast = chunkContainersWithNeighbors.NorthEast?.Chunk ?? Chunk.CreateOutOfBoundsChunk(Allocator.TempJob),
                                 East = chunkContainersWithNeighbors.East?.Chunk ?? Chunk.CreateOutOfBoundsChunk(Allocator.TempJob),
                                 SouthEast = chunkContainersWithNeighbors.SouthEast?.Chunk ?? Chunk.CreateOutOfBoundsChunk(Allocator.TempJob),
                                 South = chunkContainersWithNeighbors.South?.Chunk ?? Chunk.CreateOutOfBoundsChunk(Allocator.TempJob),
                                 SouthWest = chunkContainersWithNeighbors.SouthWest?.Chunk ?? Chunk.CreateOutOfBoundsChunk(Allocator.TempJob),
                                 West = chunkContainersWithNeighbors.West?.Chunk ?? Chunk.CreateOutOfBoundsChunk(Allocator.TempJob),
                                 NorthWest = chunkContainersWithNeighbors.NorthWest?.Chunk ?? Chunk.CreateOutOfBoundsChunk(Allocator.TempJob)
                             };
                             
                             var resultNeedsRedraw = new NativeArray<bool>(1, Allocator.TempJob);
                             
                             var job = new SomeJob(chunkWithNeighbors, resultNeedsRedraw, (uint) Time.frameCount, new Unity.Mathematics.Random((uint)random.Next()));
                             var jobHandle = job.Schedule();
                             jobHandles.Add(jobHandle);
                             // if (jobHandle == null) jobHandle = newJobHandle;
                             // else jobHandle = JobHandle.CombineDependencies(jobHandle.Value, newJobHandle);

                             // var newJobHandle = job.Schedule();
                             // if (jobHandle == null) jobHandle = newJobHandle;
                             // else jobHandle = JobHandle.CombineDependencies(jobHandle.Value, newJobHandle);

                             
                             // if (resultNeedsRedraw[0])
                             // {
                             //     chunkContainer.RequiresRedraw = true;
                             //
                             //     if (northChunkContainer != null)
                             //     {
                             //         northChunkContainer.RequiresRedraw = true;
                             //     }
                             //
                             //     if (northEastChunkContainer != null)
                             //     {
                             //         northEastChunkContainer.RequiresRedraw = true;
                             //     }
                             //
                             //     if (eastChunkContainer != null)
                             //     {
                             //         eastChunkContainer.RequiresRedraw = true;
                             //     }
                             //     
                             //     if (southEastChunkContainer != null)
                             //     {
                             //         southEastChunkContainer.RequiresRedraw = true;
                             //     }
                             //
                             //     if (southChunkContainer != null)
                             //     {
                             //         southChunkContainer.RequiresRedraw = true;
                             //     }
                             //
                             //     if (southWestChunkContainer != null)
                             //     {
                             //         southWestChunkContainer.RequiresRedraw = true;
                             //     }
                             //
                             //     if (westChunkContainer != null)
                             //     {
                             //         westChunkContainer.RequiresRedraw = true;
                             //     }
                             //
                             //     if (northWestChunkContainer != null)
                             //     {
                             //         northWestChunkContainer.RequiresRedraw = true;
                             //     }
                             //
                             // }
                             //
                             // if (northChunkContainer == null)
                             // {
                             //     chunkWithNeighbors.North.Dispose();
                             // }
                             //
                             // if (northEastChunkContainer == null)
                             // {
                             //     chunkWithNeighbors.NorthEast.Dispose();
                             // }
                             //
                             // if (eastChunkContainer == null)
                             // {
                             //     chunkWithNeighbors.East.Dispose();
                             // }
                             //
                             // if (southEastChunkContainer == null)
                             // {
                             //     chunkWithNeighbors.SouthEast.Dispose();
                             // }
                             //
                             // if (southChunkContainer == null)
                             // {
                             //     chunkWithNeighbors.South.Dispose();
                             // }
                             //
                             // if (southWestChunkContainer == null)
                             // {
                             //     chunkWithNeighbors.SouthWest.Dispose();
                             // }
                             //
                             // if (westChunkContainer == null)
                             // {
                             //     chunkWithNeighbors.West.Dispose();
                             // }
                             //
                             // if (northWestChunkContainer == null)
                             // {
                             //     chunkWithNeighbors.NorthWest.Dispose();
                             // }

                             // resultNeedsRedraw.Dispose();
                         }
                     }
                     
                     // if (jobHandle != null) jobHandle.Value.Complete();

                     var a = new NativeArray<JobHandle>(jobHandles.ToArray(), Allocator.Temp);
                     JobHandle.CompleteAll(a);
                     a.Dispose();
                     // if (jobHandle != null) jobHandle.Value.Complete();
                     
                     yield return new WaitForSeconds(simulationStep);
                 }
                 }
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