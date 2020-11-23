using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataTypes;
using Drawing;
using Extensions;
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

public class ChunkContainer : IDisposable
{
    public Chunk Chunk;
    public ChunkBehaviour ChunkBehaviour;
    public ComputeBuffer CellsComputeBuffer;
    public RenderTexture OutputRenderTexture;

    public void Dispose()
    {
        Chunk.Dispose();
        CellsComputeBuffer.Dispose();
        GameObject.Destroy(ChunkBehaviour.gameObject);
    }
}

public class WorldGrid : MonoBehaviour
{
    public const int ChunkSize = 64;
    
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private float simulationStep;
    [SerializeField] private ChunkBehaviour chunkPrefab;
    [SerializeField] private float pixelsPerUnit;
    
    private readonly Dictionary<int2, ChunkContainer> allChunks = new Dictionary<int2, ChunkContainer>();
    private readonly Map<int2, ChunkContainer> loadedChunks = new Map<int2, ChunkContainer>();
    private int renderChunkKernelIndex;
    private ComputeBuffer cellTypeColorBuffer;
    private Unity.Mathematics.Random random;
    
    private float ChunkScale => ChunkSize / pixelsPerUnit;

    private unsafe int SizeOfCell => sizeof(Cell);
    private unsafe int SizeOfColor => sizeof(Color);

    public IEnumerable<int2> LoadedChunkPositions => loadedChunks.Keys;

    public bool ChunkIsLoaded(int2 chunkPosition)
    {
        return loadedChunks.ContainsKey(chunkPosition);
    }
    
    public void LoadChunk(int2 chunkPosition)
    {
        if (allChunks.TryGetValue(chunkPosition, out var existingChunkContainer))
        {
            loadedChunks.Add(chunkPosition, existingChunkContainer);
            
            return;
        }
        
        var chunkRenderer = Instantiate(chunkPrefab, transform, true);
        chunkRenderer.transform.position = ChunkScale * new Vector3(chunkPosition.x, chunkPosition.y);
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
        chunkRenderer.SetTexture(outputRenderTexture);

        if (chunkPosition.y == 0)
        {
            for (var cellX = 0; cellX < ChunkSize; cellX++)
            {
                for (var cellY = 0; cellY < ChunkSize; cellY++)
                {
                    chunk.SetCell(int2(cellX, cellY), (Random.value > 0.5f) ? Cell.EmptyCell : SandCellImplementation.CreateSandCell(ref random));
                }
            }
        }
        else if (chunkPosition.y < 0)
        {
            for (var cellX = 0; cellX < ChunkSize; cellX++)
            {
                for (var cellY = 0; cellY < ChunkSize; cellY++)
                {
                    chunk.SetCell(int2(cellX, cellY), SandCellImplementation.CreateSandCell(ref random));
                }
            }
        }
    
        var chunkContainer = new ChunkContainer
        {
            Chunk = chunk,
            ChunkBehaviour = chunkRenderer,
            CellsComputeBuffer = cellsComputeBuffer,
            OutputRenderTexture = outputRenderTexture
        };
    
        allChunks.Add(chunkPosition, chunkContainer);
        loadedChunks.Add(chunkPosition, chunkContainer);
    }

    public void UnloadChunk(int2 chunkPosition)
    {
        if (loadedChunks.TryGetValue(chunkPosition, out var chunkContainer))
        {
            loadedChunks.Remove(chunkPosition);
        }
    }
    
    private void Awake()
    {
        random = new Unity.Mathematics.Random((uint) new System.Random().Next());
    }

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
        
        StartCoroutine(UpdateWorldCoroutine());
    }

    private void OnDestroy()
    {
        cellTypeColorBuffer.Dispose();
        
        foreach (var chunkContainerPair in loadedChunks)
        {
            var chunkContainer = chunkContainerPair.Value;
            
            chunkContainer.Dispose();
        }
    }

    public int2 ChunkPosition(Vector2 worldPosition)
    {
        var absoluteCellCursorPositionVector = Vector2Int.RoundToInt(transform.InverseTransformPoint(worldPosition) * pixelsPerUnit);
        var absoluteCellCursorPosition = int2(absoluteCellCursorPositionVector.x, absoluteCellCursorPositionVector.y) + (ChunkSize / 2);
        
        var chunkPosition = int2(floor(absoluteCellCursorPosition / (float2) ChunkSize));
        // var cellPosition = offsetedCursorPosition - (chunkPosition * ChunkSize);

        return chunkPosition;
    }

    private void Update()
    {
        if (loadedChunks.Count == 0) return;
        
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

                    if (loadedChunks.TryGetValue(chunkPosition, out var chunkContainer))
                    {
                        chunkContainer.Chunk.SetCell(cellPosition, WaterCellImplementation.CreateWaterCell(ref random));
                    }
                }
            }
        }
        
        var minX = loadedChunks.Min(a => a.Key.x);
        var minY = loadedChunks.Min(a => a.Key.y);
        var maxX = loadedChunks.Max(a => a.Key.x);
        var maxY = loadedChunks.Max(a => a.Key.y);

        
        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++)
            {
                var chunkPosition = int2(x, y);
                if (!loadedChunks.TryGetValue(chunkPosition, out var chunkContainer)) continue;

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
        private readonly float chunkScale;
        private readonly float simulationStep;
        
        public UpdateChunkJob(ValueWithNeighbors<Chunk> chunkWithNeighbors, uint frameCount, Unity.Mathematics.Random random, CommandBuilder drawingCommands, float2 chunkPosition, float chunkScale, float simulationStep)
        {
            this.chunkWithNeighbors = chunkWithNeighbors;
            this.frameCount = frameCount;
            this.random = random;
            this.drawingCommands = drawingCommands;
            this.chunkPosition = chunkPosition;
            this.chunkScale = chunkScale;
            this.simulationStep = simulationStep;
        }

        public void Execute()
        {
            var sandCellImplementation = new SandCellImplementation();
            var waterCellImplementation = new WaterCellImplementation();

            var shouldGoReverse = frameCount % 2 == 0;
            // var shouldGoReverse2 = frameCount % 2 == 0;
            // var shouldGoReverse = random.NextBool();
            // var shouldGoReverse2 = random.NextBool();

            // var hasDirtyRect = false;
            // int? minCellPositionX = null;
            // int? minCellPositionY = null;
            // int? maxCellPositionX = null;
            // int? maxCellPositionY = null;
            //
            // void CheckForDirtyRect(Cell cell, int2 cellPosition)
            // {
            //     if (cell.isStale) return;
            //     
            //     hasDirtyRect = true;
            //
            //     if (!minCellPositionX.HasValue || cellPosition.x < minCellPositionX.Value)
            //     {
            //         minCellPositionX = cellPosition.x;
            //     }
            //         
            //     if (!minCellPositionY.HasValue || cellPosition.y < minCellPositionY.Value)
            //     {
            //         minCellPositionY = cellPosition.y;
            //     }
            //         
            //     if (!maxCellPositionX.HasValue || cellPosition.x > maxCellPositionX.Value)
            //     {
            //         maxCellPositionX = cellPosition.x;
            //     }
            //         
            //     if (!maxCellPositionY.HasValue || cellPosition.y < maxCellPositionY.Value)
            //     {
            //         maxCellPositionY = cellPosition.y;
            //     }
            // }
            //
            // for (var chunkX = -1; chunkX < 2; chunkX++)
            // {
            //     for (var chunkY = -1; chunkY < 2; chunkY++)
            //     {
            //         if (chunkX == 0 && chunkY == 0) continue;
            //         
            //         var chunkPosition = int2(chunkX, chunkY);
            //         var chunk = chunkWithNeighbors.ChunkFromPosition(chunkPosition);
            //         
            //         if (chunk.IsOutOfBounds) continue;
            //
            //         if (chunkX == 1 && chunkY == 1)
            //         {
            //             var cell = chunk.GetCell(int2(0, 0));
            //             CheckForDirtyRect(cell , int2(0, 0));
            //         }
            //         else if (chunkX == 1 && chunkY == -1)
            //         {
            //             var cell = chunk.GetCell(int2(0, ChunkSize - 1));
            //             CheckForDirtyRect(cell , int2(0, ChunkSize - 1));
            //         }
            //         else if (chunkX == -1 && chunkY == 1)
            //         {
            //             var cell = chunk.GetCell(int2(ChunkSize - 1, 0));
            //             CheckForDirtyRect(cell , int2(ChunkSize - 1, 0));
            //         }
            //         else if (chunkX == -1 && chunkY == -1)
            //         {
            //             var cell = chunk.GetCell(int2(ChunkSize - 1, ChunkSize - 1));
            //             CheckForDirtyRect(cell , int2(ChunkSize - 1, ChunkSize - 1));
            //         } 
            //         else if (chunkY == -1)
            //         {
            //             for (var x = 0; x < ChunkSize; x++)
            //             {
            //                 var cell = chunk.GetCell(int2(x, ChunkSize - 1));
            //                 CheckForDirtyRect(cell , int2(x, ChunkSize - 1));
            //             }
            //         }
            //         else if (chunkY == 1)
            //         {
            //             for (var x = 0; x < ChunkSize; x++)
            //             {
            //                 var cell = chunk.GetCell(int2(x, 0));
            //                 CheckForDirtyRect(cell , int2(x, 0));
            //             }
            //         }
            //         else if (chunkX == -1)
            //         {
            //             for (var y = 0; y < ChunkSize; y++)
            //             {
            //                 var cell = chunk.GetCell(int2(ChunkSize - 1, y));
            //                 CheckForDirtyRect(cell , int2(ChunkSize - 1, y));
            //             }
            //         }
            //         else if (chunkX == 1)
            //         {
            //             for (var y = 0; y < ChunkSize; y++)
            //             {
            //                 var cell = chunk.GetCell(int2(0, y));
            //                 CheckForDirtyRect(cell , int2(0, y));
            //             }
            //         }
            //     }
            // }

            // for (var x = 0; x < ChunkSize; x++)
            // {
            //     for (var y = 0; y < ChunkSize; y++)
            //     {
            //         var cellPosition = new int2(x, y);
            //         var cell = chunkWithNeighbors.Value.GetCell(cellPosition);
            //         
            //         if (cell.isStale) continue;
            //         hasDirtyRect = true;
            //
            //         if (!minCellPositionX.HasValue || cellPosition.x < minCellPositionX.Value)
            //         {
            //             minCellPositionX = cellPosition.x;
            //         }
            //         
            //         if (!minCellPositionY.HasValue || cellPosition.y < minCellPositionY.Value)
            //         {
            //             minCellPositionY = cellPosition.y;
            //         }
            //         
            //         if (!maxCellPositionX.HasValue || cellPosition.x > maxCellPositionX.Value)
            //         {
            //             maxCellPositionX = cellPosition.x;
            //         }
            //         
            //         if (!maxCellPositionY.HasValue || cellPosition.y > maxCellPositionY.Value)
            //         {
            //             maxCellPositionY = cellPosition.y;
            //         }
            //     }
            // }
            //
            //
            // if (!hasDirtyRect) return;

            // var minCellPosition = max(int2(minCellPositionX.Value, minCellPositionY.Value) - int2(1), int2(0));
            // var maxCellPosition = min(int2(maxCellPositionX.Value, maxCellPositionY.Value) + int2(1), int2(ChunkSize));

            // var minCellPercentage = ((float2) minCellPosition) / (float)ChunkSize;
            // var maxCellPercentage = ((float2) maxCellPosition) / (float)ChunkSize;

            
            var minCellPosition = int2(0);
            var maxCellPosition = int2(ChunkSize);
            
            
            // var a = new Rect(
            //     chunkPosition.x - (chunkScale / 2),// + (minCellPercentage.x * chunkScale), 
            //     chunkPosition.y - (chunkScale / 2) + chunkScale - (minCellPercentage.x * chunkScale),// - (maxCellPercentage.y * chunkScale), 
            //     chunkScale,// * maxCellPercentage.x,
            //     chunkScale// * maxCellPercentage.y
            // );
            //
            // drawingCommands.PushLineWidth(0.5f);
            // drawingCommands.PushDuration(simulationStep * 50);
            // drawingCommands.WireRectangle(a, Color.red);
            // drawingCommands.PopLineWidth();
            // drawingCommands.PopDuration();

            // for (var x = shouldGoReverse ? ChunkSize - 1 : 0; shouldGoReverse ? x >= 0 : x < ChunkSize; x += (shouldGoReverse ? -1 : 1))
            for (var x = shouldGoReverse ? maxCellPosition.x - 1 : minCellPosition.x; shouldGoReverse ? x >= minCellPosition.x : x < maxCellPosition.x; x += (shouldGoReverse ? -1 : 1))
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
                        // cell.isStale = false;
                        // chunkWithNeighbors.Value.SetCell(cellPosition, cell);

                        if (sandCellImplementation.Update(chunkWithNeighbors, cellPosition, random))
                        {
                        }
                        else
                        {
                            // cell.isStale = true;
                            // chunkWithNeighbors.Value.SetCell(cellPosition, cell);
                        }
                    }
                    else if (cell.type == CellType.Water)
                    {
                        // cell.isStale = false;

                        if (waterCellImplementation.Update(chunkWithNeighbors, cellPosition, random))
                        {
                        }
                        else
                        {
                            // cell.isStale = true;
                            // chunkWithNeighbors.Value.SetCell(cellPosition, cell);
                        }
                    }
                }
            }
        }
    }
    
    private readonly Chunk outOfBoundsChunk = Chunk.CreateOutOfBoundsChunk(Allocator.Persistent);
    
    private IEnumerator UpdateWorldCoroutine()
    {

        var random = new System.Random();

        while (true)
        {
            if (loadedChunks.Count == 0)
            {
                yield return new WaitForSeconds(simulationStep);
                continue;
            }
            
            using var drawingCommands = DrawingManager.GetBuilder();

            var minChunkX = loadedChunks.Min(a => a.Key.x);
            var minChunkY = loadedChunks.Min(a => a.Key.y);
            var maxChunkX = loadedChunks.Max(a => a.Key.x);
            var maxChunkY = loadedChunks.Max(a => a.Key.y);

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    JobHandle? jobHandle = null;

                    for (var chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
                    {
                        for (var chunkY = minChunkY; chunkY <= maxChunkY; chunkY++)
                        {
                            if (!((abs(chunkX % 2) == i) || (abs(chunkY % 2) == j))) continue;

                            var chunkPosition = int2(chunkX, chunkY);
                            if (!loadedChunks.TryGetValue(chunkPosition, out var chunkContainer)) continue;
                            
                            var chunkContainersWithNeighbors = new ValueWithNeighbors<ChunkContainer?>
                            {
                                Value = chunkContainer,
                                North = loadedChunks.TryGetValue(chunkPosition + int2(0, 1)),
                                NorthEast = loadedChunks.TryGetValue(chunkPosition + int2(1, 1)),
                                East = loadedChunks.TryGetValue(chunkPosition + int2(1, 0)),
                                SouthEast = loadedChunks.TryGetValue(chunkPosition + int2(1, -1)),
                                South = loadedChunks.TryGetValue(chunkPosition + int2(0, -1)),
                                SouthWest = loadedChunks.TryGetValue(chunkPosition + int2(-1, -1)),
                                West = loadedChunks.TryGetValue(chunkPosition + int2(-1, 0)),
                                NorthWest = loadedChunks.TryGetValue(chunkPosition + int2(-1, 1)),
                            };

                            var chunkWithNeighbors = new ValueWithNeighbors<Chunk>
                            {
                                Value = chunkContainer.Chunk,
                                North = chunkContainersWithNeighbors.North?.Chunk ?? outOfBoundsChunk,
                                NorthEast = chunkContainersWithNeighbors.NorthEast?.Chunk ?? outOfBoundsChunk,
                                East = chunkContainersWithNeighbors.East?.Chunk ?? outOfBoundsChunk,
                                SouthEast = chunkContainersWithNeighbors.SouthEast?.Chunk ?? outOfBoundsChunk,
                                South = chunkContainersWithNeighbors.South?.Chunk ?? outOfBoundsChunk,
                                SouthWest = chunkContainersWithNeighbors.SouthWest?.Chunk ?? outOfBoundsChunk,
                                West = chunkContainersWithNeighbors.West?.Chunk ?? outOfBoundsChunk,
                                NorthWest = chunkContainersWithNeighbors.NorthWest?.Chunk ?? outOfBoundsChunk
                            };

                            var job = new UpdateChunkJob(chunkWithNeighbors, (uint) Time.frameCount,
                                new Unity.Mathematics.Random((uint) random.Next()), drawingCommands,
                                (float2) chunkPosition * ChunkScale, ChunkScale, simulationStep);
                            var newJobHandle = job.Schedule();

                            if (jobHandle == null) jobHandle = newJobHandle;
                            else jobHandle = JobHandle.CombineDependencies(jobHandle.Value, newJobHandle);
                        }
                    }

                    if (jobHandle != null) jobHandle.Value.Complete();
                }
            }

            foreach (var chunk in loadedChunks)
            {
                bool findStartPoint(Chunk chunk, out Vector2 startPoint) {
                    for(int x= 0; x< ChunkSize; x++){
                        for(int y= 0; y< ChunkSize; y++){
                            
                            if(chunk.GetCell(int2(x, y)).type != CellType.None) {
                                startPoint = new Vector2(x,y);
                                Debug.Log("StartPoint: "+ startPoint);
                                return true;
                            }
                        }
                    }

                    startPoint = default;
                    return false; // Cannot find any start points.
                }
                
                List<Vector2> getPath2(Chunk chunk, ref List<Vector2> prevPoints, Vector2 startPoint) {

                    int[,] dirs = {{0,1},{1,0},{0,-1},{-1,0}};
                    
                    Vector2 currPoint= Vector2.zero, newPoint = Vector2.zero;
                    bool isOpen = true; // Is the path closed?

                    for(int z=0; z<dirs.GetLength(0); z++) {
                        int i = (int)startPoint.x + dirs[z,0];
                        int j = (int)startPoint.y + dirs[z,1];
                        if(i<ChunkSize && i>=0 && j<ChunkSize && j>=0) {
                            if(chunk.GetCell(int2(i, j)).type != CellType.None) {
                                currPoint = new Vector2(i,j);
                            }
                        }
                    }

                    prevPoints.Add(startPoint);

                    int count = 0;

                    while(isOpen && count<500) {
                        count++;

                        Debug.Log(currPoint);

                        prevPoints.Add(currPoint);
			
                        // Check each direction around the start point and repeat for each new point
                        for(int z=0; z<dirs.GetLength(0); z++) {
                            int i = (int)currPoint.x + dirs[z,0];
                            int j = (int)currPoint.y + dirs[z,1];
                            if(i<ChunkSize && i>=0 && j<ChunkSize && j>=0) {
                                if(chunk.GetCell(int2(i, j)).type != CellType.None) {
                                    if(!prevPoints.Contains(new Vector2(i,j))) {
                                        newPoint = new Vector2(i,j);
                                        break;
                                    } else {
                                        if(new Vector2(i,j)==startPoint) {
                                            isOpen = false;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if(!isOpen) continue;

                        // Deadend
                        if(newPoint==currPoint) {
                            for(int p=prevPoints.Count-1; p>=0; p--) {
                                for(int z=0; z<dirs.GetLength(0); z++) {
                                    int i = (int)prevPoints[p].x + dirs[z,0];
                                    int j = (int)prevPoints[p].y + dirs[z,1];
                                    if(i<ChunkSize && i>=0 && j<ChunkSize && j>=0) {
                                        if(chunk.GetCell(int2(i, j)).type != CellType.None) {
                                            if(!prevPoints.Contains(new Vector2(i,j))) {
                                                newPoint = new Vector2(i,j);
                                                break;
                                            }
                                        }
                                    }
                                }
                                if(newPoint!=currPoint) break;
                            }
                            Debug.Log("NEVER GETS PRINTED");
                        }
                        currPoint = newPoint;
                    }
                    Debug.Log("count<500?: "+count);
                    return prevPoints;
                }
                
                List<Vector2> simplifyPath(ref List<Vector2> path) {

                    List<Vector2> shortPath = new List<Vector2>();

                    Vector2 prevPoint = path[0];
                    int x=(int)path[0].x, y=(int)path[0].y;

                    shortPath.Add(prevPoint);

                    for(int i=1; i<path.Count; i++) {
                        // if x||y is the same as the previous x||y then we can skip that point
                        if(x!=(int)path[i].x && y!=(int)path[i].y)
                        {	
                            shortPath.Add(prevPoint);
                            x = (int)prevPoint.x;
                            y = (int)prevPoint.y;

                            if(shortPath.Count>3) { // if we have more than 3 points we can start checking if we can remove triangle points
                                Vector2 first = shortPath[shortPath.Count-1];
                                Vector2 last = shortPath[shortPath.Count-3];
                                if(first.x == last.x-1 && first.y == last.y-1 ||
                                   first.x == last.x+1 && first.y == last.y+1 ||
                                   first.x == last.x-1 && first.y == last.y+1 ||
                                   first.x == last.x+1 && first.y == last.y-1) {
                                    shortPath.RemoveAt(shortPath.Count-2);
                                }
                            }
                            if(shortPath.Count>3) {
                                Vector2 first = shortPath[shortPath.Count-1];
                                Vector2 middle = shortPath[shortPath.Count-2];
                                Vector2 last = shortPath[shortPath.Count-3];

                                if((first.x==middle.x+1&&middle.x+1==last.x+2 && first.y==middle.y+1&&middle.y+1==last.y+2) ||
                                   (first.x==middle.x+1&&middle.x+1==last.x+2 && first.y==middle.y-1&&middle.y-1==last.y-2) ||
                                   (first.x==middle.x-1&&middle.x-1==last.x-2 && first.y==middle.y+1&&middle.y+1==last.y+2) ||
                                   (first.x==middle.x-1&&middle.x-1==last.x-2 && first.y==middle.y-1&&middle.y-1==last.y-2)) {
                                    shortPath.RemoveAt(shortPath.Count-2);
                                }
                            }
                        }
                        prevPoint = path[i];
                    }

//		for(int i=1; i<shortPath.Count; i++) {
//			// if x||y is the same as the previous x||y then we can skip that point
//			if(x!=(int)path[i].x && y!=(int)path[i].y)
//			{	
//				shortPath.Add(prevPoint);
//				x = (int)prevPoint.x;
//				y = (int)prevPoint.y;
//			}
//			prevPoint = path[i];
//		}

                    return shortPath;
                }

                List<List<Vector2>> GetPaths(Chunk chunk) {
                    List<List<Vector2>> paths = new List<List<Vector2>>();
                    
                    // var a = chunk.Cells.copy

                    var tempChunk = chunk.Copy(Allocator.Temp);
                    
                    while(findStartPoint(tempChunk, out var startPoint)) {
                        List<Vector2> points = new List<Vector2>();

                        // Get vertices from outline
                        List<Vector2> path = getPath2(tempChunk, ref points, startPoint);

                        // remove points from temp
                        foreach(Vector2 point in path) {
                            tempChunk.SetCell(new int2((int)point.x, (int)point.y), Cell.EmptyCell);
                        }
                        paths.Add ( simplifyPath( ref path ) );
//			paths.Add (  path ); //REMOVE

                    }
                    
                    tempChunk.Dispose();

                    return paths;
                }
                
                var chunkContainer = chunk.Value;

                if (chunk.Key.Equals(int2.zero))
                {
                var paths = GetPaths(chunkContainer.Chunk);
                    
                chunkContainer.ChunkBehaviour.UpdateCollider(paths);
                }
                
                // chunkContainer.Chunk.Cells
                // chunkContainer.ChunkBehaviour.
            }

            yield return new WaitForSeconds(simulationStep);
        }
    }

    private void OnDrawGizmos()
    {
        if (loadedChunks.Count > 0)
        {
            foreach (var chunkContainer in loadedChunks)
            {
                Gizmos.DrawWireCube(chunkContainer.Value.ChunkBehaviour.transform.position, new Vector3(ChunkScale, ChunkScale));
            }
        }
    }
}