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

    private bool isDashing = false;
    [HideInInspector] public bool isStunned = false;
    private float lastDashTime = -5f;

    private Rigidbody rb;
    private Vector2 moveInput;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction dashAction;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        dashAction = playerInput.actions["Dash"];
    }

    void Update()
    {
        if (Time.timeScale == 0f || isStunned) return;

        if (rb.IsSleeping()) rb.WakeUp();

        moveInput = moveAction.ReadValue<Vector2>();

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
        if (Time.timeScale == 0f || isStunned) return;

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

    private IEnumerator PerformDash()
    {
        isDashing = true;
        lastDashTime = Time.time;
        yield return new WaitForSeconds(dashDuration);
        isDashing = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isDashing && collision.gameObject.CompareTag("Player"))
        {
            PlayerMovement otherMovement = collision.gameObject.GetComponent<PlayerMovement>();

            // Pastikan musuh belum dalam keadaan stun untuk mencegah tabrakan beruntun
            if (otherMovement != null && !otherMovement.isStunned)
            {
                PlayerInteraction otherPlayer = collision.gameObject.GetComponent<PlayerInteraction>();

                Vector3 bumpDirection = (collision.transform.position - transform.position).normalized;
                bumpDirection.y = 1.2f;

                if (otherPlayer != null)
                {
                    otherPlayer.ForceDropBox(bumpDirection * bumpForce);
                }

                otherMovement.TakeHitAndStun(bumpDirection * playerKnockback, stunDuration);
            }
        }
    }

    public void TakeHitAndStun(Vector3 knockbackForce, float duration)
    {
        StartCoroutine(StunRoutine(knockbackForce, duration));
    }

    private IEnumerator StunRoutine(Vector3 knockbackForce, float duration)
    {
        isStunned = true;

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(knockbackForce, ForceMode.Impulse);

        yield return new WaitForSeconds(duration);

        isStunned = false;
    }
}