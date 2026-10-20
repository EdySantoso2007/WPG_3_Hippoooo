using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoxSpawner : MonoBehaviour
{
    [Header("Pengaturan Spawner")]
    public int maxBoxes = 4;          // Maksimal kotak yang boleh ada di conveyor
    public float spawnInterval = 2f;  // Waktu jeda antar spawn

    private readonly List<GameObject> activeBoxes = new List<GameObject>();

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(spawnInterval);

        while (true)
        {
            // BERSIH-BERSIH LIST:
            // Hapus kotak dari daftar jika sudah hancur / nonaktif (kembali ke pool)
            // ATAU sudah diambil pemain (parent tidak null)
            activeBoxes.RemoveAll(box => box == null || !box.activeInHierarchy || box.transform.parent != null);

            if (activeBoxes.Count < maxBoxes)
            {
                if (BoxPool.Instance == null)
                {
                    Debug.LogError("[BoxSpawner] Tidak ada BoxPool di scene!");
                }
                else
                {
                    // Ambil kotak dari pool (bukan Instantiate)
                    GameObject newBox = BoxPool.Instance.Get(transform.position, transform.rotation);
                    if (newBox != null) activeBoxes.Add(newBox);
                }
            }

            yield return wait;
        }
    }
}