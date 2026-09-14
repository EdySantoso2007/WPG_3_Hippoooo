using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; // Ditambahkan untuk sistem Coroutine (Waktu tunggu)

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Pengaturan Gerak")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 15f;

    [Header("Pengaturan Dash")]
    public float dashForce = 15f;      // Kecepatan melesat
    public float dashDuration = 0.2f;  // Lama waktu melesat
    public float dashCooldown = 3f;    // Waktu tunggu sebelum bisa dash lagi
    public float bumpForce = 12f;      // Kekuatan memental kotak musuh

    private bool isDashing = false;
    private float lastDashTime = -5f; // Diatur minus agar saat awal main langsung bisa dash

    private Rigidbody rb;
    private Vector2 moveInput;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction dashAction; // Variabel baru untuk tombol Dash

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];

        // Mengambil aksi Dash dari Input System
        dashAction = playerInput.actions["Dash"];
    }

    void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();

        // Mengecek apakah tombol Dash ditekan DAN cooldown sudah selesai
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
        // Jika sedang dash, jangan gunakan pergerakan normal
        if (isDashing)
        {
            // Melesat ke arah depan karakter
            rb.linearVelocity = transform.forward * dashForce;
            return; // Lewati kode pergerakan di bawah
        }

        // 1. Sistem Pergerakan Normal
        rb.linearVelocity = new Vector3(moveInput.x * moveSpeed, rb.linearVelocity.y, moveInput.y * moveSpeed);

        // 2. Sistem Rotasi Normal
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    // Sistem pengatur waktu melesat
    private IEnumerator PerformDash()
    {
        isDashing = true;
        lastDashTime = Time.time; // Catat waktu kapan dash dilakukan

        yield return new WaitForSeconds(dashDuration); // Tunggu sepersekian detik

        isDashing = false; // Berhenti melesat
    }

    // Mendeteksi tabrakan saat bermain
    void OnCollisionEnter(Collision collision)
    {
        // Jika kita KITA yang sedang dash, dan menabrak PEMAIN LAIN
        if (isDashing && collision.gameObject.CompareTag("Player"))
        {
            // Ambil script Interaksi milik pemain yang ditabrak
            PlayerInteraction otherPlayer = collision.gameObject.GetComponent<PlayerInteraction>();

            if (otherPlayer != null)
            {
                // Hitung arah pantulan (Menjauh dari kita, dan agak melambung ke atas)
                Vector3 bumpDirection = (collision.transform.position - transform.position).normalized;
                bumpDirection.y = 1.5f; // Efek melambung ke atas

                // Paksa pemain lain menjatuhkan kotaknya dengan kekuatan pantulan
                otherPlayer.ForceDropBox(bumpDirection * bumpForce);
            }
        }
    }
}