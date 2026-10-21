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

    [Tooltip("Urutan: [0] Merah, [1] Biru, [2] Hijau, [3] Kuning (sama seperti PlayerManager)")]
    public Texture2D[] playerTextures;

    [Header("Titik Posisi Karakter (urut dari juara 1)")]
    [Tooltip("[0] Juara 1 (tengah)\n[1] Juara 2 (kanan)\n[2] Juara 3 (kiri)\n[3] Juara 4 (di kiri juara 3)\n" +
             "Rotasi tiap titik = arah hadap karakter (hadapkan ke kamera).")]
    public Transform[] slots = new Transform[4];

    [Header("Label Teks (opsional, urutan sama dengan Slots)")]
    public TMP_Text[] slotLabels;

    [Header("Animasi Muncul")]
    public bool useReveal = true;
    [Tooltip("Jeda sebelum tiap karakter muncul (mulai dari peringkat terbawah sampai juara 1)")]
    public float revealInterval = 1f;
    public float popDuration = 0.4f;

    [Header("Animator (opsional, kosongkan kalau tidak ada)")]
    [Tooltip("Nama parameter Trigger di Animator untuk pose juara 1 (misal: Win)")]
    public string winnerTrigger = "";
    [Tooltip("Nama parameter Trigger di Animator untuk pemain lain (misal: Lose)")]
    public string loserTrigger = "";

    [Header("Tombol")]
    public Button backToMenuButton;
    public string menuSceneName = "MainMenu";

    [Header("Testing")]
    [Tooltip("Kalau scene Podium dijalankan langsung (tanpa lewat minigame), pakai skor acak.")]
    public bool useTestDataIfEmpty = true;
    [Range(2, 4)] public int testPlayerCount = 4;

    // Property material (URP Lit = _BaseMap, Built-in = _MainTex)
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    void Start()
    {
        // Jaga-jaga kalau scene sebelumnya meninggalkan game dalam keadaan freeze
        Time.timeScale = 1f;

        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.RemoveAllListeners();
            backToMenuButton.onClick.AddListener(BackToMenu);
            backToMenuButton.gameObject.SetActive(false); // muncul setelah semua karakter tampil
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
                ShowButton();
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

            GameObject go = SpawnCharacter(slots[i], results[i].playerIndex);
            spawned.Add(go);
            SetLabel(i, results[i]);
        }

        if (useReveal)
            StartCoroutine(RevealRoutine(spawned, results));
        else
            FinishReveal(spawned, results);
    }

    // ---------- Spawn ----------

    private GameObject SpawnCharacter(Transform slot, int playerIndex)
    {
        if (characterPrefab == null)
        {
            Debug.LogError("[Podium] Character Prefab belum diisi!");
            return null;
        }

        // Dibuat di bawah induk NONAKTIF dulu, supaya Awake/OnEnable script player
        // (PlayerInput, PlayerMovement, dll) tidak sempat jalan sebelum dibuang.
        GameObject holder = new GameObject("PodiumHolder");
        holder.SetActive(false);

        GameObject go = Instantiate(characterPrefab, holder.transform);
        StripGameplayComponents(go);

        if (playerTextures != null && playerIndex >= 0 && playerIndex < playerTextures.Length)
            ApplyTexture(go, playerTextures[playerIndex]);

        // Pindahkan keluar dari holder ke titik podium (menjadi aktif di sini)
        go.transform.SetParent(transform, false);
        go.transform.SetPositionAndRotation(slot.position, slot.rotation);

        Destroy(holder);
        return go;
    }

    private static void StripGameplayComponents(GameObject go)
    {
        // 1. Script (PlayerInput, PlayerMovement, PlayerInteraction, dst), kecuali UI / TMP
        foreach (MonoBehaviour mb in go.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null) continue;

            string ns = mb.GetType().Namespace;
            if (ns != null && (ns.StartsWith("TMPro") || ns.StartsWith("UnityEngine.UI"))) continue;

            DestroyImmediate(mb);
        }

        // 2. Fisika & audio (harus setelah script, karena PlayerMovement butuh Rigidbody)
        foreach (Rigidbody r in go.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(r);
        foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
        foreach (AudioSource a in go.GetComponentsInChildren<AudioSource>(true)) DestroyImmediate(a);
    }

    private void ApplyTexture(GameObject player, Texture2D texture)
    {
        if (texture == null) return;

        Animator animator = player.GetComponentInChildren<Animator>(true);
        Transform modelRoot = animator != null ? animator.transform : player.transform;

        foreach (Renderer r in modelRoot.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material m in r.materials)
            {
                if (m.HasProperty(BaseMapId))
                {
                    m.SetTexture(BaseMapId, texture);
                    if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, Color.white);
                }
                else if (m.HasProperty(MainTexId))
                {
                    m.SetTexture(MainTexId, texture);
                    if (m.HasProperty(ColorId)) m.SetColor(ColorId, Color.white);
                }
            }
        }
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
        slotLabels[slotIndex].gameObject.SetActive(!useReveal); // kalau pakai reveal, muncul belakangan
    }

    // ---------- Reveal ----------

    private IEnumerator RevealRoutine(List<GameObject> spawned, List<PlayerResult> results)
    {
        // Sembunyikan semua dulu
        foreach (GameObject go in spawned)
            if (go != null) go.SetActive(false);

        // Muncul dari peringkat terbawah sampai juara 1
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            yield return new WaitForSeconds(revealInterval);

            if (spawned[i] != null)
                yield return StartCoroutine(PopIn(spawned[i]));

            if (slotLabels != null && i < slotLabels.Length && slotLabels[i] != null)
                slotLabels[i].gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(0.3f);
        FinishReveal(spawned, results);
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

    private void FinishReveal(List<GameObject> spawned, List<PlayerResult> results)
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] == null) continue;
            spawned[i].SetActive(true);

            Animator anim = spawned[i].GetComponentInChildren<Animator>();
            string trigger = results[i].rank == 1 ? winnerTrigger : loserTrigger;
            SetTriggerSafe(anim, trigger);
        }

        if (slotLabels != null)
            for (int i = 0; i < spawned.Count && i < slotLabels.Length; i++)
                if (slotLabels[i] != null) slotLabels[i].gameObject.SetActive(true);

        ShowButton();
    }

    private static void SetTriggerSafe(Animator anim, string triggerName)
    {
        if (anim == null || string.IsNullOrEmpty(triggerName)) return;

        foreach (AnimatorControllerParameter p in anim.parameters)
        {
            if (p.name == triggerName && p.type == AnimatorControllerParameterType.Trigger)
            {
                anim.SetTrigger(triggerName);
                return;
            }
        }
        Debug.LogWarning("[Podium] Parameter Trigger '" + triggerName + "' tidak ada di Animator.");
    }

    private void ShowButton()
    {
        if (backToMenuButton != null) backToMenuButton.gameObject.SetActive(true);
    }

    // ---------- Tombol ----------

    public void BackToMenu()
    {
        if (string.IsNullOrEmpty(menuSceneName))
        {
            Debug.LogWarning("Nama scene menu belum diisi di Inspector Podium!");
            return;
        }

        ScoreBridge.Clear();
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
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