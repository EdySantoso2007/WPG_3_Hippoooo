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

    [Header("Texture Player (UV)")]
    [Tooltip("Urutan harus: [0] Merah, [1] Biru, [2] Hijau, [3] Kuning")]
    public Texture2D[] playerTextures;

    // ID properti material (URP Lit memakai _BaseMap/_BaseColor, Built-in memakai _MainTex/_Color)
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

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
        if (playersJoined >= spawnPoints.Count || playersJoined >= playerTextures.Length)
        {
            Debug.Log("Maksimal player sudah tercapai, spawnPoints kurang, atau Player Textures kurang!");
            Destroy(playerInput.gameObject);
            return;
        }

        // 1. Pindahkan player ke titik spawn sesuai urutan
        playerInput.transform.position = spawnPoints[playersJoined].position;

        // 2. Ganti texture (UV) sesuai urutan player
        ApplyTexture(playerInput.gameObject, playerTextures[playersJoined]);

        // Tambah jumlah player yang sudah masuk
        playersJoined++;
    }

    private void ApplyTexture(GameObject player, Texture2D texture)
    {
        if (texture == null)
        {
            Debug.LogWarning("Texture untuk player ini belum diisi di PlayerManager (Player Textures).");
            return;
        }

        // Hanya ubah Renderer milik model hippo (anak dari Animator),
        // supaya Renderer lain (mis. kapsul lama yang dimatikan) tidak ikut.
        // Kalau Animator tidak ketemu, pakai semua Renderer di player.
        Animator animator = player.GetComponentInChildren<Animator>();
        Transform modelRoot = animator != null ? animator.transform : player.transform;

        Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("Tidak ada Renderer ditemukan di Player untuk diberi texture.");
            return;
        }

        foreach (Renderer r in renderers)
        {
            // r.materials membuat salinan material khusus renderer ini,
            // jadi texture satu player tidak ikut mengubah player lain.
            foreach (Material m in r.materials)
            {
                if (m.HasProperty(BaseMapId))
                {
                    m.SetTexture(BaseMapId, texture);
                    if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, Color.white);
                }
                else if (m.HasProperty(MainTexId))
                {
                    m.SetTexture(MainTexId, texture);
                    if (m.HasProperty(ColorId)) m.SetColor(ColorId, Color.white);
                }
            }
        }
    }
}