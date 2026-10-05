using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    private PlayerInput playerInput;
    private InputAction interactAction;

    private GameObject boxToPickUp;
    private GameObject carriedBox;
    private BoxStorage currentDropZone;

    public Transform holdPoint;

    [Header("SFX")]
    public AudioClip pickupSfx;
    public AudioClip dropSfx;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    private AudioSource audioSource;

    // --- Animator ---
    private Animator animator;
    private static readonly int IsCarryingHash = Animator.StringToHash("IsCarrying");

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        interactAction = playerInput.actions["Interact"];

        // Pakai AudioSource di player jika ada, kalau tidak ada tetap bunyi lewat PlayClipAtPoint
        audioSource = GetComponent<AudioSource>();

        // Animator ada di anak (WorkerHippo)
        animator = GetComponentInChildren<Animator>();
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;

        if (audioSource != null)
            audioSource.PlayOneShot(clip, sfxVolume);
        else
            AudioSource.PlayClipAtPoint(clip, transform.position, sfxVolume);
    }

    void Update()
    {
        // CEK STUN: Jika sedang stun, batalkan semua perintah interaksi
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null && pm.isStunned) return;

        if (interactAction != null && interactAction.WasPressedThisFrame())
        {
            // Jika sedang di dalam area Drop Zone
            if (currentDropZone != null)
            {
                // Taruh Kotak
                if (carriedBox != null)
                {
                    currentDropZone.AddBox(carriedBox);

                    carriedBox.transform.SetParent(null);
                    carriedBox = null;

                    PlaySfx(dropSfx);
                }
                // Curi Kotak
                else
                {
                    GameObject stolenBox = currentDropZone.RemoveLastBox();
                    if (stolenBox != null)
                    {
                        carriedBox = stolenBox;

                        // Samakan dengan ambil dari lantai: matikan fisika kotak saat dibawa
                        Rigidbody stolenRb = carriedBox.GetComponent<Rigidbody>();
                        if (stolenRb != null) stolenRb.isKinematic = true;
                        Collider stolenCol = carriedBox.GetComponent<Collider>();
                        if (stolenCol != null) stolenCol.enabled = false;

                        // FIX POSISI: Jadikan child dulu, lalu samakan posisinya dengan Hold Point
                        carriedBox.transform.SetParent(holdPoint);
                        carriedBox.transform.position = holdPoint.position; // Kembali pakai ini agar presisi!
                        carriedBox.transform.localRotation = Quaternion.identity;

                        PlaySfx(pickupSfx);
                    }
                }
            }
            // Jika sedang di luar Drop Zone (Interaksi biasa dengan kotak di lantai)
            else
            {
                // Ambil kotak dari lantai
                if (carriedBox == null && boxToPickUp != null)
                {
                    carriedBox = boxToPickUp;
                    boxToPickUp = null;

                    carriedBox.GetComponent<Rigidbody>().isKinematic = true;
                    carriedBox.GetComponent<Collider>().enabled = false;

                    // FIX POSISI: Jadikan child dulu, lalu samakan posisinya dengan Hold Point
                    carriedBox.transform.SetParent(holdPoint);
                    carriedBox.transform.position = holdPoint.position; // Kembali pakai ini agar presisi!
                    carriedBox.transform.localRotation = Quaternion.identity;

                    PlaySfx(pickupSfx);
                }
                // Jatuhkan kotak ke lantai
                else if (carriedBox != null)
                {
                    carriedBox.transform.SetParent(null);

                    Rigidbody rb = carriedBox.GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = false;

                    Collider col = carriedBox.GetComponent<Collider>();
                    if (col != null) col.enabled = true;

                    carriedBox.tag = "Box";
                    carriedBox = null;

                    PlaySfx(dropSfx);
                }
            }
        }
    }

    // Kirim status "membawa kotak" ke Animator setiap frame.
    // Ditaruh di LateUpdate supaya semua kasus ikut tercakup
    // (ambil, taruh, curi, dan jatuh karena tabrakan dash).
    void LateUpdate()
    {
        if (animator != null)
            animator.SetBool(IsCarryingHash, carriedBox != null);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Box"))
        {
            boxToPickUp = other.gameObject;
        }
        else if (other.CompareTag("DropZone"))
        {
            currentDropZone = other.GetComponent<BoxStorage>();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Box") && boxToPickUp == other.gameObject)
        {
            boxToPickUp = null;
        }
        else if (other.CompareTag("DropZone"))
        {
            BoxStorage zone = other.GetComponent<BoxStorage>();
            if (currentDropZone == zone)
            {
                currentDropZone = null;
            }
        }
    }

    // Dipanggil saat ditabrak (Dash) oleh pemain lain
    public void ForceDropBox(Vector3 throwForce)
    {
        if (carriedBox != null)
        {
            carriedBox.transform.SetParent(null);

            carriedBox.transform.position = transform.position + new Vector3(0f, 1.5f, 0f);

            Rigidbody rb = carriedBox.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddForce(throwForce, ForceMode.Impulse);
            }

            Collider col = carriedBox.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            carriedBox.tag = "Box";
            carriedBox = null;
        }
    }
}