using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using static Unity.Mathematics.math;

[RequireComponent(typeof(WorldGrid))]
public class PlayerChunkLoader : MonoBehaviour
{
    [Required]
    [SerializeField] private Transform player = null!;
    [Min(1)]
    [SerializeField] private int chunkLoadRadius = 3;
    
    
    private WorldGrid worldGrid;

    private void Awake()
    {
        worldGrid = GetComponent<WorldGrid>();
    }

    private void Start()
    {
        StartCoroutine(CheckForChunkLoadCoroutine());
    }

    private IEnumerator CheckForChunkLoadCoroutine()
    {
        while (true)
        {
            var worldPosition = player.position;
            var chunkPosition = worldGrid.ChunkPosition(worldPosition);

            for (var x = -chunkLoadRadius; x <= chunkLoadRadius; x++)
            {
                for (var y = -chunkLoadRadius; y <= chunkLoadRadius; y++)
                {
                    var offsetedChunkPosition = chunkPosition + int2(x, y);

                    if (!worldGrid.ChunkIsLoaded(offsetedChunkPosition))
                    {
                        worldGrid.LoadChunk(offsetedChunkPosition);
                    }
                }
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }
}
