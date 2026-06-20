using UnityEngine;

public class World: MonoBehaviour
{
    [SerializeField] private Chunk chunkPrefab;

    private void Start()
    {
        Instantiate(chunkPrefab, Vector3.zero, Quaternion.identity);
    }
}