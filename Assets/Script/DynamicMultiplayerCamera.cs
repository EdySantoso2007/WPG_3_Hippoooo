using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class DynamicMultiplayerCamera : MonoBehaviour
{
    [Header("Target Pemain")]
    public List<Transform> targets = new List<Transform>();

    [Header("Pengaturan Gerak (Posisi)")]
    public Vector3 offset = new Vector3(0, 10, -10);
    public float smoothTime = 0.5f;

    [Header("Pengaturan Zoom (Field of View)")]
    public float minZoom = 40f;
    public float maxZoom = 60f;
    public float minDistanceBeforeZoom = 15f;
    public float zoomLimiter = 20f;

    [Header("Pengaturan Efek Getar Halus (Continuous Shake)")]
    public bool enableContinuousShake = true;
    [Tooltip("Kekuatan getaran. Gunakan angka kecil misal 0.1 atau 0.5")]
    public float continuousShakePower = 0.2f;
    [Tooltip("Seberapa cepat getarannya berayun")]
    public float continuousShakeSpeed = 2f;

    [Header("Batas Map (agar luar map tidak terlihat)")]
    public bool useMapBounds = true;
    [Tooltip("Pojok kiri-bawah lantai map (X, Z) dalam koordinat dunia")]
    public Vector2 mapMin = new Vector2(-20f, -10f);
    [Tooltip("Pojok kanan-atas lantai map (X, Z) dalam koordinat dunia")]
    public Vector2 mapMax = new Vector2(20f, 10f);
    [Tooltip("Tinggi lantai map (Y)")]
    public float groundY = 0f;
    [Header("Margin Per Sisi")]
    [Tooltip("Positif = kamera berhenti lebih awal (luar map makin aman). Negatif = kamera boleh melewati batas map, berguna agar pemain di pojok tidak terpotong tepi layar / UI.")]
    public float marginLeft = 0.5f;
    public float marginRight = 0.5f;
    [Tooltip("Sisi atas layar (bagian belakang map)")]
    public float marginTop = 0.5f;
    [Tooltip("Sisi bawah layar. Isi negatif (misal -3) jika bawah layar tertutup UI skor")]
    public float marginBottom = 0.5f;

    // --- VARIABEL TRIGGER SHAKE ---
    private float shakeTimeRemaining;
    private float shakePower;
    private float shakeFadeTime;

    private Vector3 currentSmoothPosition;
    private Vector3 velocity;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        currentSmoothPosition = transform.position;
    }

    void LateUpdate()
    {
        if (targets == null || targets.Count == 0) return;

        Move();
        Zoom();
    }

    void Move()
    {
        Vector3 centerPoint = GetCenterPoint();
        Vector3 targetPosition = centerPoint + offset;

        // Batasi posisi kamera agar area luar map tidak terlihat
        if (useMapBounds) targetPosition = ClampToMap(targetPosition);

        // 1. Hitung pergerakan mulus kamera dasar
        currentSmoothPosition = Vector3.SmoothDamp(currentSmoothPosition, targetPosition, ref velocity, smoothTime);

        Vector3 shakeOffset = Vector3.zero;

        // 2. GETARAN HORIZONTAL TERUS MENERUS (Kanan-Kiri Saja)
        if (enableContinuousShake)
        {
            float noiseX = (Mathf.PerlinNoise(Time.time * continuousShakeSpeed, 0f) * 2f) - 1f;
            shakeOffset += new Vector3(noiseX, 0f, 0f) * continuousShakePower;
        }

        // 3. GETARAN KEJUTAN (Jika dipanggil TriggerShake saat tabrakan)
        if (shakeTimeRemaining > 0)
        {
            shakeTimeRemaining -= Time.deltaTime;

            float xAmount = Random.Range(-1f, 1f) * shakePower;
            float yAmount = Random.Range(-1f, 1f) * shakePower;

            shakeOffset += new Vector3(xAmount, yAmount, 0f);

            shakePower = Mathf.MoveTowards(shakePower, 0f, shakeFadeTime * Time.deltaTime);
        }

        // 4. Gabungkan posisi kamera dasar dengan efek getaran
        transform.position = currentSmoothPosition + shakeOffset;
    }

    void Zoom()
    {
        float greatestDistance = GetGreatestDistance();

        float activeDistance = Mathf.Max(0, greatestDistance - minDistanceBeforeZoom);
        float zoomRatio = activeDistance / zoomLimiter;
        float targetFOV = Mathf.Lerp(minZoom, maxZoom, zoomRatio);

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 3f);
    }

    // ------------------------------------------------------------
    //  BATAS MAP
    // ------------------------------------------------------------

    // Tembak ray dari titik viewport ke bidang lantai (dihitung dari posisi 'origin',
    // bukan posisi kamera yang sedang bergetar, supaya hasil clamp stabil).
    bool GroundPoint(float vx, float vy, Vector3 origin, out Vector3 point)
    {
        Vector3 dir = cam.ViewportPointToRay(new Vector3(vx, vy, 0f)).direction;
        point = Vector3.zero;

        // Ray harus mengarah ke bawah agar bisa mengenai lantai
        if (dir.y >= -0.0001f) return false;

        float t = (groundY - origin.y) / dir.y;
        if (t <= 0f) return false;

        point = origin + dir * t;
        return true;
    }

    Vector3 ClampToMap(Vector3 desiredCamPos)
    {
        Vector3 origin = currentSmoothPosition;

        // Titik lantai di tengah layar + 4 sudut layar
        if (!GroundPoint(0.5f, 0.5f, origin, out Vector3 mid) ||
            !GroundPoint(0f, 0f, origin, out Vector3 bl) ||
            !GroundPoint(1f, 0f, origin, out Vector3 br) ||
            !GroundPoint(0f, 1f, origin, out Vector3 tl) ||
            !GroundPoint(1f, 1f, origin, out Vector3 tr))
        {
            return desiredCamPos; // kamera tidak menghadap lantai, lewati clamp
        }

        // Batas terluar area terlihat (ambil yang paling jauh dari 4 sudut)
        float visMinX = Mathf.Min(Mathf.Min(bl.x, br.x), Mathf.Min(tl.x, tr.x));
        float visMaxX = Mathf.Max(Mathf.Max(bl.x, br.x), Mathf.Max(tl.x, tr.x));
        float visMinZ = Mathf.Min(Mathf.Min(bl.z, br.z), Mathf.Min(tl.z, tr.z));
        float visMaxZ = Mathf.Max(Mathf.Max(bl.z, br.z), Mathf.Max(tl.z, tr.z));

        // Seberapa jauh area terlihat melebar dari titik tengah layar
        float leftExt   = mid.x - visMinX + marginLeft;
        float rightExt  = visMaxX - mid.x + marginRight;
        float bottomExt = mid.z - visMinZ + marginBottom;
        float topExt    = visMaxZ - mid.z + marginTop;

        // Selisih antara posisi kamera dan titik lantai yang dilihat di tengah layar
        Vector2 shift = new Vector2(mid.x - origin.x, mid.z - origin.z);

        // Titik tengah layar (di lantai) kalau kamera berada di posisi yang diinginkan
        float viewX = desiredCamPos.x + shift.x;
        float viewZ = desiredCamPos.z + shift.y;

        // Batas aman titik tengah layar
        float minX = mapMin.x + leftExt;
        float maxX = mapMax.x - rightExt;
        float minZ = mapMin.y + bottomExt;
        float maxZ = mapMax.y - topExt;

        // Kalau area terlihat lebih besar dari map, taruh di tengah map
        viewX = (minX > maxX) ? (mapMin.x + mapMax.x) * 0.5f : Mathf.Clamp(viewX, minX, maxX);
        viewZ = (minZ > maxZ) ? (mapMin.y + mapMax.y) * 0.5f : Mathf.Clamp(viewZ, minZ, maxZ);

        // Kembalikan ke posisi kamera
        desiredCamPos.x = viewX - shift.x;
        desiredCamPos.z = viewZ - shift.y;
        return desiredCamPos;
    }

    // Gambar batas map di Scene view (pilih objek kamera untuk melihatnya)
    void OnDrawGizmosSelected()
    {
        if (!useMapBounds) return;

        Gizmos.color = Color.green;
        Vector3 a = new Vector3(mapMin.x, groundY, mapMin.y);
        Vector3 b = new Vector3(mapMax.x, groundY, mapMin.y);
        Vector3 c = new Vector3(mapMax.x, groundY, mapMax.y);
        Vector3 d = new Vector3(mapMin.x, groundY, mapMax.y);
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }

    // ------------------------------------------------------------
    //  UTILITAS
    // ------------------------------------------------------------

    Vector3 GetCenterPoint()
    {
        if (targets.Count == 1) return targets[0].position;

        var bounds = new Bounds(targets[0].position, Vector3.zero);
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != null) bounds.Encapsulate(targets[i].position);
        }

        return bounds.center;
    }

    float GetGreatestDistance()
    {
        if (targets.Count <= 1) return 0f;

        var bounds = new Bounds(targets[0].position, Vector3.zero);
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != null) bounds.Encapsulate(targets[i].position);
        }

        return bounds.size.magnitude;
    }

    public void AddTarget(Transform newTarget)
    {
        if (targets == null) targets = new List<Transform>();
        if (!targets.Contains(newTarget)) targets.Add(newTarget);
    }

    public void RemoveTarget(Transform targetToRemove)
    {
        if (targets == null) return;
        if (targets.Contains(targetToRemove)) targets.Remove(targetToRemove);
    }

    public void TriggerShake(float duration, float magnitude)
    {
        shakeTimeRemaining = duration;
        shakePower = magnitude;
        shakeFadeTime = magnitude / duration;
    }
}