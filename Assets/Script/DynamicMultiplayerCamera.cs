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

        // 1. Hitung pergerakan mulus kamera dasar
        currentSmoothPosition = Vector3.SmoothDamp(currentSmoothPosition, targetPosition, ref velocity, smoothTime);

        Vector3 shakeOffset = Vector3.zero;

        // 2. GETARAN HORIZONTAL TERUS MENERUS (Kanan-Kiri Saja)
        if (enableContinuousShake)
        {
            // Hanya menghitung noise untuk sumbu X
            float noiseX = (Mathf.PerlinNoise(Time.time * continuousShakeSpeed, 0f) * 2f) - 1f;

            // Sumbu Y dan Z dikunci ke 0f
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