using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Port dari script cutscene di Godot (Cutscene.tscn / cutscene_shot.gd / worker_hippo.gd).
/// Menjalankan urutan shot kamera, animasi boss, dan reaksi worker.
/// target_worker_id di Godot -> di sini WorkerTarget.LastPlace = hippo juara terakhir di Podium (ScoreBridge.Results, elemen terakhir).
///
/// LOOP: setelah cutscene, hippo yang dipecat ditandai eliminated di RoomBridge.
/// Kalau pemain yang tersisa >= 2 -> kembali ke minigame. Kalau tersisa 1 -> loop selesai di sini.
/// Perpindahan scene memakai transisi iris (lingkaran menutup) kalau field Iris diisi.
/// </summary>
public class FiredSceneController : MonoBehaviour
{
    [Header("Cutscene Sequence")]
    public bool autoplayOnStart = true;
    public List<CutsceneShot> sequence = new List<CutsceneShot>();

    [Header("Workers")]
    [Tooltip("Isi 4 WorkerHippo (index 0-3). Yang dipakai hanya yang sesuai pemain yang ikut main.")]
    public List<WorkerHippo> workers = new List<WorkerHippo>();
    [Tooltip("Nama Trigger / State reaksi di Animator worker (Godot: Armature_001|WorkerFall).")]
    public string reactionAnimName = "Fall";
    [Tooltip("Urutan: [0] Merah, [1] Biru, [2] Hijau, [3] Kuning (sama dengan PlayerManager).")]
    public Texture2D[] playerTextures;
    [Tooltip("Dipakai kalau scene ini dijalankan langsung (ScoreBridge kosong / HasResults false). -1 = tidak ada.")]
    public int debugLoserIndex = 3;

    [Header("Boss")]
    public Animator bossAnimator;

    [Header("Camera")]
    public Camera cam;
    [Tooltip("Posisi awal kamera (Godot: default_marker / ShotWide).")]
    public Transform defaultMarker;

    [Header("Loop Minigame")]
    [Tooltip("Scene minigame yang dimuat lagi selama pemain yang tersisa masih 2 atau lebih.")]
    public string minigameSceneName = "MainScenery";
    [Tooltip("Minimal pemain tersisa agar loop lanjut. Kalau pemain tersisa kurang dari ini, loop selesai.")]
    public int minPlayersToContinue = 2;

    [Header("Setelah Loop Selesai (tersisa 1 pemain)")]
    [Tooltip("Scene yang dimuat setelah loop selesai (mis. MainMenu). Kosong = tetap di scene ini.")]
    public string nextSceneName = "";
    public float delayBeforeNextScene = 1f;
    [Tooltip("Kalau pemain hanya 1, lewati cutscene dan langsung ke Fallback Scene Name.")]
    public bool skipIfSinglePlayer = true;
    public string fallbackSceneName = "MainMenu";

    [Header("Transisi Iris (Dipecat -> Minigame / Scene Berikutnya)")]
    [Tooltip("Drag objek IrisTransition dari scene ini. Kosong = langsung pindah scene tanpa transisi.")]
    public IrisTransition iris;
    [Tooltip("Lama lingkaran menutup sebelum scene berikutnya dimuat (detik).")]
    public float irisCloseDuration = 0.8f;

    [Header("Testing")]
    [Tooltip("Tekan Space untuk mengulang cutscene (seperti di Godot).")]
    public bool replayWithSpace = true;

    private bool isPlaying;
    private int loserIndex = -1;

    // Isi default sesuai Cutscene.tscn di Godot (marker tetap harus diisi manual)
    void Reset()
    {
        cam = Camera.main;
        sequence = new List<CutsceneShot>
        {
            new CutsceneShot { label = "ShotWide", transitionType = CutsceneShot.TransitionType.HardCut,
                transitionDuration = 0f, holdDuration = 0f,
                triggerBossAnimation = true, bossAnimName = "Angry", bossAnimDelay = 0.515f },

            new CutsceneShot { label = "Shot2", holdDuration = 0.14f },

            new CutsceneShot { label = "ShotCloseup", transitionType = CutsceneShot.TransitionType.SmoothPan,
                transitionDuration = 1.2f, holdDuration = 1f },

            new CutsceneShot { label = "ShotShoulder", holdDuration = 3.005f,
                triggerWorkerAnimation = true, animationDelay = 0.5f,
                workerTarget = CutsceneShot.WorkerTarget.LastPlace },

            new CutsceneShot { label = "ShotShoulder2", holdDuration = 2f,
                triggerBossAnimation = true, bossAnimName = "Laugh" },
        };
    }

    void Start()
    {
        Time.timeScale = 1f; // jaga-jaga kalau datang dari scene yang ter-freeze

        if (cam == null) cam = Camera.main;

        // Hippo yang dipecat = juara terakhir di Podium.
        // Podium menaruh Results[i] di Slots[i], jadi elemen terakhir Results = posisi podium terakhir.
        if (ScoreBridge.HasResults)
        {
            List<PlayerResult> results = ScoreBridge.Results;

            // Pemain tunggal: tidak ada yang bisa dipecat
            if (skipIfSinglePlayer && results.Count < 2 && !string.IsNullOrEmpty(fallbackSceneName))
            {
                SceneManager.LoadScene(fallbackSceneName);
                return;
            }

            loserIndex = results[results.Count - 1].playerIndex;
        }
        else
        {
            loserIndex = debugLoserIndex;
        }

        SetupWorkers();

        if (defaultMarker != null) SnapCameraTo(defaultMarker);

        if (autoplayOnStart && sequence.Count > 0) StartCutscene();
    }

    void Update()
    {
        if (!replayWithSpace || isPlaying) return;
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            StartCutscene();
    }

    // ---------- Setup ----------

    void SetupWorkers()
    {
        foreach (WorkerHippo w in workers)
        {
            if (w == null) continue;

            // Warna sesuai pemain
            if (playerTextures != null && w.playerIndex < playerTextures.Length)
                w.ApplyTexture(playerTextures[w.playerIndex]);

            // Tampilkan semua hippo pemain yang ikut main di ronde ini.
            // Yang tidak dipecat tidak dapat trigger, jadi diam di state default (FallHold).
            w.gameObject.SetActive(IsPlaying(w.playerIndex));
        }
    }

    // Pemain ini ikut main? (ada di hasil Podium). Kalau tidak ada data (testing), semua tampil.
    bool IsPlaying(int playerIndex)
    {
        if (!ScoreBridge.HasResults) return true;

        foreach (PlayerResult r in ScoreBridge.Results)
            if (r.playerIndex == playerIndex) return true;

        return false;
    }

    // ---------- Sequence ----------

    public void StartCutscene()
    {
        if (isPlaying) return;
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        isPlaying = true;
        Debug.Log("Starting automated cutscene sequence...");

        for (int i = 0; i < sequence.Count; i++)
        {
            CutsceneShot shot = sequence[i];
            if (shot == null || shot.marker == null) continue;

            Debug.Log($"Playing shot {i}: {shot.marker.name}");

            // 1. Transisi kamera (hard cut atau smooth pan)
            if (shot.transitionType == CutsceneShot.TransitionType.HardCut || shot.transitionDuration <= 0f)
                SnapCameraTo(shot.marker);
            else
                yield return PanCameraTo(shot.marker, shot.transitionDuration);

            // 2. Animasi boss
            if (shot.triggerBossAnimation && bossAnimator != null)
            {
                if (shot.bossAnimDelay > 0f) yield return new WaitForSeconds(shot.bossAnimDelay);

                if (AnimatorUtil.Fire(bossAnimator, shot.bossAnimName))
                    Debug.Log("Boss playing animation: " + shot.bossAnimName);
                else
                    Debug.LogWarning("Boss animation not found: " + shot.bossAnimName);
            }

            // 3. Reaksi worker
            if (shot.triggerWorkerAnimation && workers.Count > 0)
            {
                if (shot.animationDelay > 0f) yield return new WaitForSeconds(shot.animationDelay);

                WorkerHippo chosen = ChooseWorker(shot);
                if (chosen != null)
                    chosen.PlayReaction(reactionAnimName);
                else
                    Debug.LogWarning("Worker target tidak ditemukan (cek playerIndex di WorkerHippo).");
            }

            // 4. Tahan shot
            if (shot.holdDuration > 0f) yield return new WaitForSeconds(shot.holdDuration);
        }

        Debug.Log("Cutscene sequence finished!");

        // Putuskan scene berikutnya: lanjut minigame, atau loop selesai
        string target = DecideNextScene();

        if (!string.IsNullOrEmpty(target))
        {
            yield return new WaitForSeconds(delayBeforeNextScene);

            // Iris menutup (layar jadi hitam) -> baru pindah scene
            if (iris != null)
                yield return iris.Close(irisCloseDuration);

            SceneManager.LoadScene(target);
            yield break; // scene sedang berpindah, isPlaying sengaja tidak di-reset (cegah replay Space)
        }

        isPlaying = false;
    }

    // Tandai pemain yang dipecat, lalu tentukan scene berikutnya.
    // Return "" kalau tidak ada scene yang perlu dimuat (tetap di scene ini).
    string DecideNextScene()
    {
        // Scene dijalankan langsung (testing, tanpa lewat Room/minigame): perilaku lama
        bool inLoop = ScoreBridge.HasResults && RoomBridge.JoinedPlayers.Count > 0;
        if (!inLoop) return nextSceneName;

        RoomBridge.Eliminate(loserIndex);
        int remaining = RoomBridge.ActiveCount;
        Debug.Log($"Pemain {loserIndex} dipecat. Sisa pemain: {remaining}");

        // Masih ada cukup pemain -> main lagi tanpa yang dipecat
        if (remaining >= minPlayersToContinue)
        {
            if (string.IsNullOrEmpty(minigameSceneName))
            {
                Debug.LogWarning("[FiredScene] Minigame Scene Name belum diisi di Inspector!");
                return "";
            }
            return minigameSceneName;
        }

        // Tersisa 1 pemain -> loop selesai di cutscene ini
        Debug.Log("Loop selesai: tersisa " + remaining + " pemain.");
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            RoomBridge.ResetRoom();
            ScoreBridge.Clear();
        }
        return nextSceneName;
    }

    WorkerHippo ChooseWorker(CutsceneShot shot)
    {
        switch (shot.workerTarget)
        {
            case CutsceneShot.WorkerTarget.LastPlace:
                return FindWorker(loserIndex);

            case CutsceneShot.WorkerTarget.Specific:
                return FindWorker(shot.specificPlayerIndex);

            default: // Random
                List<WorkerHippo> active = workers.FindAll(w => w != null && w.gameObject.activeInHierarchy);
                return active.Count > 0 ? active[Random.Range(0, active.Count)] : null;
        }
    }

    WorkerHippo FindWorker(int playerIndex)
    {
        foreach (WorkerHippo w in workers)
            if (w != null && w.playerIndex == playerIndex) return w;
        return null;
    }

    // ---------- Kamera ----------

    void SnapCameraTo(Transform marker)
    {
        if (cam == null || marker == null) return;
        cam.transform.SetPositionAndRotation(marker.position, marker.rotation);
    }

    // Setara tween Godot: TRANS_CUBIC + EASE_IN_OUT, posisi & rotasi bersamaan
    IEnumerator PanCameraTo(Transform marker, float duration)
    {
        if (cam == null || marker == null) yield break;

        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = EaseInOutCubic(Mathf.Clamp01(t / duration));
            cam.transform.SetPositionAndRotation(
                Vector3.Lerp(startPos, marker.position, k),
                Quaternion.Slerp(startRot, marker.rotation, k));
            yield return null;
        }
        SnapCameraTo(marker);
    }

    static float EaseInOutCubic(float x)
    {
        return x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) / 2f;
    }
}