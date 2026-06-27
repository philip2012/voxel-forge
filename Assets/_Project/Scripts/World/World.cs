using UnityEngine;
using System.Collections.Generic;

public class World : MonoBehaviour
{
    [SerializeField] private Chunk chunkPrefab;
    [SerializeField, Min(0)] private int viewDistanceInChunks = 1;
    [SerializeField, Min(1)] private int maxVisualChunkRefreshesPerFrame = 2;
    [SerializeField, Min(1)] private int maxColliderChunkRefreshesPerFrame = 1;
    [SerializeField] private int baseTerrainHeight = 4;
    [SerializeField] private int terrainHeightVariation = 4;
    [SerializeField] private float terrainScale = 0.08f;
    [SerializeField] private int seed = 12345;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Vector2Int playerSpawnXZ = Vector2Int.zero;
    [SerializeField, Range(0, 20)] private int treeChancePercent = 1;
    private Vector2Int currentPlayerChunkCoordinate = new Vector2Int(int.MinValue, int.MinValue);
    private Dictionary<Vector3Int, BlockType> modifiedBlocks = new Dictionary<Vector3Int, BlockType>();
    private Dictionary<Vector2Int, Chunk> activeChunks = new Dictionary<Vector2Int, Chunk>();
    private HashSet<Vector2Int> dirtyVisualChunks = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> dirtyColliderChunks = new HashSet<Vector2Int>();
    private List<Vector2Int> processedChunksThisFrame = new List<Vector2Int>();
    private List<Vector2Int> chunksToUnload = new List<Vector2Int>();

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

        SpawnPlayerOnTerrain();

        if (playerTransform != null)
        {
            currentPlayerChunkCoordinate = GetChunkCoordinateFromWorldPosition(playerTransform.position);
            LoadChunksAroundPlayer();
            UnloadDistantChunks();
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            return;
        }

        Vector2Int playerChunkCoordinate = GetChunkCoordinateFromWorldPosition(playerTransform.position);

        if (playerChunkCoordinate == currentPlayerChunkCoordinate)
        {
            return;
        }

        currentPlayerChunkCoordinate = playerChunkCoordinate;
        LoadChunksAroundPlayer();
        UnloadDistantChunks();
    }

    private void LateUpdate()
    {
        ProcessVisualRefreshQueue();
        ProcessColliderRefreshQueue();
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

        BlockType oldBlockType = chunk.GetVoxelFromLocalPosition(localX, globalPosition.y, localZ);

        if (oldBlockType == blockType)
        {
            return;
        }

        bool oldBlockIsSolid = BlockDatabase.IsSolid(oldBlockType);
        bool newBlockIsSolid = BlockDatabase.IsSolid(blockType);

        bool colliderChanged = oldBlockIsSolid != newBlockIsSolid;

        chunk.SetVoxelFromLocalPosition(localX, globalPosition.y, localZ, blockType);
        modifiedBlocks[globalPosition] = blockType;
        MarkChunkDirty(chunkCoordinate, colliderChanged);

        if (!colliderChanged)
        {
            return;
        }

        if (localX == 0)
        {
            MarkNeighborChunkDirtyIfSolid(
                chunkCoordinate + new Vector2Int(-1, 0),
                globalPosition + new Vector3Int(-1, 0, 0),
                colliderChanged
            );
        }
        else if (localX == VoxelData.ChunkWidth - 1)
        {
            MarkNeighborChunkDirtyIfSolid(
                chunkCoordinate + new Vector2Int(1, 0),
                globalPosition + new Vector3Int(1, 0, 0),
                colliderChanged
            );
        }

        if (localZ == 0)
        {
            MarkNeighborChunkDirtyIfSolid(
                chunkCoordinate + new Vector2Int(0, -1),
                globalPosition + new Vector3Int(0, 0, -1),
                colliderChanged
            );
        }
        else if (localZ == VoxelData.ChunkWidth - 1)
        {
            MarkNeighborChunkDirtyIfSolid(
                chunkCoordinate + new Vector2Int(0, 1),
                globalPosition + new Vector3Int(0, 0, 1),
                colliderChanged
            );
        }
    }

    private void MarkChunkDirty(Vector2Int chunkCoordinate, bool updateCollider)
    {
        if (!activeChunks.ContainsKey(chunkCoordinate))
        {
            return;
        }

        dirtyVisualChunks.Add(chunkCoordinate);

        if (updateCollider)
        {
            dirtyColliderChunks.Add(chunkCoordinate);
        }
    }

    private void MarkNeighborChunkDirtyIfSolid(
        Vector2Int neighborChunkCoordinate,
        Vector3Int neighborGlobalPosition,
        bool updateCollider)
    {
        if (!activeChunks.ContainsKey(neighborChunkCoordinate))
        {
            return;
        }

        BlockType neighborBlockType = GetBlock(neighborGlobalPosition);

        if (BlockDatabase.IsSolid(neighborBlockType))
        {
            MarkChunkDirty(neighborChunkCoordinate, updateCollider);
        }
    }

    private void ProcessVisualRefreshQueue()
    {
        if (dirtyVisualChunks.Count == 0)
        {
            return;
        }

        processedChunksThisFrame.Clear();

        foreach (Vector2Int chunkCoordinate in dirtyVisualChunks)
        {
            if (activeChunks.TryGetValue(chunkCoordinate, out Chunk chunk))
            {
                chunk.RefreshChunkMesh(false);
            }

            processedChunksThisFrame.Add(chunkCoordinate);

            if (processedChunksThisFrame.Count >= maxVisualChunkRefreshesPerFrame)
            {
                break;
            }
        }

        foreach (Vector2Int chunkCoordinate in processedChunksThisFrame)
        {
            dirtyVisualChunks.Remove(chunkCoordinate);
        }
    }

    private void ProcessColliderRefreshQueue()
    {
        if (dirtyColliderChunks.Count == 0)
        {
            return;
        }

        processedChunksThisFrame.Clear();

        foreach (Vector2Int chunkCoordinate in dirtyColliderChunks)
        {
            if (dirtyVisualChunks.Contains(chunkCoordinate))
            {
                continue;
            }

            if (activeChunks.TryGetValue(chunkCoordinate, out Chunk chunk))
            {
                chunk.RefreshColliderOnly();
            }

            processedChunksThisFrame.Add(chunkCoordinate);

            if (processedChunksThisFrame.Count >= maxColliderChunkRefreshesPerFrame)
            {
                break;
            }
        }

        foreach (Vector2Int chunkCoordinate in processedChunksThisFrame)
        {
            dirtyColliderChunks.Remove(chunkCoordinate);
        }
    }

    private void SpawnPlayerOnTerrain()
    {
        if (playerTransform == null)
        {
            return;
        }

        int terrainHeight = GetTerrainHeight(playerSpawnXZ.x, playerSpawnXZ.y);

        playerTransform.position = new Vector3(
            playerSpawnXZ.x + 0.5f,
            terrainHeight + 1f,
            playerSpawnXZ.y + 0.5f
        );
    }
    
    public bool ShouldPlaceTree(int globalX, int globalZ)
    {
        int hash = GetDeterministicHash(globalX, globalZ);
        return hash % 100 < treeChancePercent;
    }

    private int GetDeterministicHash(int x, int z)
    {
        unchecked
        {
            int hash = x;
            hash = hash * 397 ^ z;
            hash = hash * 397 ^ seed;
            hash ^= hash >> 16;
            return hash & 0x7fffffff;
        }
    }

    private Vector2Int GetChunkCoordinateFromWorldPosition(Vector3 worldPosition)
    {
        int chunkX = Mathf.FloorToInt(worldPosition.x / VoxelData.ChunkWidth);
        int chunkZ = Mathf.FloorToInt(worldPosition.z / VoxelData.ChunkWidth);

        return new Vector2Int(chunkX, chunkZ);
    }

    private void LoadChunksAroundPlayer()
    {
        for (int x = -viewDistanceInChunks; x <= viewDistanceInChunks; x++)
        {
            for (int z = -viewDistanceInChunks; z <= viewDistanceInChunks; z++)
            {
                Vector2Int chunkCoordinate = currentPlayerChunkCoordinate + new Vector2Int(x, z);

                if (activeChunks.ContainsKey(chunkCoordinate))
                {
                    continue;
                }

                Chunk chunk = SpawnChunk(chunkCoordinate);
                chunk.Initialize(this, chunkCoordinate);
                chunk.GenerateChunkMesh();

                MarkExistingNeighborChunksDirty(chunkCoordinate);
            }
        }
    }

    private void MarkExistingNeighborChunksDirty(Vector2Int chunkCoordinate)
    {
        MarkChunkDirty(chunkCoordinate + new Vector2Int(-1, 0), true);
        MarkChunkDirty(chunkCoordinate + new Vector2Int(1, 0), true);
        MarkChunkDirty(chunkCoordinate + new Vector2Int(0, -1), true);
        MarkChunkDirty(chunkCoordinate + new Vector2Int(0, 1), true);
    }

    private void UnloadDistantChunks()
    {
        chunksToUnload.Clear();

        foreach (Vector2Int chunkCoordinate in activeChunks.Keys)
        {
            if (!IsChunkWithinViewDistance(chunkCoordinate))
            {
                chunksToUnload.Add(chunkCoordinate);
            }
        }

        foreach (Vector2Int chunkCoordinate in chunksToUnload)
        {
            if (!activeChunks.TryGetValue(chunkCoordinate, out Chunk chunk))
            {
                continue;
            }

            dirtyVisualChunks.Remove(chunkCoordinate);
            dirtyColliderChunks.Remove(chunkCoordinate);

            activeChunks.Remove(chunkCoordinate);

            Destroy(chunk.gameObject);

            MarkExistingNeighborChunksDirty(chunkCoordinate);
        }
    }

    private bool IsChunkWithinViewDistance(Vector2Int chunkCoordinate)
    {
        int distanceX = Mathf.Abs(chunkCoordinate.x - currentPlayerChunkCoordinate.x);
        int distanceZ = Mathf.Abs(chunkCoordinate.y - currentPlayerChunkCoordinate.y);

        return distanceX <= viewDistanceInChunks && distanceZ <= viewDistanceInChunks;
    }

    public bool TryGetModifiedBlock(Vector3Int globalPosition, out BlockType blockType)
    {
        return modifiedBlocks.TryGetValue(globalPosition, out blockType);
    }
}
