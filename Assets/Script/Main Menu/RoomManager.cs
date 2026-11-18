using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Ditempel di GameObject YANG SAMA dengan komponen PlayerInputManager.
//
// PENTING - Setting di komponen PlayerInputManager (Inspector):
// - Player Prefab       : isi dengan prefab PLACEHOLDER kosong (cukup ada
//   komponen PlayerInput saja, TANPA Renderer/visual apapun). Prefab ini
//   cuma numpang lewat untuk mendeteksi device yang join, lalu langsung
//   dihancurkan di OnPlayerJoined di bawah - jadi TIDAK akan kelihatan.
// - Notification Behavior : Send Messages
// - Joining Behavior      : sesuai kebutuhan
//
// Joining dimatikan sejak Awake(), dan dibuka otomatis begitu roomPanel aktif.
// Saat tombol kembali ditekan, panggil ResetRoomState() untuk menghapus semua
// player yang sudah join.
[RequireComponent(typeof(PlayerInputManager))]
public class RoomManager : MonoBehaviour
{
    [Header("Scene Tujuan")]
    [Tooltip("Nama scene minigame yang di-load setelah tombol START ditekan")]
    public string gameSceneName = "MainScenery";

    [Header("UI Checklist (urutan WAJIB: Merah, Biru, Hijau, Kuning)")]
    [Tooltip("GameObject icon check hijau di bawah tiap warna pada billboard")]
    public GameObject[] checkIcons = new GameObject[4];

    [Header("Tombol Start")]
    public Button startButton;
    [Tooltip("Minimal jumlah player yang harus join sebelum tombol Start bisa ditekan")]
    public int minPlayersToStart = 1;

    [Header("Panel Room")]
    [Tooltip("Panel room di Canvas. Begitu panel ini aktif, joining otomatis dibuka.")]
    public GameObject roomPanel;

    private PlayerInputManager playerInputManager;
    private bool gameStarting = false;
    private bool roomActive = false;        // true setelah ActivateRoom() dipanggil
    private bool blockAutoActivate = false; // true setelah reset, sampai panel room benar-benar tertutup

    private void Awake()
    {
        playerInputManager = GetComponent<PlayerInputManager>();

        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        ResetRoomState();
        blockAutoActivate = false; // reset awal bukan karena tombol kembali
    }

    // Hapus semua player yang sudah join, matikan joining, dan kembalikan UI room ke kondisi awal.
    // Panggil dari tombol kembali.
    public void ResetRoomState()
    {
        // Hapus data device yang sudah tercatat
        RoomBridge.ResetRoom();

        // Matikan semua icon check
        foreach (GameObject icon in checkIcons)
        {
            if (icon != null) icon.SetActive(false);
        }

        // Tombol Start nonaktif lagi sampai ada yang join
        if (startButton != null)
            startButton.interactable = false;

        // Tutup joining sampai panel room dibuka lagi
        roomActive = false;
        blockAutoActivate = true;
        if (playerInputManager != null)
            playerInputManager.DisableJoining();
    }

    // Bisa juga dipanggil manual dari tombol/logic yang menampilkan panel room.
    public void ActivateRoom()
    {
        if (roomActive || gameStarting) return;

        roomActive = true;

        if (playerInputManager != null)
            playerInputManager.EnableJoining();
    }

    private void Update()
    {
        if (gameStarting) return;

        // Panel room sudah tertutup -> boleh dibuka otomatis lagi saat panel muncul berikutnya
        if (roomPanel != null && !roomPanel.activeInHierarchy)
            blockAutoActivate = false;

        // Otomatis buka joining begitu panel room aktif
        if (!roomActive && !blockAutoActivate && roomPanel != null && roomPanel.activeInHierarchy)
            ActivateRoom();

        // Tombol A = Start (hanya jika room aktif dan syarat minimal player terpenuhi)
        if (!roomActive) return;

        var gamepad = Gamepad.current;
        if (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame)
        {
            if (RoomBridge.JoinedPlayers.Count >= minPlayersToStart)
                StartGame();
        }
    }

    // Dipanggil OTOMATIS oleh PlayerInputManager tiap ada device baru yang join.
    // "playerInput" di sini cuma placeholder kosong -> catat info device-nya saja.
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        if (gameStarting)
        {
            Destroy(playerInput.gameObject);
            return;
        }

        // Join yang lolos sebelum panel room aktif (atau setelah reset) ditolak
        if (!roomActive)
        {
            Debug.Log("Panel room belum aktif, join diabaikan.");
            Destroy(playerInput.gameObject);
            return;
        }

        // Tolak kalau device ini sudah pernah join
        if (IsDeviceAlreadyJoined(playerInput))
        {
            Debug.Log("Device ini sudah join sebelumnya, join baru diabaikan.");
            Destroy(playerInput.gameObject);
            return;
        }

        if (RoomBridge.JoinedPlayers.Count >= checkIcons.Length)
        {
            Debug.Log($"Maksimal {checkIcons.Length} Player, join ditolak.");
            Destroy(playerInput.gameObject);
            return;
        }

        int index = RoomBridge.JoinedPlayers.Count;

        // Simpan HANYA data control scheme & device fisiknya, bukan gameobject-nya
        RoomBridge.JoinedPlayers.Add(new JoinedPlayerData
        {
            controlScheme = playerInput.currentControlScheme,
            devices = playerInput.devices.ToArray()
        });

        string scheme = playerInput.currentControlScheme;

        // Placeholder sudah selesai tugasnya
        Destroy(playerInput.gameObject);

        // Nyalakan icon check sesuai urutan warna join: Merah, Biru, Hijau, Kuning
        if (checkIcons[index] != null)
            checkIcons[index].SetActive(true);

        // Aktifkan tombol Start begitu syarat minimal jumlah player terpenuhi
        if (startButton != null && RoomBridge.JoinedPlayers.Count >= minPlayersToStart)
            startButton.interactable = true;

        Debug.Log($"Device join ke room, urutan ke-{index + 1}, control scheme: {scheme}");
    }

    // Cek apakah salah satu device fisik di playerInput ini sudah tercatat di RoomBridge
    private bool IsDeviceAlreadyJoined(PlayerInput playerInput)
    {
        foreach (JoinedPlayerData data in RoomBridge.JoinedPlayers)
        {
            foreach (InputDevice newDevice in playerInput.devices)
            {
                foreach (InputDevice existingDevice in data.devices)
                {
                    if (newDevice.deviceId == existingDevice.deviceId)
                        return true;
                }
            }
        }
        return false;
    }

    // Dipanggil dari tombol START (di-wire lewat AddListener di Awake)
    private void StartGame()
    {
        if (gameStarting) return;
        if (RoomBridge.JoinedPlayers.Count < minPlayersToStart) return;

        gameStarting = true;

        // Stop nerima player baru begitu game mau mulai
        if (playerInputManager != null)
            playerInputManager.DisableJoining();

        SceneManager.LoadScene(gameSceneName);
    }
}