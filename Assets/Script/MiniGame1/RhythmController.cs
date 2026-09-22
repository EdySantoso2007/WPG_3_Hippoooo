using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class RhythmController : MonoBehaviour
{
    [Header("Scene Rhythm")]
    [Tooltip("Nama scene minigame rhythm. QTEManager HANYA dicari kalau scene aktif = ini.")]
    public string rhythmSceneName = "RhythmScene";

    // Dicari otomatis, tidak perlu ditarik manual
    private QTEManager myQTEManager;

    private PlayerInput playerInput;
    private InputAction hitAAction, hitXAction, hitBAction, hitYAction;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        // Dengarkan tiap kali ada scene baru yang selesai di-load
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Kalau saat komponen ini aktif ternyata scene rhythm SUDAH jadi scene
        // aktif (misal object di-enable belakangan), langsung coba connect juga
        if (SceneManager.GetActiveScene().name == rhythmSceneName)
        {
            ConnectToQTEManager();
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Abaikan kalau yang baru di-load bukan scene rhythm
        // (mis. masih di menu, room, atau scene lain)
        if (scene.name != rhythmSceneName) return;

        ConnectToQTEManager();
    }

    // ==========================================
    // SISTEM PENCARI QTE MANAGER OTOMATIS
    // Sekarang hanya jalan saat scene rhythm benar-benar aktif
    // ==========================================
    private void ConnectToQTEManager()
    {
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
        if (playerInput == null) return;

        // Mengaktifkan peta kontrol khusus Rhythm
        playerInput.SwitchCurrentActionMap("Rhythm");

        hitAAction = playerInput.actions["HitA"];
        hitXAction = playerInput.actions["HitX"];
        hitBAction = playerInput.actions["HitB"];
        hitYAction = playerInput.actions["HitY"];

        // playerIndex dimulai dari 0 (Player 1 = 0, Player 2 = 1, dst.)
        // Ditambah 1 agar sesuai dengan nama grup PC-mu (PC1, PC2, PC3, PC4)
        int pcID = playerInput.playerIndex + 1;

        // Mencari path objek di Hierarchy (Contoh: "PC1/QTE_Canvas")
        string targetCanvasName = "PC" + pcID + "/QTE_Canvas";
        GameObject targetCanvas = GameObject.Find(targetCanvasName);

        if (targetCanvas != null)
        {
            myQTEManager = targetCanvas.GetComponent<QTEManager>();
            Debug.Log("Pemain " + pcID + " BERHASIL terhubung ke layar " + targetCanvasName);
        }
        else
        {
            Debug.LogError("GAGAL MENCARI LAYAR! Pastikan nama objek " + targetCanvasName + " benar di Hierarchy.");
        }
    }

    private void Update()
    {
        // Jika manager belum ketemu (atau belum di scene rhythm), hentikan agar tidak error
        if (myQTEManager == null) return;

        // Membaca input dan mengirimnya ke QTEManager PC masing-masing
        if (hitAAction != null && hitAAction.WasPressedThisFrame()) myQTEManager.CheckInput(1);
        if (hitXAction != null && hitXAction.WasPressedThisFrame()) myQTEManager.CheckInput(2);
        if (hitBAction != null && hitBAction.WasPressedThisFrame()) myQTEManager.CheckInput(3);
        if (hitYAction != null && hitYAction.WasPressedThisFrame()) myQTEManager.CheckInput(4);
    }
}