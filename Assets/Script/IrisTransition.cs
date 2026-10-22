using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Transisi lingkaran (iris wipe).
// Close = Fade Out (lingkaran mengecil sampai layar hitam)
// Open  = Fade In  (lingkaran membesar sampai layar terbuka)
public class IrisTransition : MonoBehaviour
{
    [Header("Referensi")]
    [Tooltip("Raw Image layar penuh (warna hitam) yang materialnya memakai shader UI/IrisWipe.")]
    public RawImage image;

    [Header("Pengaturan")]
    [Tooltip("Centang: scene dimulai dalam keadaan hitam lalu otomatis Fade In.")]
    public bool openOnStart = true;
    public float openDuration = 0.8f;

    [Tooltip("Pusat lingkaran (0..1). (0.5, 0.5) = tengah layar.")]
    public Vector2 viewportCenter = new Vector2(0.5f, 0.5f);
    [Tooltip("(Opsional) Kalau diisi, lingkaran memusat ke objek ini, misalnya pemain atau juara 1.")]
    public Transform focusTarget;
    [Tooltip("Kamera untuk menghitung posisi Focus Target. Kosong = Camera.main.")]
    public Camera focusCamera;

    public AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Range(0.0005f, 0.05f)] public float softness = 0.003f;

    private static readonly int CenterId = Shader.PropertyToID("_Center");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int AspectId = Shader.PropertyToID("_Aspect");
    private static readonly int SoftnessId = Shader.PropertyToID("_Softness");

    // Frame yang lag (misalnya saat scene baru selesai dimuat) dihitung maksimal segini
    private const float MaxFrameTime = 0.05f;

    private Material mat;

    void Awake()
    {
        if (image == null)
        {
            Debug.LogError("[IrisTransition] Field Image belum diisi!");
            return;
        }

        // Salinan material, supaya asset material asli tidak ikut berubah
        mat = new Material(image.material);
        image.material = mat;
        image.raycastTarget = false;

        // Mulai dalam keadaan tertutup (hitam), jadi tidak ada kedipan scene sebelum transisi
        Apply(0f);
    }

    void Start()
    {
        if (openOnStart)
            StartCoroutine(Open(openDuration));
        else
            Apply(1f);
    }

    void OnDestroy()
    {
        if (mat != null) Destroy(mat);
    }

    /// <summary>Fade Out: lingkaran mengecil sampai layar hitam.</summary>
    public IEnumerator Close(float duration)
    {
        yield return Animate(1f, 0f, duration);
    }

    /// <summary>Fade In: lingkaran membesar sampai layar terbuka.</summary>
    public IEnumerator Open(float duration)
    {
        yield return Animate(0f, 1f, duration);
    }

    private IEnumerator Animate(float from, float to, float duration)
    {
        if (image == null || mat == null) yield break;

        // Pakai waktu nyata (unscaled) karena game sering di-freeze (Time.timeScale = 0) saat transisi
        float t = 0f;
        while (t < duration)
        {
            t += Mathf.Min(Time.unscaledDeltaTime, MaxFrameTime);
            float k = ease.Evaluate(Mathf.Clamp01(t / duration));
            Apply(Mathf.Lerp(from, to, k));
            yield return null;
        }
        Apply(to);
    }

    // progress: 0 = tertutup (hitam penuh), 1 = terbuka penuh
    private void Apply(float progress)
    {
        if (image == null || mat == null) return;

        Vector2 c = GetCenter();
        float aspect = (float)Screen.width / Screen.height;

        // Radius yang cukup untuk menutupi sudut layar terjauh dari pusat (tinggi layar = 1)
        float dx = Mathf.Max(c.x, 1f - c.x) * aspect;
        float dy = Mathf.Max(c.y, 1f - c.y);
        float maxRadius = Mathf.Sqrt(dx * dx + dy * dy) + softness;

        mat.SetVector(CenterId, new Vector4(c.x, c.y, 0f, 0f));
        mat.SetFloat(AspectId, aspect);
        mat.SetFloat(SoftnessId, softness);
        mat.SetFloat(RadiusId, Mathf.Lerp(0f, maxRadius, progress));

        // Kalau sudah terbuka penuh, matikan gambarnya (hemat render)
        image.enabled = progress < 1f;
    }

    private Vector2 GetCenter()
    {
        if (focusTarget != null)
        {
            Camera cam = focusCamera != null ? focusCamera : Camera.main;
            if (cam != null)
            {
                Vector3 v = cam.WorldToViewportPoint(focusTarget.position);
                if (v.z > 0f) return new Vector2(v.x, v.y);
            }
        }
        return viewportCenter;
    }
}