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
    private Mesh mesh;

    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();

    private List<Vector2> uvs = new List<Vector2>();

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        mesh = new Mesh
        {
            name = "Chunk Mesh"
        };
        mesh.MarkDynamic();

        meshFilter.sharedMesh = mesh;
    }

    public void Initialize(World world, Vector2Int chunkCoordinate)
    {
        this.world = world;
        this.chunkCoordinate = chunkCoordinate;

        PopulateVoxelMap();
    }

    public void GenerateChunkMesh()
    {
        GenerateMesh();
    }

    public void RefreshChunkMesh()
    {
        GenerateMesh();
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
        voxelMap[index] = (byte)blockType;
    }

    private BlockType GetVoxel(int x, int y, int z)
    {
        int index = GetVoxelIndex(x, y, z);
        return (BlockType)voxelMap[index];
    }

    private void PopulateVoxelMap()
    {
        for (int x = 0; x < VoxelData.ChunkWidth; x++)
        {
            for (int z = 0; z < VoxelData.ChunkWidth; z++)
            {
                int globalX = chunkCoordinate.x * VoxelData.ChunkWidth + x;
                int globalZ = chunkCoordinate.y * VoxelData.ChunkWidth + z;
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
    }

    private void GenerateMesh()
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();

        for (int x = 0; x < VoxelData.ChunkWidth; x++)
        {
            for (int y = 0; y < VoxelData.ChunkHeight; y++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    if (GetVoxel(x, y, z) != BlockType.Air)
                    {
                        for (int faceIndex = 0; faceIndex < VoxelData.FaceChecks.Length; faceIndex++)
                        {
                            Vector3Int neighborOffset = VoxelData.FaceChecks[faceIndex];

                            int neighborX = x + neighborOffset.x;
                            int neighborY = y + neighborOffset.y;
                            int neighborZ = z + neighborOffset.z;

                            if (!CheckVoxel(neighborX, neighborY, neighborZ))
                            {
                                AddFace(x, y, z, faceIndex);
                            }
                        }

                    }
                }
            }
        }

        ApplyMesh();
    }

    private bool IsVoxelInsideChunk(int x, int y, int z)
    {
        return x >= 0 && x < VoxelData.ChunkWidth && y >= 0 && y < VoxelData.ChunkHeight && z >= 0 && z < VoxelData.ChunkWidth;
    }

    private bool CheckVoxel(int x, int y, int z)
    {
        BlockType blockType;

        if (IsVoxelInsideChunk(x, y, z))
        {
            blockType = GetVoxel(x, y, z);
        }
        else
        {
            Vector3Int globalPosition = new Vector3Int(
                chunkCoordinate.x * VoxelData.ChunkWidth + x,
                y,
                chunkCoordinate.y * VoxelData.ChunkWidth + z
            );

            blockType = world.GetBlock(globalPosition);
        }

        BlockData blockData = BlockDatabase.GetBlockData(blockType);
        return blockData.isSolid;
    }

    private void AddFace(int x, int y, int z, int faceIndex)
    {
        int vertexIndex = vertices.Count;
        Vector3 blockPosition = new Vector3(x, y, z);
        for (int i = 0; i < 4; i++)
        {
            int cubeVertexIndex = VoxelData.VoxelTriangles[faceIndex, i];
            Vector3 cubeVertexPosition = VoxelData.VoxelVertices[cubeVertexIndex];
            Vector3 finalVertexPosition = blockPosition + cubeVertexPosition;
            vertices.Add(finalVertexPosition);
        }
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);

        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 3);

        BlockType blockType = GetVoxel(x, y, z);
        BlockData blockData = BlockDatabase.GetBlockData(blockType);
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
    
    private void ApplyMesh()
    {
        mesh.Clear();

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();

        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }
}
