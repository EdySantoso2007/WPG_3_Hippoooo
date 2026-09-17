using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoxSpawner : MonoBehaviour
{
    [Header("Pengaturan Spawner")]
    public GameObject boxPrefab; // Masukkan Prefab Kotak ke sini
    public int maxBoxes = 4;     // Maksimal kotak yang boleh ada di conveyor
    public float spawnInterval = 2f; // Waktu jeda antar spawn

    private List<GameObject> activeBoxes = new List<GameObject>();

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            // BERSIH-BERSIH LIST:
            // Hapus kotak dari daftar jika kotak sudah hancur (null)
            // ATAU kotak sudah diambil pemain (parent tidak null)
            activeBoxes.RemoveAll(box => box == null || box.transform.parent != null);

            // Jika jumlah kotak yang jalan di conveyor masih kurang dari batas
            if (activeBoxes.Count < maxBoxes)
            {
                // Clone kotak baru
                GameObject newBox = Instantiate(boxPrefab, transform.position, transform.rotation);
                activeBoxes.Add(newBox);
            }

            // Tunggu beberapa detik sebelum cek lagi
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}