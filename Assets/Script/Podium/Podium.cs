using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Podium : MonoBehaviour
{
    [Header("Prefab Karakter")]
    [Tooltip("Boleh pakai prefab player asli. Script gameplay, Rigidbody, Collider, dan PlayerInput " +
             "otomatis dibuang dari salinan yang muncul di podium.")]
    public GameObject characterPrefab;

    [Tooltip("(Opsional) Animator Controller khusus podium yang punya state Rank1..Rank4. " +
             "Kosongkan kalau state-nya ditaruh di controller bawaan hippo.")]
    public RuntimeAnimatorController podiumController;

    [Tooltip("Urutan: [0] Merah, [1] Biru, [2] Hijau, [3] Kuning (sama seperti PlayerManager)")]
    public Texture2D[] playerTextures;

    [Header("Titik Posisi Karakter (urut dari juara 1)")]
    [Tooltip("[0] Juara 1 (tengah)\n[1] Juara 2 (kanan)\n[2] Juara 3 (kiri)\n[3] Juara 4 (di kiri juara 3)\n" +
             "Rotasi tiap titik = arah hadap karakter (hadapkan ke kamera).")]
    public Transform[] slots = new Transform[4];

    [Header("Animasi per Posisi")]
    [Tooltip("Nama STATE di Animator Controller untuk tiap posisi (urutan sama dengan Slots). " +
             "Dimainkan begitu karakter muncul.")]
    public string[] rankStates = { "Rank1", "Rank2", "Rank3", "Rank4" };

    [Header("Label Teks (opsional, urutan sama dengan Slots)")]
    public TMP_Text[] slotLabels;

    [Header("Animasi Muncul")]
    public bool useReveal = true;
    [Tooltip("Jeda sebelum tiap karakter muncul (mulai dari peringkat terbawah sampai juara 1)")]
    public float revealInterval = 1f;
    public float popDuration = 0.4f;

    [Header("Pindah ke Scene Berikutnya (Scene Dipecat)")]
    public string nextSceneName = "";
    [Tooltip("Centang: otomatis pindah setelah semua karakter tampil + jeda di bawah.")]
    public bool autoLoadNextScene = true;
    [Tooltip("Lama podium dilihat (detik) setelah juara 1 muncul, sebelum pindah scene.")]
    public float delayBeforeNextScene = 5f;
    [Tooltip("(Opsional) Tombol untuk melewati podium. Muncul setelah semua karakter tampil.")]
    public Button continueButton;

    [Header("Testing")]
    [Tooltip("Kalau scene Podium dijalankan langsung (tanpa lewat minigame), pakai skor acak.")]
    public bool useTestDataIfEmpty = true;
    [Range(2, 4)] public int testPlayerCount = 4;

    private bool isLoading;

    void Start()
    {
        // Jaga-jaga kalau scene sebelumnya meninggalkan game dalam keadaan freeze
        Time.timeScale = 1f;

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(LoadNextScene);
            continueButton.gameObject.SetActive(false); // muncul setelah semua karakter tampil
        }

        if (!ScoreBridge.HasResults)
        {
            if (useTestDataIfEmpty)
            {
                Debug.LogWarning("[Podium] Tidak ada data skor, memakai data TEST acak.");
                FillTestData();
            }
            else
            {
                Debug.LogWarning("[Podium] Tidak ada data skor dari minigame.");
                HideAllLabels();
                return;
            }
        }

        List<PlayerResult> results = ScoreBridge.Results;
        int count = Mathf.Min(results.Count, slots.Length);

        HideAllLabels();

        // Spawn semua karakter dulu
        List<GameObject> spawned = new List<GameObject>();
        for (int i = 0; i < count; i++)
        {
            if (slots[i] == null)
            {
                Debug.LogWarning("[Podium] Slots[" + i + "] belum diisi di Inspector.");
                spawned.Add(null);
                continue;
            }

            int pi = results[i].playerIndex;
            Texture2D tex = (playerTextures != null && pi >= 0 && pi < playerTextures.Length) ? playerTextures[pi] : null;

            GameObject go = PodiumCharacterUtil.Spawn(characterPrefab, transform,
                                                      slots[i].position, slots[i].rotation, tex, podiumController);
            spawned.Add(go);
            SetLabel(i, results[i]);
        }

        StartCoroutine(PodiumRoutine(spawned));
    }

    // ---------- Alur utama ----------

    private IEnumerator PodiumRoutine(List<GameObject> spawned)
    {
        if (useReveal)
        {
            // Sembunyikan semua dulu
            foreach (GameObject go in spawned)
                if (go != null) go.SetActive(false);

            // Muncul dari peringkat terbawah sampai juara 1
            for (int i = spawned.Count - 1; i >= 0; i--)
            {
                yield return new WaitForSeconds(revealInterval);

                if (spawned[i] != null)
                {
                    yield return StartCoroutine(PopIn(spawned[i]));
                    PlayPositionAnimation(spawned[i], i);
                }

                ShowLabel(i);
            }
        }
        else
        {
            for (int i = 0; i < spawned.Count; i++)
            {
                PlayPositionAnimation(spawned[i], i);
                ShowLabel(i);
            }
        }

        if (continueButton != null) continueButton.gameObject.SetActive(true);

        if (autoLoadNextScene && !string.IsNullOrEmpty(nextSceneName))
        {
            yield return new WaitForSeconds(delayBeforeNextScene);
            LoadNextScene();
        }
    }

    private void PlayPositionAnimation(GameObject go, int slotIndex)
    {
        if (go == null || rankStates == null || slotIndex >= rankStates.Length) return;
        PodiumCharacterUtil.PlayState(go, rankStates[slotIndex]);
    }

    private IEnumerator PopIn(GameObject go)
    {
        Vector3 target = go.transform.localScale;
        go.transform.localScale = Vector3.zero;
        go.SetActive(true);

        float t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / popDuration);

            // Ease-out-back: sedikit membesar melewati ukuran asli lalu pas
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float e = 1f + c3 * Mathf.Pow(k - 1f, 3f) + c1 * Mathf.Pow(k - 1f, 2f);

            go.transform.localScale = target * e;
            yield return null;
        }
        go.transform.localScale = target;
    }

    // ---------- Label ----------

    private void HideAllLabels()
    {
        if (slotLabels == null) return;
        foreach (TMP_Text t in slotLabels)
            if (t != null) t.gameObject.SetActive(false);
    }

    private void SetLabel(int slotIndex, PlayerResult r)
    {
        if (slotLabels == null || slotIndex >= slotLabels.Length || slotLabels[slotIndex] == null) return;

        slotLabels[slotIndex].text = r.playerName + "\n" + r.score;
        slotLabels[slotIndex].gameObject.SetActive(false); // dimunculkan lewat ShowLabel
    }

    private void ShowLabel(int slotIndex)
    {
        if (slotLabels == null || slotIndex >= slotLabels.Length || slotLabels[slotIndex] == null) return;
        slotLabels[slotIndex].gameObject.SetActive(true);
    }

    // ---------- Pindah scene ----------

    public void LoadNextScene()
    {
        if (isLoading) return;

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("[Podium] Next Scene Name belum diisi di Inspector!");
            return;
        }

        isLoading = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName); // ScoreBridge tetap utuh, dipakai scene Dipecat
    }

    // ---------- Testing ----------

    private void FillTestData()
    {
        string[] names = { "Merah", "Biru", "Hijau", "Kuning" };
        List<PlayerResult> list = new List<PlayerResult>();

        for (int i = 0; i < testPlayerCount; i++)
        {
            list.Add(new PlayerResult
            {
                playerIndex = i,
                playerName = names[i],
                score = Random.Range(0, 15)
            });
        }
        ScoreBridge.SetResults(list);
    }
}