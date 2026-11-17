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
    [Tooltip("Hanya dipakai sebagai ARAH kamera dari pemain. Jaraknya diatur oleh Min/Max Cam Distance.")]
    public Vector3 offset = new Vector3(0, 10, -10);
    public float smoothTime = 0.5f;

    [Header("Pengaturan Zoom (Jarak Kamera)")]
    [Tooltip("FOV tetap selama game berjalan.")]
    public float fixedFOV = 45f;
    [Tooltip("Jarak kamera saat pemain berdekatan (zoom in).")]
    public float minCamDistance = 12f;
    [Tooltip("Jarak kamera saat pemain berjauhan (zoom out). Naikkan angka ini kalau zoom out kurang tinggi.")]
    public float maxCamDistance = 30f;
    [Tooltip("Jarak antar pemain (di lantai) di mana kamera masih paling dekat.")]
    public float minPlayerSpread = 4f;
    [Tooltip("Jarak antar pemain (di lantai) di mana kamera sudah paling jauh.")]
    public float maxPlayerSpread = 25f;
    [Tooltip("Kecepatan zoom normal. Makin besar = makin cepat.")]
    public float zoomSpeed = 3f;
    public enum ZoomMode { PlayerSpread, Zones }

    [Header("Mode Zoom")]
    [Tooltip("PlayerSpread = zoom berdasarkan jarak antar pemain. Zones = zoom berdasarkan zona (CameraZone) tempat pemain berada.")]
    public ZoomMode zoomMode = ZoomMode.Zones;

    [Header("Mode Zones: Jarak Kamera")]
    [Tooltip("Jarak kamera di zona Near.")]
    public float nearCamDistance = 12f;
    [Tooltip("Jarak kamera di zona Far (prioritas lebih tinggi).")]
    public float farCamDistance = 30f;
    [Tooltip("Kalau pemain tidak ada di zona manapun: centang = pakai kamera jauh, hilangkan = pakai kamera dekat.")]
    public bool outsideZonesUseFar = false;

    [Tooltip("Hanya untuk melihat nilai saat Play Mode (membantu mengatur Min/Max Player Spread).")]
    public float debugCurrentSpread;
    public float debugCurrentDistance;

    [Header("Pengaturan Efek Getar Halus (Continuous Shake)")]
    public bool enableContinuousShake = true;
    [Tooltip("Kekuatan getaran. Gunakan angka kecil misal 0.1 atau 0.5")]
    public float continuousShakePower = 0.2f;
    [Tooltip("Seberapa cepat getarannya berayun")]
    public float continuousShakeSpeed = 2f;

    [Header("Batas Map (agar luar map tidak terlihat)")]
    [Tooltip("Kalau zoom out terasa 'tertahan', coba matikan ini untuk tes. Kalau area terlihat lebih besar dari map, kamera dikunci di tengah map.")]
    public bool useMapBounds = true;
    [Tooltip("Pojok kiri-bawah lantai map (X, Z) dalam koordinat dunia")]
    public Vector2 mapMin = new Vector2(-20f, -10f);
    [Tooltip("Pojok kanan-atas lantai map (X, Z) dalam koordinat dunia")]
    public Vector2 mapMax = new Vector2(20f, 10f);
    [Tooltip("Tinggi lantai map (Y)")]
    public float groundY = 0f;
    [Header("Margin Per Sisi")]
    public float marginLeft = 0.5f;
    public float marginRight = 0.5f;
    public float marginTop = 0.5f;
    [Tooltip("Isi negatif (misal -3) jika bawah layar tertutup UI skor")]
    public float marginBottom = 0.5f;

    // ------------------------------------------------------------
    //  INTRO: HITUNGAN SEBELUM ZOOM KE PEMAIN
    // ------------------------------------------------------------
    [Header("Intro Hitungan (sebelum zoom ke pemain)")]
    public bool useIntro = true;
    [Tooltip("Titik yang dilihat kamera selama hitungan. Kosongkan = tengah map.")]
    public Transform introFocus;
    [Tooltip("Field of View selama hitungan.")]
    public float introFOV = 60f;
    [Tooltip("Jarak kamera dari titik fokus selama hitungan. Makin besar = makin luas.")]
    public float introDistance = 28f;
    public int countdownFrom = 3;
    [Tooltip("Centang: 3,2,1. Hilangkan centang: 1,2,3.")]
    public bool countDownwards = true;
    public float secondsPerCount = 1f;
    public string goText = "GO!";
    public float goDuration = 0.8f;
    public float goStartScale = 2.5f;
    public TextMeshProUGUI countdownText;
    public bool freezeGameDuringCountdown = true;

    [Header("Transisi Zoom ke Pemain (setelah hitungan)")]
    public float transitionSmoothTime = 1.2f;
    public float transitionZoomSpeed = 2f;
    public float transitionDuration = 1.5f;

    public UnityEvent onCountdownFinished;

    public bool IsIntroPlaying { get; private set; }

    /// <summary>
    /// True selama hitungan sampai teks GO! selesai. Dipakai script pemain untuk mengunci input.
    /// </summary>
    public static bool InputLocked { get; private set; }

    // --- VARIABEL SHAKE ---
    private float shakeTimeRemaining;
    private float shakePower;
    private float shakeFadeTime;

    private Vector3 currentSmoothPosition;
    private Vector3 velocity;
    private Camera cam;
    private float currentCamDistance;

    // --- VARIABEL INTRO ---
    private Vector3 introPosition;
    private float transitionTimer;
    private bool timeFrozenByIntro;
    private float savedTimeScale = 1f;
    private Coroutine introRoutine;

    private const float MaxFrameTime = 0.05f;
    private const int WarmupFrames = 5;

    void Awake()
    {
        InputLocked = useIntro;
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        currentSmoothPosition = transform.position;
        currentCamDistance = minCamDistance;

        if (useIntro)
        {
            PlayIntro();
        }
        else
        {
            // Langsung mulai dengan FOV normal
            cam.fieldOfView = fixedFOV;
        }
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
        if (IsIntroPlaying)
        {
            HoldIntroView();
            return;
        }

        if (targets == null || targets.Count == 0) return;

        // Urutan penting: hitung zoom dulu, baru gerak
        Zoom();
        Move();

        if (transitionTimer > 0f) transitionTimer -= Time.deltaTime;
    }

    // ------------------------------------------------------------
    //  INTRO
    // ------------------------------------------------------------

    public void PlayIntro()
    {
        if (cam == null) cam = GetComponent<Camera>();
        InputLocked = true;

        if (introRoutine != null) StopCoroutine(introRoutine);

        Vector3 focus = introFocus != null
            ? introFocus.position
            : new Vector3((mapMin.x + mapMax.x) * 0.5f, groundY, (mapMin.y + mapMax.y) * 0.5f);

        introPosition = focus + offset.normalized * introDistance;
        introRoutine = StartCoroutine(IntroRoutine());
    }

    void HoldIntroView()
    {
        transform.position = introPosition;
        currentSmoothPosition = introPosition;
        velocity = Vector3.zero;
        cam.fieldOfView = introFOV;

        // Supaya setelah intro, zoom mulai dari jarak intro lalu mendekat dengan mulus
        currentCamDistance = introDistance;
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

        for (int f = 0; f < WarmupFrames; f++)
            yield return null;

        for (int i = 0; i < countdownFrom; i++)
        {
            int number = countDownwards ? countdownFrom - i : i + 1;
            yield return ShowText(number.ToString(), secondsPerCount);
        }

        if (!string.IsNullOrEmpty(goText))
            yield return ShowText(goText, goDuration, goStartScale);

        if (countdownText != null) countdownText.gameObject.SetActive(false);

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

    void Zoom()
    {
        float spread = GetGreatestDistance();
        float targetDist;

        if (zoomMode == ZoomMode.Zones)
        {
            targetDist = GetZoneTargetDistance();
        }
        else
        {
            float t = Mathf.InverseLerp(minPlayerSpread, maxPlayerSpread, spread);
            targetDist = Mathf.Lerp(minCamDistance, maxCamDistance, t);
        }

        float speed = transitionTimer > 0f ? transitionZoomSpeed : zoomSpeed;
        float lerpT = 1f - Mathf.Exp(-speed * Time.deltaTime); // smooth & tidak tergantung framerate

        currentCamDistance = Mathf.Lerp(currentCamDistance, targetDist, lerpT);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fixedFOV, lerpT);

        debugCurrentSpread = spread;
        debugCurrentDistance = currentCamDistance;
    }

    void Move()
    {
        Vector3 centerPoint = GetCenterPoint();

        // Kamera berada di belakang titik tengah, sejauh currentCamDistance
        Vector3 targetPosition = centerPoint + offset.normalized * currentCamDistance;

        if (useMapBounds) targetPosition = ClampToMap(targetPosition);

        float activeSmoothTime = transitionTimer > 0f ? transitionSmoothTime : smoothTime;

        currentSmoothPosition = Vector3.SmoothDamp(currentSmoothPosition, targetPosition, ref velocity, activeSmoothTime);

        Vector3 shakeOffset = Vector3.zero;

        // Getaran horizontal terus menerus
        if (enableContinuousShake)
        {
            float noiseX = (Mathf.PerlinNoise(Time.time * continuousShakeSpeed, 0f) * 2f) - 1f;
            shakeOffset += new Vector3(noiseX, 0f, 0f) * continuousShakePower;
        }

        // Getaran kejutan (TriggerShake)
        if (shakeTimeRemaining > 0)
        {
            shakeTimeRemaining -= Time.deltaTime;

            float xAmount = Random.Range(-1f, 1f) * shakePower;
            float yAmount = Random.Range(-1f, 1f) * shakePower;

            shakeOffset += new Vector3(xAmount, yAmount, 0f);

            shakePower = Mathf.MoveTowards(shakePower, 0f, shakeFadeTime * Time.deltaTime);
        }

        transform.position = currentSmoothPosition + shakeOffset;
    }

    // ------------------------------------------------------------
    //  BATAS MAP
    // ------------------------------------------------------------

    bool GroundPoint(float vx, float vy, Vector3 origin, out Vector3 point)
    {
        Vector3 dir = cam.ViewportPointToRay(new Vector3(vx, vy, 0f)).direction;
        point = Vector3.zero;

        if (dir.y >= -0.0001f) return false;

        float t = (groundY - origin.y) / dir.y;
        if (t <= 0f) return false;

        point = origin + dir * t;
        return true;
    }

    Vector3 ClampToMap(Vector3 desiredCamPos)
    {
        Vector3 origin = currentSmoothPosition;

        if (!GroundPoint(0.5f, 0.5f, origin, out Vector3 mid) ||
            !GroundPoint(0f, 0f, origin, out Vector3 bl) ||
            !GroundPoint(1f, 0f, origin, out Vector3 br) ||
            !GroundPoint(0f, 1f, origin, out Vector3 tl) ||
            !GroundPoint(1f, 1f, origin, out Vector3 tr))
        {
            return desiredCamPos;
        }

        float visMinX = Mathf.Min(Mathf.Min(bl.x, br.x), Mathf.Min(tl.x, tr.x));
        float visMaxX = Mathf.Max(Mathf.Max(bl.x, br.x), Mathf.Max(tl.x, tr.x));
        float visMinZ = Mathf.Min(Mathf.Min(bl.z, br.z), Mathf.Min(tl.z, tr.z));
        float visMaxZ = Mathf.Max(Mathf.Max(bl.z, br.z), Mathf.Max(tl.z, tr.z));

        float leftExt   = mid.x - visMinX + marginLeft;
        float rightExt  = visMaxX - mid.x + marginRight;
        float bottomExt = mid.z - visMinZ + marginBottom;
        float topExt    = visMaxZ - mid.z + marginTop;

        Vector2 shift = new Vector2(mid.x - origin.x, mid.z - origin.z);

        float viewX = desiredCamPos.x + shift.x;
        float viewZ = desiredCamPos.z + shift.y;

        float minX = mapMin.x + leftExt;
        float maxX = mapMax.x - rightExt;
        float minZ = mapMin.y + bottomExt;
        float maxZ = mapMax.y - topExt;

        viewX = (minX > maxX) ? (mapMin.x + mapMax.x) * 0.5f : Mathf.Clamp(viewX, minX, maxX);
        viewZ = (minZ > maxZ) ? (mapMin.y + mapMax.y) * 0.5f : Mathf.Clamp(viewZ, minZ, maxZ);

        desiredCamPos.x = viewX - shift.x;
        desiredCamPos.z = viewZ - shift.y;
        return desiredCamPos;
    }

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

    bool TryGetBounds(out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null) continue;

            if (!found)
            {
                bounds = new Bounds(targets[i].position, Vector3.zero);
                found = true;
            }
            else
            {
                bounds.Encapsulate(targets[i].position);
            }
        }
        return found;
    }

    Vector3 GetCenterPoint()
    {
        return TryGetBounds(out Bounds b) ? b.center : transform.position;
    }

    // Tentukan jarak kamera dari zona tempat pemain berada.
    // Aturan: kalau ada SATU saja pemain di zona Far -> pakai Far (prioritas).
    //         Kalau tidak ada, tapi ada pemain di zona Near -> pakai Near.
    //         Kalau tidak ada di zona manapun -> pakai pilihan outsideZonesUseFar.
    float GetZoneTargetDistance()
    {
        bool anyFar = false, anyNear = false;
        float farDist = farCamDistance;
        float nearDist = nearCamDistance;

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null) continue;
            Vector3 p = targets[i].position;

            for (int z = 0; z < CameraZone.All.Count; z++)
            {
                CameraZone zone = CameraZone.All[z];
                if (zone == null || !zone.Contains(p)) continue;

                if (zone.type == CameraZone.ZoneType.Far)
                {
                    // Kalau beberapa zona Far aktif, ambil yang paling jauh
                    float d = zone.customDistance > 0f ? zone.customDistance : farCamDistance;
                    farDist = anyFar ? Mathf.Max(farDist, d) : d;
                    anyFar = true;
                }
                else
                {
                    float d = zone.customDistance > 0f ? zone.customDistance : nearCamDistance;
                    nearDist = anyNear ? Mathf.Max(nearDist, d) : d;
                    anyNear = true;
                }
            }
        }

        if (anyFar) return farDist;
        if (anyNear) return nearDist;
        return outsideZonesUseFar ? farCamDistance : nearCamDistance;
    }

    // Jarak terjauh antar pemain, hanya di lantai (X dan Z), tinggi (Y) diabaikan
    float GetGreatestDistance()
    {
        if (!TryGetBounds(out Bounds b)) return 0f;
        return new Vector2(b.size.x, b.size.z).magnitude;
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