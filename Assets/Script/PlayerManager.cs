using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    [Header("Pengaturan Spawn")]
    public List<Transform> spawnPoints;

    [Header("Urutan Warna Player")]
    // Merah, Biru, Hijau, Kuning
    private Color[] playerColors = { Color.red, Color.blue, Color.green, Color.yellow };

    private int playersJoined = 0;

    // Fungsi ini dipanggil otomatis oleh PlayerInputManager saat player baru masuk
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        if (playersJoined >= 4)
        {
            Debug.Log("Maksimal 4 Player!");
            return;
        }

        // 1. Pindahkan player ke titik spawn sesuai urutan
        playerInput.transform.position = spawnPoints[playersJoined].position;

        // 2. Ubah warna player (versi 3D: cari SEMUA Renderer di karakter,
        // termasuk yang ada di child object seperti body/mesh visual)
        Renderer[] renderers = playerInput.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            foreach (Renderer r in renderers)
            {
                // Instance baru material supaya tidak ikut mewarnai
                // player lain yang share material yang sama
                r.material.color = playerColors[playersJoined];
            }
        }
        else
        {
            Debug.LogWarning("Tidak ada Renderer ditemukan di Player untuk diwarnai.");
        }

        // Tambah jumlah player yang sudah masuk
        playersJoined++;
    }
}