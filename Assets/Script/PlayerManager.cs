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

    // Hanya dipakai untuk jalur testing (PlayerInputManager langsung di scene ini)
    private int playersJoined = 0;

    private void Start()
    {
        // Spawn player asli sesuai data device yang sudah dicatat waktu join
        // di scene Room/Lobby (lewat RoomManager). Slot = urutan join = warna.
        // Pemain yang sudah DIPECAT dilewati, tapi slot (spawn point & warna) pemain lain tidak bergeser.
        for (int slot = 0; slot < RoomBridge.JoinedPlayers.Count; slot++)
        {
            JoinedPlayerData data = RoomBridge.JoinedPlayers[slot];
            if (data.eliminated) continue;

            SpawnPlayer(data, slot);
        }
    }

    // Tetap dipertahankan untuk kasus ada PlayerInputManager langsung di scene
    // ini juga (mis. testing tanpa lewat Room/Lobby)
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        SetupPlayer(playerInput, playersJoined);
        playersJoined++;
    }

    private void SpawnPlayer(JoinedPlayerData data, int slot)
    {
        // Instantiate prefab asli, dipasangkan ke device fisik yang SAMA
        // dengan waktu player itu join di room
        PlayerInput playerInput = PlayerInput.Instantiate(
            playerPrefab,
            controlScheme: data.controlScheme,
            pairWithDevices: data.devices
        );

        // Kunci supaya HANYA device yang join room yang mengontrol player ini
        LockToJoinedDevices(playerInput, data.devices);

        SetupPlayer(playerInput, slot);
    }

    // Pastikan player ini hanya dikontrol device miliknya:
    // 1. Matikan auto-switch control scheme
    // 2. Lepas device apapun yang terpasang tapi bukan milik player ini
    private void LockToJoinedDevices(PlayerInput playerInput, InputDevice[] allowedDevices)
    {
        playerInput.neverAutoSwitchControlSchemes = true;

        var user = playerInput.user;

        InputDevice[] paired = user.pairedDevices.ToArray();
        foreach (InputDevice device in paired)
        {
            if (!ContainsDevice(allowedDevices, device))
            {
                Debug.Log($"[PlayerManager] Melepas device '{device.displayName}' yang tidak join room.");
                user.UnpairDevice(device);
            }
        }

        if (user.pairedDevices.Count == 0)
        {
            Debug.LogWarning("[PlayerManager] Player ini tidak punya device terpasang " +
                             "(device mungkin sudah dicabut sebelum game mulai).");
        }
    }

    private static bool ContainsDevice(InputDevice[] devices, InputDevice target)
    {
        if (devices == null || target == null) return false;

        foreach (InputDevice d in devices)
        {
            if (d != null && d.deviceId == target.deviceId) return true;
        }
        return false;
    }

    private void SetupPlayer(PlayerInput playerInput, int slot)
    {
        if (slot >= spawnPoints.Count || slot >= playerTextures.Length)
        {
            Debug.Log("Maksimal player sudah tercapai, spawnPoints kurang, atau Player Textures kurang!");
            Destroy(playerInput.gameObject);
            return;
        }

        // 1. Pindahkan player ke titik spawn sesuai slot
        playerInput.transform.position = spawnPoints[slot].position;

        // 2. Ganti texture (UV) sesuai slot
        ApplyTexture(playerInput.gameObject, playerTextures[slot]);
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
            // r.materials membuat salinan material khusus renderer ini
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