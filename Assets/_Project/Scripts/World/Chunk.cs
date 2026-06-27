using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]

public class Chunk : MonoBehaviour
{
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private byte[] voxelMap = new byte[VoxelData.ChunkVoxelCount];
    private World world;
    private Vector2Int chunkCoordinate;
    private Mesh visualMesh;
    private Mesh colliderMesh;
    private Bounds chunkMeshBounds;
    private int highestSolidY = -1;
    private int[] solidVoxelCountByY = new int[VoxelData.ChunkHeight];

    private int chunkOriginX;
    private int chunkOriginZ;

    private const int EstimatedVisibleFacesPerColumn = 6;
    private const int EstimatedVisibleFaceCount = VoxelData.ChunkWidth * VoxelData.ChunkWidth * EstimatedVisibleFacesPerColumn;

    private List<Vector3> vertices = new List<Vector3>(EstimatedVisibleFaceCount * 4);
    private List<int> triangles = new List<int>(EstimatedVisibleFaceCount * 6);
    private List<Vector2> uvs = new List<Vector2>(EstimatedVisibleFaceCount * 4);
    private List<Vector3> normals = new List<Vector3>(EstimatedVisibleFaceCount * 4);

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        visualMesh = new Mesh
        {
            name = "Chunk Visual Mesh"
        };
        visualMesh.MarkDynamic();

        colliderMesh = new Mesh
        {
            name = "Chunk Collider Mesh"
        };
        colliderMesh.MarkDynamic();

        meshFilter.sharedMesh = visualMesh;

        chunkMeshBounds = new Bounds(
            new Vector3(
                VoxelData.ChunkWidth / 2f,
                VoxelData.ChunkHeight / 2f,
                VoxelData.ChunkWidth / 2f
            ),
            new Vector3(
                VoxelData.ChunkWidth,
                VoxelData.ChunkHeight,
                VoxelData.ChunkWidth
            )
        );
    }

    public void Initialize(World world, Vector2Int chunkCoordinate)
    {
        this.world = world;
        this.chunkCoordinate = chunkCoordinate;

        chunkOriginX = chunkCoordinate.x * VoxelData.ChunkWidth;
        chunkOriginZ = chunkCoordinate.y * VoxelData.ChunkWidth;

        PopulateVoxelMap();
    }

    public void GenerateChunkMesh()
    {
        GenerateMesh();
    }

    public void RefreshChunkMesh(bool updateCollider = true)
    {
        GenerateMesh(updateCollider);
    }

    public BlockType GetVoxelFromLocalPosition(int x, int y, int z)
    {
        if (!IsVoxelInsideChunk(x, y, z))
        {
            return BlockType.Air;
        }

        return GetVoxel(x, y, z);
    }

    public void SetVoxelFromLocalPosition(int x, int y, int z, BlockType blockType)
    {
        if (!IsVoxelInsideChunk(x, y, z))
        {
            return;
        }

        SetVoxel(x, y, z, blockType);
    }

    private int GetVoxelIndex(int x, int y, int z)
    {
        return x + VoxelData.ChunkWidth * (y + VoxelData.ChunkHeight * z);
    }

    private void SetVoxel(int x, int y, int z, BlockType blockType)
    {
        int index = GetVoxelIndex(x, y, z);

        BlockType oldBlockType = (BlockType)voxelMap[index];

        bool oldBlockWasSolid = BlockDatabase.IsSolid(oldBlockType);
        bool newBlockIsSolid = BlockDatabase.IsSolid(blockType);

        voxelMap[index] = (byte)blockType;

        if (oldBlockWasSolid == newBlockIsSolid)
        {
            return;
        }

        if (newBlockIsSolid)
        {
            solidVoxelCountByY[y]++;

            if (y > highestSolidY)
            {
                highestSolidY = y;
            }
        }
        else
        {
            solidVoxelCountByY[y]--;

            if (y == highestSolidY && solidVoxelCountByY[y] == 0)
            {
                LowerHighestSolidY();
            }
        }
    }

    private BlockType GetVoxel(int x, int y, int z)
    {
        int index = GetVoxelIndex(x, y, z);
        return (BlockType)voxelMap[index];
    }

    private void PopulateVoxelMap()
    {
        highestSolidY = -1;
        System.Array.Clear(solidVoxelCountByY, 0, solidVoxelCountByY.Length);

        // Pass 1: terrain
        for (int z = 0; z < VoxelData.ChunkWidth; z++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                int globalX = chunkOriginX + x;
                int globalZ = chunkOriginZ + z;
                int terrainHeight = world.GetTerrainHeight(globalX, globalZ);

                for (int y = 0; y <= terrainHeight; y++)
                {
                    if (y == 0)
                    {
                        SetVoxel(x, y, z, BlockType.Bedrock);
                    }
                    else if (y == terrainHeight)
                    {
                        SetVoxel(x, y, z, BlockType.Grass);
                    }
                    else if (y >= terrainHeight - 3)
                    {
                        SetVoxel(x, y, z, BlockType.Dirt);
                    }
                    else
                    {
                        SetVoxel(x, y, z, BlockType.Stone);
                    }
                }
            }
        }

        // Pass 2: trees
        for (int z = 0; z < VoxelData.ChunkWidth; z++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                int globalX = chunkOriginX + x;
                int globalZ = chunkOriginZ + z;
                int terrainHeight = world.GetTerrainHeight(globalX, globalZ);

                TryGenerateTree(x, terrainHeight, z, globalX, globalZ);
            }
        }
    }

    private void GenerateMesh(bool updateCollider = true)
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        normals.Clear();

        if (highestSolidY < 0)
        {
            ApplyMesh(updateCollider);
            return;
        }

        for (int z = 0; z < VoxelData.ChunkWidth; z++)
        {
            for (int y = 0; y <= highestSolidY; y++)
            {
                int rowStartIndex = VoxelData.ChunkWidth * (y + VoxelData.ChunkHeight * z);

                for (int x = 0; x < VoxelData.ChunkWidth; x++)
                {
                    BlockType blockType = (BlockType)voxelMap[rowStartIndex + x];

                    if (blockType == BlockType.Air)
                    {
                        continue;
                    }

                    BlockData blockData = BlockDatabase.GetBlockData(blockType);

                    for (int faceIndex = 0; faceIndex < VoxelData.FaceChecks.Length; faceIndex++)
                    {
                        Vector3Int neighborOffset = VoxelData.FaceChecks[faceIndex];

                        int neighborX = x + neighborOffset.x;
                        int neighborY = y + neighborOffset.y;
                        int neighborZ = z + neighborOffset.z;

                        if (!CheckVoxel(neighborX, neighborY, neighborZ))
                        {
                            AddFace(x, y, z, faceIndex, blockData);
                        }
                    }
                }
            }
        }
        ApplyMesh(updateCollider);
    }

    private bool IsVoxelInsideChunk(int x, int y, int z)
    {
        return x >= 0 && x < VoxelData.ChunkWidth && y >= 0 && y < VoxelData.ChunkHeight && z >= 0 && z < VoxelData.ChunkWidth;
    }

    private bool CheckVoxel(int x, int y, int z)
    {
        if (y < 0 || y >= VoxelData.ChunkHeight)
        {
            return false;
        }

        if (
            x >= 0 && x < VoxelData.ChunkWidth &&
            z >= 0 && z < VoxelData.ChunkWidth
        )
        {
            int index = x + VoxelData.ChunkWidth * (y + VoxelData.ChunkHeight * z);
            BlockType blockType = (BlockType)voxelMap[index];

            return BlockDatabase.IsSolid(blockType);
        }

        Vector3Int globalPosition = new Vector3Int(
            chunkOriginX + x,
            y,
            chunkOriginZ + z
        );

        return BlockDatabase.IsSolid(world.GetBlock(globalPosition));
    }

    private void AddFace(int x, int y, int z, int faceIndex, BlockData blockData)
    {
        int vertexIndex = vertices.Count;
        Vector3 blockPosition = new Vector3(x, y, z);
        Vector3 faceNormal = VoxelData.FaceChecks[faceIndex];
        for (int i = 0; i < 4; i++)
        {
            int cubeVertexIndex = VoxelData.VoxelTriangles[faceIndex, i];
            Vector3 cubeVertexPosition = VoxelData.VoxelVertices[cubeVertexIndex];
            Vector3 finalVertexPosition = blockPosition + cubeVertexPosition;
            vertices.Add(finalVertexPosition);
            normals.Add(faceNormal);
        }
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);

        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 3);

        Vector2Int textureTile = blockData.GetTextureForFace(faceIndex);

        float textureSize = VoxelData.NormalizedBlockTextureSize;
        float padding = VoxelData.TexturePadding;

        float uMin = textureTile.x * textureSize + padding;
        float vMin = textureTile.y * textureSize + padding;
        float uMax = (textureTile.x + 1) * textureSize - padding;
        float vMax = (textureTile.y + 1) * textureSize - padding;
        uvs.Add(new Vector2(uMin, vMin));
        uvs.Add(new Vector2(uMin, vMax));
        uvs.Add(new Vector2(uMax, vMin));
        uvs.Add(new Vector2(uMax, vMax));
    }
    
    private void ApplyMesh(bool updateCollider)
    {
        visualMesh.Clear();

        visualMesh.SetVertices(vertices);
        visualMesh.SetTriangles(triangles, 0, false);
        visualMesh.bounds = chunkMeshBounds;
        visualMesh.SetUVs(0, uvs);
        visualMesh.SetNormals(normals);

        if (updateCollider)
        {
            UpdateColliderMesh();
        }
    }

    private void UpdateColliderMesh()
    {
        colliderMesh.Clear();

        colliderMesh.SetVertices(vertices);
        colliderMesh.SetTriangles(triangles, 0, false);
        colliderMesh.bounds = chunkMeshBounds;

        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = colliderMesh;
    }

    public void RefreshColliderOnly()
    {
        UpdateColliderMesh();
    }

    private void LowerHighestSolidY()
    {
        while (highestSolidY >= 0 && solidVoxelCountByY[highestSolidY] == 0)
        {
            highestSolidY--;
        }
    }

    private void TryGenerateTree(int x, int terrainHeight, int z, int globalX, int globalZ)
    {
        if (!world.ShouldPlaceTree(globalX, globalZ))
        {
            return;
        }

        // Keep trees away from chunk borders for now.
        // This prevents leaves crossing into neighboring chunks.
        if (x < 2 || x > VoxelData.ChunkWidth - 3 || z < 2 || z > VoxelData.ChunkWidth - 3)
        {
            return;
        }

        int trunkHeight = 4;
        int treeTopY = terrainHeight + trunkHeight + 1;

        if (treeTopY >= VoxelData.ChunkHeight)
        {
            return;
        }

        for (int y = terrainHeight + 1; y <= terrainHeight + trunkHeight; y++)
        {
            SetVoxel(x, y, z, BlockType.Wood);
        }

        int leafStartY = terrainHeight + trunkHeight - 1;
        int leafEndY = terrainHeight + trunkHeight + 1;

        for (int leafY = leafStartY; leafY <= leafEndY; leafY++)
        {
            for (int offsetZ = -2; offsetZ <= 2; offsetZ++)
            {
                for (int offsetX = -2; offsetX <= 2; offsetX++)
                {
                    int distance = Mathf.Abs(offsetX) + Mathf.Abs(offsetZ);

                    if (distance > 3)
                    {
                        continue;
                    }

                    if (leafY == leafEndY && distance > 1)
                    {
                        continue;
                    }

                    int leafX = x + offsetX;
                    int leafZ = z + offsetZ;

                    bool isTrunkPosition = leafX == x && leafZ == z && leafY <= terrainHeight + trunkHeight;

                    if (isTrunkPosition)
                    {
                        continue;
                    }

                    SetVoxel(leafX, leafY, leafZ, BlockType.Leaves);
                }
            }
        }
    }
}
