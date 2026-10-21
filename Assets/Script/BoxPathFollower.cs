using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kotak berjalan di sepanjang jalur (ConveyorPath) dengan cara digerakkan langsung (kinematic),
/// bukan didorong fisika. Jadi kotak tidak bisa tersangkut di collider / sambungan / sudut.
/// Kotak antri rapi di belakang kotak lain (jarak = Spacing) dan berhenti di ujung jalur.
/// Berhenti mengikuti jalur otomatis saat diambil pemain. Ditambahkan otomatis oleh BoxSpawner.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BoxPathFollower : MonoBehaviour
{
    [Tooltip("Kecepatan kotak di conveyor (meter/detik). Diisi oleh BoxSpawner.")]
    public float speed = 3f;
    [Tooltip("Jarak minimal antar kotak di jalur (sesuaikan dengan ukuran kotak, sedikit lebih besar).")]
    public float spacing = 1.1f;
    [Tooltip("Kecepatan kotak bergeser ke garis tengah jalur kalau muncul agak jauh dari jalur.")]
    public float centerSpeed = 3f;
    [Tooltip("Putar kotak menghadap arah jalan.")]
    public bool alignToDirection = true;
    public float turnSpeed = 360f;
    [Tooltip("Kotak menempel di permukaan lantai / belt (cek ke bawah dengan raycast).")]
    public bool snapToGround = true;

    [Header("Status (hanya untuk dilihat saat Play)")]
    [SerializeField] private string status = "Belum dimulai";
    [SerializeField] private float progress;

    public bool IsFollowing { get; private set; }
    /// <summary>True kalau kotak sudah sampai di ujung jalur dan diam di sana.</summary>
    public bool Finished { get; private set; }

    // Semua kotak yang sedang di conveyor (berjalan atau sudah diam di ujung)
    private static readonly List<BoxPathFollower> OnBelt = new List<BoxPathFollower>();
    private static readonly RaycastHit[] groundHits = new RaycastHit[8];

    private ConveyorPath path;
    private Rigidbody rb;
    private Collider col;
    private float s;            // jarak dari awal jalur
    private Vector3 offset;     // geseran dari garis tengah (mengecil sampai 0)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        OnBelt.Clear();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    void OnDisable()
    {
        // Kotak kembali ke pool
        IsFollowing = false;
        Finished = false;
        OnBelt.Remove(this);
    }

    /// <summary>Mulai berjalan di jalur dari posisi kotak sekarang.</summary>
    public void Begin(ConveyorPath newPath)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (col == null) col = GetComponent<Collider>();

        path = newPath;
        if (path == null || path.points == null || path.points.Length < 2)
        {
            IsFollowing = false;
            status = "TIDAK MENGIKUTI: path kosong atau kurang dari 2 titik";
            return;
        }

        s = path.GetProgress(rb.position);
        Vector3 center = path.GetPointAt(s, out Vector3 _);
        offset = new Vector3(rb.position.x - center.x, 0f, rb.position.z - center.z);

        Finished = false;
        IsFollowing = true;

        // Digerakkan lewat script (kinematic), bukan fisika
        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (!OnBelt.Contains(this)) OnBelt.Add(this);
        status = "Mengikuti jalur";
    }

    void FixedUpdate()
    {
        // Dibawa pemain -> lepas dari jalur (boleh mengikuti lagi kalau nanti dijatuhkan di conveyor)
        if (transform.parent != null)
        {
            IsFollowing = false;
            Finished = false;
            OnBelt.Remove(this);
            status = "Dibawa pemain";
            return;
        }

        if (!IsFollowing || path == null) return;

        float total = path.TotalLength;

        // Cari kotak di depan (lebih jauh di jalur); tidak boleh lebih dekat dari 'spacing'
        float limit = total;
        for (int i = 0; i < OnBelt.Count; i++)
        {
            BoxPathFollower o = OnBelt[i];
            if (o == null || o == this) continue;

            bool ahead = o.s > s || (Mathf.Approximately(o.s, s) && o.GetInstanceID() > GetInstanceID());
            if (ahead) limit = Mathf.Min(limit, o.s - spacing);
        }

        float newS = Mathf.Min(s + speed * Time.fixedDeltaTime, limit);
        bool blocked = newS <= s + 0.0001f && s < total - 0.001f;
        if (newS > s) s = newS; // tidak pernah mundur

        if (s >= total - 0.001f)
        {
            s = total;
            IsFollowing = false;
            Finished = true;
            status = "Selesai: sampai di ujung jalur";
        }
        else
        {
            status = blocked ? "Antri di belakang kotak lain" : "Berjalan";
        }
        progress = s;

        // Posisi di jalur (+ geseran yang mengecil menuju garis tengah)
        Vector3 point = path.GetPointAt(s, out Vector3 tangent);
        offset = Vector3.MoveTowards(offset, Vector3.zero, centerSpeed * Time.fixedDeltaTime);
        Vector3 target = point + offset;

        float y = rb.position.y;
        if (snapToGround) y = Mathf.MoveTowards(y, GroundY(y), 8f * Time.fixedDeltaTime);

        rb.MovePosition(new Vector3(target.x, y, target.z));

        if (alignToDirection && tangent != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(tangent, Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime));
        }
    }

    // Tinggi tengah kotak supaya alasnya pas di permukaan di bawahnya
    float GroundY(float currentY)
    {
        float halfHeight = col != null ? col.bounds.extents.y : 0.4f;
        Vector3 origin = new Vector3(rb.position.x, currentY + halfHeight + 0.3f, rb.position.z);

        int n = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits, halfHeight + 1.5f, ~0, QueryTriggerInteraction.Ignore);

        float bestDist = float.MaxValue;
        float result = currentY;
        for (int i = 0; i < n; i++)
        {
            Collider c = groundHits[i].collider;
            if (c == null || c.attachedRigidbody == rb) continue;
            if (c.CompareTag("Box") || c.CompareTag("Player")) continue;

            if (groundHits[i].distance < bestDist)
            {
                bestDist = groundHits[i].distance;
                result = groundHits[i].point.y + halfHeight;
            }
        }
        return result;
    }
}