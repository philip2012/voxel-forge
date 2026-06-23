using UnityEngine;
using System.Collections.Generic;

public class World : MonoBehaviour
{
    [SerializeField] private Chunk chunkPrefab;
    [SerializeField, Min(0)] private int viewDistanceInChunks = 1;    
    [SerializeField] private int baseTerrainHeight = 4;
    [SerializeField] private int terrainHeightVariation = 4;
    [SerializeField] private float terrainScale = 0.08f;
    [SerializeField] private int seed = 12345;
    private Dictionary<Vector2Int, Chunk> activeChunks = new Dictionary<Vector2Int, Chunk>();

    private void Start()
    {
        for (int x = -viewDistanceInChunks; x <= viewDistanceInChunks; x++)
        {
            for (int z = -viewDistanceInChunks; z <= viewDistanceInChunks; z++)
            {
                SpawnChunk(new Vector2Int(x, z));
            }
        }

        foreach (KeyValuePair<Vector2Int, Chunk> activeChunk in activeChunks)
        {
            activeChunk.Value.Initialize(this, activeChunk.Key);
        }

        foreach (KeyValuePair<Vector2Int, Chunk> activeChunk in activeChunks)
        {
            activeChunk.Value.GenerateChunkMesh();
        }
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

        return chunk;
    }

    public int GetTerrainHeight(int globalX, int globalZ)
    {
        float noise = Mathf.PerlinNoise(
            (globalX + seed) * terrainScale,
            (globalZ + seed) * terrainScale
        );

        int height = baseTerrainHeight + Mathf.FloorToInt(noise * terrainHeightVariation);

        return Mathf.Clamp(height, 1, VoxelData.ChunkHeight - 1);
    }

    public void SetBlock(Vector3Int globalPosition, BlockType blockType)
    {
        if (globalPosition.y < 0 || globalPosition.y >= VoxelData.ChunkHeight)
        {
            return;
        }

        int chunkX = Mathf.FloorToInt((float)globalPosition.x / VoxelData.ChunkWidth);
        int chunkZ = Mathf.FloorToInt((float)globalPosition.z / VoxelData.ChunkWidth);

        int localX = globalPosition.x - chunkX * VoxelData.ChunkWidth;
        int localZ = globalPosition.z - chunkZ * VoxelData.ChunkWidth;

        Vector2Int chunkCoordinate = new Vector2Int(chunkX, chunkZ);

        if (!activeChunks.TryGetValue(chunkCoordinate, out Chunk chunk))
        {
            return;
        }

        if (chunk.GetVoxelFromLocalPosition(localX, globalPosition.y, localZ) == blockType)
        {
            return;
        }

        chunk.SetVoxelFromLocalPosition(localX, globalPosition.y, localZ, blockType);
        RefreshChunkAtCoordinate(chunkCoordinate);

        if (localX == 0)
        {
            RefreshChunkAtCoordinate(chunkCoordinate + new Vector2Int(-1, 0));
        }
        else if (localX == VoxelData.ChunkWidth - 1)
        {
            RefreshChunkAtCoordinate(chunkCoordinate + new Vector2Int(1, 0));
        }

        if (localZ == 0)
        {
            RefreshChunkAtCoordinate(chunkCoordinate + new Vector2Int(0, -1));
        }
        else if (localZ == VoxelData.ChunkWidth - 1)
        {
            RefreshChunkAtCoordinate(chunkCoordinate + new Vector2Int(0, 1));
        }
    }

    private void RefreshChunkAtCoordinate(Vector2Int chunkCoordinate)
    {
        if (activeChunks.TryGetValue(chunkCoordinate, out Chunk chunk))
        {
            chunk.RefreshChunkMesh();
        }
    }
}
