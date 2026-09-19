using UnityEngine;
using UnityEngine.InputSystem;

public class RhythmController : MonoBehaviour
{
    private PlayerInput playerInput;

    // Tidak perlu public lagi karena akan dicari otomatis oleh script
    private HitZone jalur1Zone;
    private HitZone jalur2Zone;
    private HitZone jalur3Zone;
    private HitZone jalur4Zone;

    private InputAction hitAAction;
    private InputAction hitXAction;
    private InputAction hitBAction;
    private InputAction hitYAction;

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();

        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap("Rhythm");

            hitAAction = playerInput.actions["HitA"];
            hitXAction = playerInput.actions["HitX"];
            hitBAction = playerInput.actions["HitB"];
            hitYAction = playerInput.actions["HitY"];

            // ==== SISTEM PENCARI OTOMATIS (MULTIPLAYER) ====
            // playerIndex dimulai dari 0 (P1), jadi kita tambah 1 agar menjadi 1, 2, 3, 4
            int pcID = playerInput.playerIndex + 1;

            // Script otomatis mencari jalur di Hierarchy berdasarkan nama PC
            GameObject targetA = GameObject.Find("PC" + pcID + "/TargetA");
            if(targetA != null) jalur1Zone = targetA.GetComponent<HitZone>();

            GameObject targetX = GameObject.Find("PC" + pcID + "/TargetX");
            if(targetX != null) jalur2Zone = targetX.GetComponent<HitZone>();

            GameObject targetB = GameObject.Find("PC" + pcID + "/TargetB");
            if(targetB != null) jalur3Zone = targetB.GetComponent<HitZone>();

            GameObject targetY = GameObject.Find("PC" + pcID + "/TargetY");
            if(targetY != null) jalur4Zone = targetY.GetComponent<HitZone>();
            // ===============================================

            Debug.Log("Player " + pcID + " berhasil terhubung ke PC" + pcID);
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
}