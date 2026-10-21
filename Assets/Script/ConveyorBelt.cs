using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ConveyorBelt : MonoBehaviour
{
    [Header("Jalur (disarankan untuk conveyor yang berbelok)")]
    [Tooltip("Isi dengan objek ConveyorPath. Kalau diisi, arah kotak / player mengikuti jalur ini " +
             "dan pengaturan Direction & Priority di bawah diabaikan.")]
    public ConveyorPath path;

    [Header("Arah (dipakai kalau Path kosong)")]
    public Vector3 direction = Vector3.forward;
    [Tooltip("Centang: arah dihitung mengikuti rotasi segmen (local). Hilangkan centang: arah dunia (world).")]
    public bool useLocalDirection = true;
    [Tooltip("Dipakai saat dua segmen tumpang tindih (hanya kalau Path kosong): segmen dengan angka LEBIH BESAR yang menentukan arah kotak.")]
    public int priority = 0;

    [Header("Kotak")]
    [Tooltip("Kecepatan (meter/detik) kotak dibawa conveyor.")]
    public float boxSpeed = 2f;
    [Tooltip("Seberapa cepat kecepatan kotak menyesuaikan ke arah conveyor. Besar = belokan lebih tegas. " +
             "Isi 0 = langsung mencapai Box Speed (disarankan kalau kotak terasa lambat).")]
    public float boxAcceleration = 0f;
    [Tooltip("Tarikan ke garis tengah conveyor, supaya kotak tidak menempel ke tepi / keluar jalur. 0 = mati.")]
    public float centerPull = 4f;
    [Tooltip("Putar kotak pelan-pelan supaya sejajar dengan arah conveyor.")]
    public bool alignRotation = true;
    [Tooltip("Kecepatan memutar kotak (derajat/detik).")]
    public float alignSpeed = 270f;

    [Header("Player")]
    [Tooltip("Kecepatan (meter/detik) yang membawa player saat berdiri di conveyor.")]
    public float playerSpeed = 2f;

    private Collider myCollider;

    void Awake()
    {
        myCollider = GetComponent<Collider>();
    }

    Vector3 Direction
    {
        get
        {
            Vector3 d = useLocalDirection ? transform.TransformDirection(direction) : direction;
            d.y = 0f; // conveyor hanya membawa di bidang datar
            return d.normalized;
        }
    }

    // Hitung arah aliran di posisi 'pos' dan vektor koreksi menuju garis tengah conveyor.
    void GetFlow(Vector3 pos, out Vector3 dir, out Vector3 toCenter)
    {
        // Mode jalur: semua segmen menghitung hasil yang sama, jadi tidak ada rebutan arah di sambungan
        if (path != null && path.Sample(pos, out Vector3 closest, out Vector3 tangent))
        {
            dir = tangent;
            toCenter = new Vector3(closest.x - pos.x, 0f, closest.z - pos.z);
            return;
        }

        // Mode arah tetap per segmen
        dir = Direction;
        toCenter = Vector3.zero;
        if (myCollider != null && dir != Vector3.zero)
        {
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            float offset = Vector3.Dot(pos - myCollider.bounds.center, side);
            toCenter = -side * offset;
        }
    }

    void OnCollisionStay(Collision collision)
    {
        // --- KOTAK ---
        if (collision.gameObject.CompareTag("Box"))
        {
            MoveBox(collision.rigidbody);
            return;
        }

        // --- PLAYER ---
        if (collision.gameObject.CompareTag("Player"))
        {
            // Hanya kalau player menapak di permukaan atas conveyor (bukan menabrak sisinya)
            if (collision.contactCount == 0) return;
            if (Mathf.Abs(collision.GetContact(0).normal.y) < 0.5f) return;

            PlayerMovement pm = collision.gameObject.GetComponent<PlayerMovement>();
            if (pm == null) return;

            GetFlow(collision.transform.position, out Vector3 dir, out Vector3 _);
            pm.ApplyConveyor(dir * playerSpeed);
        }
    }

    void MoveBox(Rigidbody rb)
    {
        if (rb == null || rb.isKinematic) return;

        // Mode jalur: kotak digerakkan oleh BoxPathFollower (mengikuti waypoint).
        // Kalau kotak menyentuh conveyor tapi belum / tidak lagi mengikuti jalur, suruh ia mengikuti lagi.
        if (path != null)
        {
            BoxPathFollower follower = rb.GetComponent<BoxPathFollower>();
            if (follower == null) follower = rb.gameObject.AddComponent<BoxPathFollower>();

            if (!follower.IsFollowing && !follower.Finished)
            {
                follower.speed = boxSpeed;
                follower.Begin(path);
            }
            return;
        }

        // Paksa kotak "bangun" dari Physics Sleep
        rb.WakeUp();

        GetFlow(rb.position, out Vector3 dir, out Vector3 toCenter);
        if (dir == Vector3.zero) return;

        // Tanpa Path: di area tumpang tindih antar segmen, hanya segmen berprioritas tertinggi yang menggerakkan kotak
        if (path == null)
        {
            ConveyorRider rider = rb.GetComponent<ConveyorRider>();
            if (rider == null) rider = rb.gameObject.AddComponent<ConveyorRider>();

            float now = Time.fixedTime;
            if (rider.lastFixedTime == now && rider.lastPriority > priority) return;
            rider.lastFixedTime = now;
            rider.lastPriority = priority;
        }

        // Tarikan pelan ke garis tengah conveyor
        Vector3 pull = centerPull > 0f ? Vector3.ClampMagnitude(toCenter * centerPull, boxSpeed) : Vector3.zero;

        // Kecepatan datar langsung diarahkan ke arah conveyor (bukan ditambah gaya terus-menerus)
        Vector3 v = rb.linearVelocity;
        Vector3 horizontal = new Vector3(v.x, 0f, v.z);
        Vector3 target = dir * boxSpeed + pull;
        float maxDelta = boxAcceleration > 0f ? boxAcceleration * Time.fixedDeltaTime : float.MaxValue;
        horizontal = Vector3.MoveTowards(horizontal, target, maxDelta);
        rb.linearVelocity = new Vector3(horizontal.x, v.y, horizontal.z);

        // Sejajarkan arah kotak dengan conveyor (kotak simetris tiap 90 derajat)
        if (alignRotation)
        {
            float targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float diff = Mathf.DeltaAngle(rb.rotation.eulerAngles.y, targetYaw);
            diff = Mathf.Repeat(diff + 45f, 90f) - 45f; // selisih ke kelipatan 90° terdekat

            float maxStep = alignSpeed * Time.fixedDeltaTime;
            float step = Mathf.Clamp(diff, -maxStep, maxStep);
            rb.MoveRotation(Quaternion.AngleAxis(step, Vector3.up) * rb.rotation);
        }
    }

    // Panah arah conveyor di Scene view (hanya kalau Path kosong)
    void OnDrawGizmos()
    {
        if (path != null) return;

        Vector3 start = transform.position;
        Collider c = GetComponent<Collider>();
        if (c != null) start = c.bounds.center;

        Vector3 end = start + Direction * 1.5f;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.12f);
    }
}