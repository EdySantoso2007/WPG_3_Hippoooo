using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    private PlayerInput playerInput;
    private InputAction interactAction;

    // Semua kotak di lantai yang sedang masuk sphere trigger (yang besar)
    private readonly HashSet<GameObject> nearbyBoxes = new HashSet<GameObject>();
    private GameObject carriedBox;

    // Buffer dipakai ulang supaya OverlapSphere tidak membuat array baru tiap tekan Interact
    private readonly Collider[] dropHits = new Collider[16];

    public Transform holdPoint;

    [Header("Deteksi Taruh / Curi Kotak (Drop Zone)")]
    [Tooltip("Jarak maksimum (bidang datar) dari posisi tubuh karakter ke Drop Zone. " +
             "Kecil = karakter harus benar-benar berdiri di dalam / menempel zona.")]
    public float dropRadius = 0.4f;

    [Header("SFX")]
    public AudioClip pickupSfx;
    public AudioClip dropSfx;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    private AudioSource audioSource;

    // --- Animator ---
    private Animator animator;
    private PlayerMovement playerMovement;
    private static readonly int IsCarryingHash = Animator.StringToHash("IsCarrying");

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        interactAction = playerInput.actions["Interact"];
        playerMovement = GetComponent<PlayerMovement>();

        audioSource = GetComponent<AudioSource>();
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

    // Cari Drop Zone tempat karakter BERDIRI.
    // Patokannya posisi tubuh karakter (bukan titik di depannya), diukur di bidang datar (XZ).
    // - Zona yang berisi posisi karakter punya jarak 0, jadi pasti menang.
    // - Kalau ada beberapa zona yang jaraknya sama (misalnya tumpang tindih),
    //   dipilih yang pusatnya paling dekat dengan karakter.
    private BoxStorage GetNearestDropZone()
    {
        Vector3 pos = transform.position;
        int hitCount = Physics.OverlapSphereNonAlloc(pos, dropRadius + 1f, dropHits, ~0, QueryTriggerInteraction.Collide);

        BoxStorage best = null;
        float bestEdge = float.MaxValue;
        float bestCenter = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider h = dropHits[i];
            if (!h.CompareTag("DropZone")) continue;

            BoxStorage storage = h.GetComponent<BoxStorage>();
            if (storage == null) continue;

            // Samakan tinggi dengan collider zona supaya hanya posisi datar (XZ) yang dihitung
            Vector3 flatPos = new Vector3(pos.x, h.bounds.center.y, pos.z);

            Vector3 closest = h.ClosestPoint(flatPos);
            float edgeDist = (closest - flatPos).magnitude; // 0 kalau karakter ada di dalam zona
            if (edgeDist > dropRadius) continue;

            float centerDist = (h.bounds.center - flatPos).sqrMagnitude;

            bool better = edgeDist < bestEdge - 0.0001f ||
                          (Mathf.Abs(edgeDist - bestEdge) <= 0.0001f && centerDist < bestCenter);
            if (better)
            {
                bestEdge = edgeDist;
                bestCenter = centerDist;
                best = storage;
            }
        }
        return best;
    }

    // Cari kotak di lantai yang PALING DEKAT dengan karakter
    private GameObject GetNearestBox()
    {
        GameObject best = null;
        float bestDist = float.MaxValue;
        List<GameObject> dead = null;

        foreach (GameObject b in nearbyBoxes)
        {
            if (b == null || !b.activeInHierarchy || !b.CompareTag("Box"))
            {
                if (dead == null) dead = new List<GameObject>();
                dead.Add(b);
                continue;
            }

            float d = (b.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = b;
            }
        }

        if (dead != null)
            foreach (GameObject d in dead) nearbyBoxes.Remove(d);

        return best;
    }

    void Update()
    {
        // CEK STUN: Jika sedang stun, batalkan semua perintah interaksi
        if (playerMovement != null && playerMovement.isStunned) return;

        if (interactAction != null && interactAction.WasPressedThisFrame())
        {
            BoxStorage dropZone = GetNearestDropZone();

            // Jika karakter sedang berada di Drop Zone
            if (dropZone != null)
            {
                // Taruh Kotak
                if (carriedBox != null)
                {
                    // AddBox mengembalikan kotak ke pool (nonaktif + parent diatur pool),
                    // jadi tidak perlu SetParent(null) lagi di sini.
                    dropZone.AddBox(carriedBox);
                    carriedBox = null;

                    PlaySfx(dropSfx);
                }
                // Curi Kotak
                else
                {
                    GameObject stolenBox = dropZone.RemoveLastBox();
                    if (stolenBox != null)
                    {
                        carriedBox = stolenBox;

                        Rigidbody stolenRb = carriedBox.GetComponent<Rigidbody>();
                        if (stolenRb != null) stolenRb.isKinematic = true;
                        Collider stolenCol = carriedBox.GetComponent<Collider>();
                        if (stolenCol != null) stolenCol.enabled = false;

                        carriedBox.transform.SetParent(holdPoint);
                        carriedBox.transform.position = holdPoint.position;
                        carriedBox.transform.localRotation = Quaternion.identity;

                        PlaySfx(pickupSfx);
                    }
                }
            }
            // Jika tidak di Drop Zone (interaksi biasa dengan kotak di lantai)
            else
            {
                // Ambil kotak terdekat dari lantai
                if (carriedBox == null)
                {
                    GameObject target = GetNearestBox();
                    if (target != null)
                    {
                        carriedBox = target;
                        nearbyBoxes.Remove(target);

                        carriedBox.GetComponent<Rigidbody>().isKinematic = true;
                        carriedBox.GetComponent<Collider>().enabled = false;

                        carriedBox.transform.SetParent(holdPoint);
                        carriedBox.transform.position = holdPoint.position;
                        carriedBox.transform.localRotation = Quaternion.identity;

                        PlaySfx(pickupSfx);
                    }
                }
                // Jatuhkan kotak ke lantai
                else
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

    void LateUpdate()
    {
        if (animator != null)
            animator.SetBool(IsCarryingHash, carriedBox != null);
    }

    // Sphere trigger besar HANYA dipakai untuk mendeteksi kotak di lantai.
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Box"))
        {
            nearbyBoxes.Add(other.gameObject);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Box"))
        {
            nearbyBoxes.Remove(other.gameObject);
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

    // Lingkaran dropRadius di sekitar tubuh karakter (terlihat saat player dipilih)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, dropRadius);
    }
}