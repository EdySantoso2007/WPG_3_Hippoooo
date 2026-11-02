using UnityEngine;

/// <summary>
/// Padanan cutscene_shot.gd (Godot). Satu shot = satu posisi kamera + trigger animasi.
/// </summary>
[System.Serializable]
public class CutsceneShot
{
    public enum TransitionType { HardCut, SmoothPan }

    // Pengganti target_worker_id di Godot
    public enum WorkerTarget
    {
        LastPlace,   // hippo juara terakhir di Podium (yang dipecat)
        Random,      // acak dari worker yang aktif
        Specific     // pakai specificPlayerIndex
    }

    public string label = "Shot";
    [Tooltip("Marker (Empty GameObject) posisi + rotasi kamera untuk shot ini.")]
    public Transform marker;
    public TransitionType transitionType = TransitionType.HardCut;
    public float transitionDuration = 1.2f;
    [Tooltip("Lama kamera diam di shot ini (detik).")]
    public float holdDuration = 2f;

    [Header("Worker Reaction Trigger")]
    public bool triggerWorkerAnimation = false;
    public float animationDelay = 0f;
    public WorkerTarget workerTarget = WorkerTarget.LastPlace;
    [Tooltip("Dipakai kalau Worker Target = Specific. 0 Merah, 1 Biru, 2 Hijau, 3 Kuning.")]
    [Range(0, 3)] public int specificPlayerIndex = 0;

    [Header("Boss Animation Trigger")]
    public bool triggerBossAnimation = false;
    [Tooltip("Nama Trigger / State di Animator boss, mis. Angry atau Laugh.")]
    public string bossAnimName = "";
    [Tooltip("Jeda setelah kamera tiba sebelum animasi boss dijalankan.")]
    public float bossAnimDelay = 0f;
}