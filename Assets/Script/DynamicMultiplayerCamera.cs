using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

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

    // ------------------------------------------------------------
    //  INTRO: HITUNGAN SEBELUM ZOOM KE PEMAIN
    // ------------------------------------------------------------
    [Header("Intro Hitungan (sebelum zoom ke pemain)")]
    public bool useIntro = true;
    [Tooltip("Titik yang dilihat kamera selama hitungan. Kosongkan = tengah map (dari Map Min / Map Max).")]
    public Transform introFocus;
    [Tooltip("Field of View selama hitungan. Makin besar = makin jauh / luas.")]
    public float introFOV = 70f;
    [Tooltip("Angka awal hitungan (3 = 3,2,1 atau 1,2,3 tergantung opsi di bawah)")]
    public int countdownFrom = 3;
    [Tooltip("Centang: 3,2,1. Hilangkan centang: 1,2,3.")]
    public bool countDownwards = true;
    public float secondsPerCount = 1f;
    [Tooltip("Teks setelah hitungan selesai. Kosongkan kalau tidak mau.")]
    public string goText = "GO!";
    [Tooltip("Lama teks GO! tampil. Selama ini kamera MASIH diam, baru bergerak ke pemain setelahnya.")]
    public float goDuration = 0.8f;
    [Tooltip("Ukuran awal teks GO! (efek menghantam: besar lalu mengecil ke ukuran normal).")]
    public float goStartScale = 2.5f;
    [Tooltip("Teks TextMeshPro (di Canvas) untuk menampilkan angka hitungan. Boleh kosong.")]
    public TextMeshProUGUI countdownText;
    [Tooltip("Centang: game dijeda (Time.timeScale = 0) selama hitungan, lalu jalan lagi setelah selesai.")]
    public bool freezeGameDuringCountdown = true;

    [Header("Transisi Zoom ke Pemain (setelah hitungan)")]
    [Tooltip("Makin besar = kamera bergerak ke pemain makin pelan.")]
    public float transitionSmoothTime = 1.2f;
    [Tooltip("Kecepatan zoom FOV selama transisi (normal = 3).")]
    public float transitionZoomSpeed = 2f;
    [Tooltip("Berapa detik pengaturan transisi di atas dipakai sebelum kembali normal.")]
    public float transitionDuration = 1.5f;

    [Tooltip("Dipanggil tepat setelah hitungan selesai (misalnya untuk memulai timer game).")]
    public UnityEvent onCountdownFinished;

    // Script lain bisa mengecek ini (misalnya agar pemain tidak bisa gerak saat hitungan)
    public bool IsIntroPlaying { get; private set; }

    /// <summary>
    /// True selama hitungan sampai teks GO! selesai. Dipakai PlayerMovement / PlayerInteraction
    /// untuk mengunci pemain. Tidak bergantung pada Time.timeScale, jadi tetap aman walau script lain mengubahnya.
    /// </summary>
    public static bool InputLocked { get; private set; }

    // --- VARIABEL TRIGGER SHAKE ---
    private float shakeTimeRemaining;
    private float shakePower;
    private float shakeFadeTime;

    private Vector3 currentSmoothPosition;
    private Vector3 velocity;
    private Camera cam;

    // --- VARIABEL INTRO ---
    private Vector3 introPosition;
    private float transitionTimer;
    private bool timeFrozenByIntro;
    private float savedTimeScale = 1f;
    private Coroutine introRoutine;

    // Frame yang lag (misalnya saat scene baru selesai loading) dihitung maksimal segini,
    // supaya satu frame berat tidak melompati angka hitungan.
    private const float MaxFrameTime = 0.05f;
    private const int WarmupFrames = 5;

    void Awake()
    {
        // Kunci sedini mungkin, sebelum pemain sempat bergerak
        InputLocked = useIntro;
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        currentSmoothPosition = transform.position;

        if (useIntro) PlayIntro();
    }

    void OnDisable()
    {
        RestoreTimeScale();
        InputLocked = false;
    }

    void OnDestroy()
    {
        RestoreTimeScale();
        InputLocked = false;
    }

    void LateUpdate()
    {
        // Selama hitungan: kamera diam di sudut pandang awal (jauh / luas)
        if (IsIntroPlaying)
        {
            HoldIntroView();
            return;
        }

        if (targets == null || targets.Count == 0) return;

        Move();
        Zoom();

        if (transitionTimer > 0f) transitionTimer -= Time.deltaTime;
    }

    // ------------------------------------------------------------
    //  INTRO
    // ------------------------------------------------------------

    // Bisa dipanggil lagi dari script lain untuk mengulang hitungan (misalnya saat ronde baru)
    public void PlayIntro()
    {
        if (cam == null) cam = GetComponent<Camera>();
        InputLocked = true;

        if (introRoutine != null) StopCoroutine(introRoutine);

        Vector3 focus = introFocus != null
            ? introFocus.position
            : new Vector3((mapMin.x + mapMax.x) * 0.5f, groundY, (mapMin.y + mapMax.y) * 0.5f);

        introPosition = focus + offset;
        introRoutine = StartCoroutine(IntroRoutine());
    }

    void HoldIntroView()
    {
        transform.position = introPosition;
        currentSmoothPosition = introPosition;
        velocity = Vector3.zero;
        cam.fieldOfView = introFOV;
    }

    IEnumerator IntroRoutine()
    {
        IsIntroPlaying = true;
        HoldIntroView();

        if (freezeGameDuringCountdown && !timeFrozenByIntro)
        {
            savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            timeFrozenByIntro = true;
        }

        // Beri waktu beberapa frame setelah scene selesai dimuat (frame awal biasanya berat / lag)
        for (int f = 0; f < WarmupFrames; f++)
            yield return null;

        // Hitungan angka (pakai waktu real, supaya tetap jalan walau Time.timeScale = 0)
        for (int i = 0; i < countdownFrom; i++)
        {
            int number = countDownwards ? countdownFrom - i : i + 1;
            yield return ShowText(number.ToString(), secondsPerCount);
        }

        // Teks "GO!" tampil saat kamera MASIH diam di sudut pandang awal
        if (!string.IsNullOrEmpty(goText))
            yield return ShowText(goText, goDuration, goStartScale);

        if (countdownText != null) countdownText.gameObject.SetActive(false);

        // Setelah GO!: game jalan lagi, pemain boleh bergerak, kamera mulai bergerak / zoom ke pemain
        RestoreTimeScale();
        IsIntroPlaying = false;
        InputLocked = false;
        transitionTimer = transitionDuration;
        onCountdownFinished?.Invoke();
        introRoutine = null;
    }

    IEnumerator ShowText(string message, float duration, float startScale = 1.6f)
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = message;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Mathf.Min(Time.unscaledDeltaTime, MaxFrameTime);

            // Efek "pop": angka muncul besar lalu mengecil
            if (countdownText != null)
            {
                float pop = Mathf.Clamp01(t / 0.25f);
                countdownText.rectTransform.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, pop);
            }
            yield return null;
        }
    }

    void RestoreTimeScale()
    {
        if (!timeFrozenByIntro) return;
        Time.timeScale = savedTimeScale;
        timeFrozenByIntro = false;
    }

    // ------------------------------------------------------------
    //  GERAK & ZOOM
    // ------------------------------------------------------------

    void Move()
    {
        Vector3 centerPoint = GetCenterPoint();
        Vector3 targetPosition = centerPoint + offset;

        // Batasi posisi kamera agar area luar map tidak terlihat
        if (useMapBounds) targetPosition = ClampToMap(targetPosition);

        // Saat transisi setelah hitungan, kamera bergerak lebih pelan
        float activeSmoothTime = transitionTimer > 0f ? transitionSmoothTime : smoothTime;

        // 1. Hitung pergerakan mulus kamera dasar
        currentSmoothPosition = Vector3.SmoothDamp(currentSmoothPosition, targetPosition, ref velocity, activeSmoothTime);

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

        // Saat transisi setelah hitungan, zoom lebih pelan
        float zoomSpeed = transitionTimer > 0f ? transitionZoomSpeed : 3f;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
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