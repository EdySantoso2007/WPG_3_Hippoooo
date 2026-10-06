using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Pengaturan Gerak")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 15f;

    [Header("Pengaturan Dash & Tabrakan")]
    public float dashForce = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 3f;
    public float bumpForce = 12f;         // Kekuatan memental kotak musuh
    public float playerKnockback = 2f;    // Kekuatan pentalan tubuh player
    public float stunDuration = 2f;       // Lama waktu stun
    [Tooltip("Seberapa searah dash dengan lawan agar dianggap kena (1 = tepat di depan, 0 = samping, -1 = belakang). Naikkan jika pemain di samping ikut kena.")]
    [Range(-1f, 1f)] public float minHitDirection = 0.1f;

    [Header("SFX")]
    public AudioClip dashSfx;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    private AudioSource audioSource;

    private bool isDashing = false;
    [HideInInspector] public bool isStunned = false;
    private float lastDashTime = -5f;

    private Rigidbody rb;
    private Vector2 moveInput;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction dashAction;

    // --- Animator ---
    private Animator animator;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsDashingHash = Animator.StringToHash("IsDashing");
    private static readonly int IsStunnedHash = Animator.StringToHash("IsStunned");

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        dashAction = playerInput.actions["Dash"];
        audioSource = GetComponent<AudioSource>();

        // Animator ada di anak (WorkerHippo)
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        // Saat stun: diam, animasi Speed kembali 0
        if (isStunned)
        {
            if (animator != null) animator.SetFloat(SpeedHash, 0f);
            return;
        }

        if (rb.IsSleeping()) rb.WakeUp();

        moveInput = moveAction.ReadValue<Vector2>();

        // Kirim kecepatan ke Animator (0 = diam, 1 = lari penuh)
        if (animator != null)
            animator.SetFloat(SpeedHash, moveInput.magnitude);

        if (dashAction != null && dashAction.WasPressedThisFrame())
        {
            if (Time.time >= lastDashTime + dashCooldown && !isDashing)
            {
                StartCoroutine(PerformDash());
            }
        }
    }

    void FixedUpdate()
    {
        if (Time.timeScale == 0f) return;

        // Cegah karakter berputar sendiri akibat fisika (rotasi diatur lewat script)
        rb.angularVelocity = Vector3.zero;

        if (isStunned) return;

        if (isDashing)
        {
            rb.linearVelocity = transform.forward * dashForce;
            return;
        }

        // Pergerakan Normal
        rb.linearVelocity = new Vector3(moveInput.x * moveSpeed, rb.linearVelocity.y, moveInput.y * moveSpeed);

        // Rotasi Normal
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;

        if (audioSource != null)
            audioSource.PlayOneShot(clip, sfxVolume);
        else
            AudioSource.PlayClipAtPoint(clip, transform.position, sfxVolume);
    }

    private IEnumerator PerformDash()
    {
        isDashing = true;
        lastDashTime = Time.time;
        if (animator != null) animator.SetBool(IsDashingHash, true);

        PlaySfx(dashSfx);

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
        if (animator != null) animator.SetBool(IsDashingHash, false);
    }

    // Terpanggil saat dua collider MULAI bersentuhan (dash dari jauh)
    void OnCollisionEnter(Collision collision)
    {
        TryHitPlayer(collision);
    }

    // Terpanggil TERUS selama masih bersentuhan (dash saat sudah menempel).
    // Inilah perbaikan untuk kasus pemain yang sudah menempel sebelum dash.
    void OnCollisionStay(Collision collision)
    {
        TryHitPlayer(collision);
    }

    private void TryHitPlayer(Collision collision)
    {
        if (!isDashing || isStunned) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        PlayerMovement otherMovement = collision.gameObject.GetComponent<PlayerMovement>();

        // Pastikan musuh belum stun untuk mencegah tabrakan beruntun
        if (otherMovement == null || otherMovement.isStunned) return;

        // Hanya kena jika lawan berada di arah dash (bukan di samping / belakang)
        Vector3 toOther = collision.transform.position - transform.position;
        toOther.y = 0f;
        if (toOther.sqrMagnitude > 0.0001f)
        {
            if (Vector3.Dot(transform.forward, toOther.normalized) < minHitDirection) return;
        }

        PlayerInteraction otherPlayer = collision.gameObject.GetComponent<PlayerInteraction>();

        Vector3 bumpDirection = (collision.transform.position - transform.position).normalized;
        bumpDirection.y = 1.2f;

        if (otherPlayer != null)
        {
            otherPlayer.ForceDropBox(bumpDirection * bumpForce);
        }

        otherMovement.TakeHitAndStun(bumpDirection * playerKnockback, stunDuration);
    }

    public void TakeHitAndStun(Vector3 knockbackForce, float duration)
    {
        StartCoroutine(StunRoutine(knockbackForce, duration));
    }

    private IEnumerator StunRoutine(Vector3 knockbackForce, float duration)
    {
        isStunned = true;
        moveInput = Vector2.zero;
        if (animator != null) animator.SetBool(IsStunnedHash, true);

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(knockbackForce, ForceMode.Impulse);

        yield return new WaitForSeconds(duration);

        isStunned = false;
        if (animator != null) animator.SetBool(IsStunnedHash, false);
    }
}