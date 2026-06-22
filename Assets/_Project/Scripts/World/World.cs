using UnityEngine;
using System.Collections.Generic;

public class World : MonoBehaviour
{
    [SerializeField] private Chunk chunkPrefab;
    private Dictionary<Vector2Int, Chunk> activeChunks = new Dictionary<Vector2Int, Chunk>();

    private void Start()
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                SpawnChunk(new Vector2Int(x, z));
            }
        }

        Debug.Log(GetBlock(new Vector3Int(0, 4, 0)));
        Debug.Log(GetBlock(new Vector3Int(16, 4, 0)));
        Debug.Log(GetBlock(new Vector3Int(-1, 4, 0)));
        Debug.Log(GetBlock(new Vector3Int(0, 10, 0)));
    }

    public BlockType GetBlock(Vector3Int globalPosition)
    {
        if (globalPosition.y < 0 || globalPosition.y >= VoxelData.ChunkHeight)
        {
            return BlockType.Air;
        }

        int chunkX = Mathf.FloorToInt((float)globalPosition.x / VoxelData.ChunkWidth);
        int chunkZ = Mathf.FloorToInt((float)globalPosition.z / VoxelData.ChunkWidth);

        int localX = globalPosition.x - chunkX * VoxelData.ChunkWidth;
        int localZ = globalPosition.z - chunkZ * VoxelData.ChunkWidth;

        Vector2Int chunkCoordinate = new Vector2Int(chunkX, chunkZ);

        if (!activeChunks.TryGetValue(chunkCoordinate, out Chunk chunk))
        {
            return BlockType.Air;
        }

        return chunk.GetVoxelFromLocalPosition(localX, globalPosition.y, localZ);
    }

    private Chunk SpawnChunk(Vector2Int chunkCoordinate)
    {
        if (activeChunks.ContainsKey(chunkCoordinate))
        {
            return activeChunks[chunkCoordinate];
        }
        Vector3 worldPosition = new Vector3(
            chunkCoordinate.x * VoxelData.ChunkWidth,
            0,
            chunkCoordinate.y * VoxelData.ChunkWidth
        );

        Chunk chunk = Instantiate(chunkPrefab, worldPosition, Quaternion.identity, transform);
        chunk.name = $"Chunk ({chunkCoordinate.x}, {chunkCoordinate.y})";
        activeChunks.Add(chunkCoordinate, chunk);
        chunk.Initialize(this, chunkCoordinate);

        return chunk;
    }
}