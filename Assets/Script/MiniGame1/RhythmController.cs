using UnityEngine;
using UnityEngine.InputSystem;

public class RhythmController : MonoBehaviour
{
    // Sekarang dibuat private karena akan dicari otomatis oleh script, tidak perlu ditarik manual lagi
    private QTEManager myQTEManager;

    private PlayerInput playerInput;
    private InputAction hitAAction, hitXAction, hitBAction, hitYAction;

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();

        if (playerInput != null)
        {
            // Mengaktifkan peta kontrol khusus Rhythm
            playerInput.SwitchCurrentActionMap("Rhythm");

            hitAAction = playerInput.actions["HitA"];
            hitXAction = playerInput.actions["HitX"];
            hitBAction = playerInput.actions["HitB"];
            hitYAction = playerInput.actions["HitY"];

            // ==========================================
            // SISTEM PENCARI QTE MANAGER OTOMATIS
            // ==========================================

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
    }

    void Update()
    {
        // Jika manager belum ketemu, hentikan fungsi ini agar tidak error
        if (myQTEManager == null) return;

        // Membaca input dan mengirimnya ke QTEManager PC masing-masing
        if (hitAAction != null && hitAAction.WasPressedThisFrame()) myQTEManager.CheckInput(1);
        if (hitXAction != null && hitXAction.WasPressedThisFrame()) myQTEManager.CheckInput(2);
        if (hitBAction != null && hitBAction.WasPressedThisFrame()) myQTEManager.CheckInput(3);
        if (hitYAction != null && hitYAction.WasPressedThisFrame()) myQTEManager.CheckInput(4);
    }
}