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
// - Joining Behavior      : sesuai kebutuhan (mis. Join Action Is Triggered,
//   sesuai binding E/Kotak/X yang sudah di-setup)
//
// PENTING - Cara mencegah join sebelum panel room aktif:
// Script ini akan MEMATIKAN joining sejak Awake(). Supaya player baru bisa
// join setelah panel room benar-benar muncul, panggil method
// ActivateRoom() ini dari tombol/script yang menampilkan panel room
// tersebut (misal di OnClick tombol PLAY di Main Menu, tambahkan satu
// event lagi yang manggil RoomManager.ActivateRoom()).
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

    private PlayerInputManager playerInputManager;
    private bool gameStarting = false;
    private bool roomActive = false; // true setelah ActivateRoom() dipanggil

    private void Awake()
    {
        playerInputManager = GetComponent<PlayerInputManager>();

        // Pastikan data room fresh setiap kali scene lobby ini dibuka dari awal
        RoomBridge.ResetRoom();

        // Matikan semua icon check di awal (belum ada yang join)
        foreach (GameObject icon in checkIcons)
        {
            if (icon != null) icon.SetActive(false);
        }

        if (startButton != null)
        {
            startButton.interactable = false;
            startButton.onClick.AddListener(StartGame);
        }

        // Kunci joining sejak awal - baru dibuka lewat ActivateRoom()
        playerInputManager.DisableJoining();
    }

    // Panggil method ini dari tombol/logic yang menampilkan panel room
    // (mis. tombol PLAY di Main Menu), supaya player baru bisa join
    // SETELAH panel room benar-benar aktif/tampil.
    public void ActivateRoom()
    {
        if (roomActive) return;

        roomActive = true;

        if (playerInputManager != null)
        {
            playerInputManager.EnableJoining();
        }
    }

    private void Update()
    {
        if (gameStarting) return;

        // A = Start (hanya jika syarat minimal player sudah terpenuhi)
        var gamepad = Gamepad.current;
        if (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame)
        {
            if (RoomBridge.JoinedPlayers.Count >= minPlayersToStart)
            {
                StartGame();
            }
        }
    }

    // Dipanggil OTOMATIS oleh PlayerInputManager tiap ada device baru yang join.
    // "playerInput" di sini cuma placeholder kosong (lihat catatan di atas)
    // -> catat info device-nya saja, JANGAN dianggap sebagai player asli.
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        if (gameStarting) return;

        // Jaga-jaga: kalau entah bagaimana masih ada join yang lolos sebelum
        // panel room aktif (mis. race condition frame pertama), tolak saja.
        if (!roomActive)
        {
            Debug.Log("Panel room belum aktif, join diabaikan.");
            Destroy(playerInput.gameObject);
            return;
        }

        // Tolak kalau device ini (keyboard/gamepad yang sama) sudah pernah join
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

        // Placeholder ini tugasnya sudah selesai (cuma buat deteksi join),
        // langsung hancurkan supaya tidak ada apapun yang muncul di layar
        Destroy(playerInput.gameObject);

        // Nyalakan icon check sesuai urutan warna join: Merah, Biru, Hijau, Kuning
        if (checkIcons[index] != null)
        {
            checkIcons[index].SetActive(true);
        }

        // Aktifkan tombol Start begitu syarat minimal jumlah player terpenuhi
        if (startButton != null && RoomBridge.JoinedPlayers.Count >= minPlayersToStart)
        {
            startButton.interactable = true;
        }

        Debug.Log($"Device join ke room, urutan ke-{index + 1}, control scheme: {playerInput.currentControlScheme}");
    }

    // Cek apakah salah satu device fisik di playerInput ini sudah tercatat
    // sebelumnya di RoomBridge (dari player lain yang sudah join duluan)
    private bool IsDeviceAlreadyJoined(PlayerInput playerInput)
    {
        foreach (JoinedPlayerData data in RoomBridge.JoinedPlayers)
        {
            foreach (InputDevice newDevice in playerInput.devices)
            {
                foreach (InputDevice existingDevice in data.devices)
                {
                    if (newDevice.deviceId == existingDevice.deviceId)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    // Dipanggil dari tombol START (sudah di-wire otomatis lewat AddListener di Awake)
    private void StartGame()
    {
        if (RoomBridge.JoinedPlayers.Count < minPlayersToStart) return;

        gameStarting = true;

        // Stop nerima player baru begitu game mau mulai
        if (playerInputManager != null)
        {
            playerInputManager.DisableJoining();
        }

        // Load scene minigame. Prefab player yang sesungguhnya baru akan
        // di-spawn di sana oleh PlayerManager, berdasarkan RoomBridge.JoinedPlayers
        SceneManager.LoadScene(gameSceneName);
    }
}