using UnityEngine;
using UnityEngine.InputSystem;

public class RhythmController : MonoBehaviour
{
    private PlayerInput playerInput;

    [Header("Hubungkan 4 Kotak Target (HitZone) di sini")]
    public HitZone jalur1Zone; // Tombol A (HitA)
    public HitZone jalur2Zone; // Tombol X (HitX)
    public HitZone jalur3Zone; // Tombol B (HitB)
    public HitZone jalur4Zone; // Tombol Y (HitY)

    private InputAction hitAAction;
    private InputAction hitXAction;
    private InputAction hitBAction;
    private InputAction hitYAction;

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();

        if (playerInput != null)
        {
            // Pindah ke mode Rhythm (mematikan tombol jalan/interaksi)
            playerInput.SwitchCurrentActionMap("Rhythm");

            // Menyambungkan dengan Action di Input System
            hitAAction = playerInput.actions["HitA"];
            hitXAction = playerInput.actions["HitX"];
            hitBAction = playerInput.actions["HitB"];
            hitYAction = playerInput.actions["HitY"];
        }
    }

    void Update()
    {
        if (hitAAction != null && hitAAction.WasPressedThisFrame()) CheckHit(1);
        if (hitXAction != null && hitXAction.WasPressedThisFrame()) CheckHit(2);
        if (hitBAction != null && hitBAction.WasPressedThisFrame()) CheckHit(3);
        if (hitYAction != null && hitYAction.WasPressedThisFrame()) CheckHit(4);
    }

    void CheckHit(int jalur)
    {
        if (jalur == 1 && jalur1Zone != null) jalur1Zone.AttemptHit();
        else if (jalur == 2 && jalur2Zone != null) jalur2Zone.AttemptHit();
        else if (jalur == 3 && jalur3Zone != null) jalur3Zone.AttemptHit();
        else if (jalur == 4 && jalur4Zone != null) jalur4Zone.AttemptHit();
    }

    // Panggil fungsi ini jika minigame selesai agar player bisa jalan lagi
    public void SelesaiMainRhythm()
    {
        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap("Player");
        }
    }
}