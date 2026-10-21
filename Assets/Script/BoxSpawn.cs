using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoxSpawner : MonoBehaviour
{
    [Header("Pengaturan Spawner")]
    public int maxBoxes = 4;          // Maksimal kotak yang boleh ada di conveyor
    public float spawnInterval = 2f;  // Waktu jeda antar spawn

    [Header("Jalur Conveyor")]
    [Tooltip("Objek ConveyorPath. Kotak yang muncul berjalan mengikuti waypoint-nya.")]
    public ConveyorPath path;
    [Tooltip("Kecepatan kotak di conveyor (meter/detik).")]
    public float conveyorSpeed = 3f;
    [Tooltip("Jarak minimal antar kotak saat antri di conveyor (meter). Kecilkan kalau terlalu renggang.")]
    public float boxSpacing = 0.9f;

    private readonly List<GameObject> activeBoxes = new List<GameObject>();

    void Start()
    {
        if (path == null)
            Debug.LogWarning("[BoxSpawner] Kolom Path di '" + name + "' masih kosong! Kotak tidak akan mengikuti waypoint.", this);

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(spawnInterval);

        while (true)
        {
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
                    if (newBox != null)
                    {
                        activeBoxes.Add(newBox);

                        // Suruh kotak berjalan mengikuti waypoint
                        if (path != null)
                        {
                            BoxPathFollower follower = newBox.GetComponent<BoxPathFollower>();
                            if (follower == null) follower = newBox.AddComponent<BoxPathFollower>();
                            follower.speed = conveyorSpeed;
                            follower.spacing = boxSpacing;
                            follower.Begin(path);
                        }
                    }
                }
            }

            yield return wait;
        }
    }
}