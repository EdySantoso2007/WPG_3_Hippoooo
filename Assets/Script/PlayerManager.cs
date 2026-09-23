using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    [Header("Prefab Player")]
    [Tooltip("Prefab player ASLI (dengan visual, Renderer, RhythmController, dll). " +
             "Ini prefab yang DULU dipasang di PlayerInputManager - sekarang pindah ke sini.")]
    public GameObject playerPrefab;

    [Header("Pengaturan Spawn")]
    public List<Transform> spawnPoints;

    [Header("Urutan Warna Player")]
    // Merah, Biru, Hijau, Kuning
    private Color[] playerColors = { Color.red, Color.blue, Color.green, Color.yellow };

    private int playersJoined = 0;

    private void Start()
    {
        // Spawn player asli sesuai data device yang sudah dicatat waktu join
        // di scene Room/Lobby (lewat RoomManager), urut sesuai urutan join.
        foreach (JoinedPlayerData data in RoomBridge.JoinedPlayers)
        {
            SpawnPlayer(data);
        }
    }

    // Tetap dipertahankan untuk kasus ada PlayerInputManager langsung di scene
    // ini juga (mis. testing tanpa lewat Room/Lobby)
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        SetupPlayer(playerInput);
    }

    private void SpawnPlayer(JoinedPlayerData data)
    {
        // Instantiate prefab asli, dipasangkan ke device fisik yang SAMA
        // dengan waktu player itu join di room (biar controllernya nyambung
        // ke player yang benar, bukan device lain)
        PlayerInput playerInput = PlayerInput.Instantiate(
            playerPrefab,
            controlScheme: data.controlScheme,
            pairWithDevices: data.devices
        );

        SetupPlayer(playerInput);
    }

    private void SetupPlayer(PlayerInput playerInput)
    {
        if (playersJoined >= spawnPoints.Count || playersJoined >= playerColors.Length)
        {
            Debug.Log("Maksimal player sudah tercapai, atau spawnPoints kurang!");
            Destroy(playerInput.gameObject);
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