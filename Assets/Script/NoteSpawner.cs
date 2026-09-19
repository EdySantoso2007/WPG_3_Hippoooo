using UnityEngine;
using System.Collections;

public class NoteSpawner : MonoBehaviour
{
    public GameObject notePrefab;
    public float spawnInterval = 2f; // Jarak waktu antar nada muncul

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            // Menunggu selama spawnInterval
            yield return new WaitForSeconds(spawnInterval);

            // Mencetak nada baru
            if (notePrefab != null)
            {
                Instantiate(notePrefab, transform.position, transform.rotation);
            }
        }
    }
}