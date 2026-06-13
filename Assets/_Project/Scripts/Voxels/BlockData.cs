// file: Assets/_Project/Scripts/Voxels/BlockData.cs

using UnityEngine;

public struct BlockData
{
    public string name;
    public bool isSolid;
    public Vector2Int topTexture;
    public Vector2Int bottomTexture;
    public Vector2Int sideTexture;

    public Vector2Int GetTextureForFace(int faceIndex)
    {
        if (faceIndex == 2)
        {
            return topTexture;
        }

        if (faceIndex == 3)
        {
            return bottomTexture;
        }

        return sideTexture;
    }
}