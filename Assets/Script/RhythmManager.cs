using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RhythmManager : MonoBehaviour
{
    [Header("Masukkan 4 Prefab Nada (A, X, B, Y)")]
    public GameObject[] notePrefabs;

    [Header("Masukkan 4 Titik Spawner")]
    public Transform[] spawnPoints;

    [Header("Pengaturan Spawn")]
    public float spawnInterval = 1f; // Waktu normal antar nada muncul

    [Range(0f, 1f)]
    public float doubleNoteChance = 0.3f; // Peluang 30% muncul double-tap
    public float doubleTapDelay = 0.25f; // Jarak waktu antara nada 1 dan nada 2 saat double-tap

    void Start()
    {
        StartCoroutine(SpawnRandomNotes());
    }

    IEnumerator SpawnRandomNotes()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (spawnPoints.Length > 0 && notePrefabs.Length == spawnPoints.Length)
            {
                // Pilih satu jalur secara acak (0 sampai 3)
                int randomIndex = Random.Range(0, spawnPoints.Length);

                // Undi apakah akan memunculkan Double-Tap atau Single Note
                bool isDoubleNote = Random.value <= doubleNoteChance;

                if (isDoubleNote)
                {
                    // Jalankan fungsi khusus untuk memunculkan 2 nada berurutan
                    StartCoroutine(SpawnRapidNote(randomIndex));
                }
                else
                {
                    InstantiateNote(randomIndex);
                }
            }
        }
    }

    // Fungsi khusus (Coroutine) untuk memunculkan nada secara beruntun di jalur yang sama
    IEnumerator SpawnRapidNote(int index)
    {
        // Munculkan nada pertama
        InstantiateNote(index);

        // Tunggu sepersekian detik (sesuai nilai doubleTapDelay)
        yield return new WaitForSeconds(doubleTapDelay);

        // Munculkan nada kedua di jalur yang persis sama
        InstantiateNote(index);
    }

    void InstantiateNote(int index)
    {
        if (notePrefabs[index] != null && spawnPoints[index] != null)
        {
            Instantiate(notePrefabs[index], spawnPoints[index].position, spawnPoints[index].rotation);
        }
    }
}