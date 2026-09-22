using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Ditempel di GameObject YANG SAMA dengan komponen PlayerInputManager.
//
// Setting wajib di komponen PlayerInputManager (Inspector):
// - Notification Behavior : Send Messages
//   (supaya method OnPlayerJoined di bawah ini otomatis terpanggil)
// - Joining Behavior       : sesuaikan kebutuhan
//   (mis. "Join Players When Button Is Pressed", biar player pencet
//    tombol apapun di controller-nya untuk join room)
[RequireComponent(typeof(PlayerInputManager))]
public class RoomManager : MonoBehaviour
{
    [Header("Scene Tujuan")]
    [Tooltip("Nama scene gameplay yang di-load setelah tombol START ditekan")]
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
    }

    // Dipanggil OTOMATIS oleh PlayerInputManager (Notification Behavior: Send Messages)
    // setiap kali ada controller/device baru yang join ke room.
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        if (gameStarting) return;

        if (RoomBridge.JoinedPlayers.Count >= checkIcons.Length)
        {
            Debug.Log($"Maksimal {checkIcons.Length} Player, join ditolak.");
            Destroy(playerInput.gameObject);
            return;
        }

        int index = RoomBridge.JoinedPlayers.Count;
        RoomBridge.JoinedPlayers.Add(playerInput);

        // Player ini akan ikut pindah ke scene gameplay, jangan sampai
        // dihancurkan otomatis saat scene lobby di-unload
        DontDestroyOnLoad(playerInput.gameObject);

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

        Debug.Log($"Player join ke room, urutan ke-{index + 1} ({playerInput.currentControlScheme})");
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

        // Load scene gameplay. RoomManager & PlayerInputManager di scene ini
        // otomatis ikut dihancurkan Unity karena tidak di-DontDestroyOnLoad,
        // sedangkan player yang sudah join (RoomBridge.JoinedPlayers) tetap ada.
        SceneManager.LoadScene(gameSceneName);
    }
}