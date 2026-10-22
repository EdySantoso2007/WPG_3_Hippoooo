using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [Header("Pengaturan Waktu")]
    public float timeRemaining = 180f; // 180 detik = 3 menit
    public bool timerIsRunning = false;

    [Header("UI Referensi")]
    public TMP_Text timeText;

    [Header("UI Waktu Habis")]
    [Tooltip("Panel berisi sprite 'Waktu Habis' + teks skor. CanvasGroup ditambahkan otomatis kalau belum ada.")]
    public GameObject timeUpPanel;
    [Tooltip("Lama panel fade-in (detik).")]
    public float fadeInDuration = 0.8f;
    [Tooltip("Lama panel tampil penuh (detik) setelah fade-in selesai, sebelum transisi ke scene berikutnya.")]
    public float showDuration = 3f;

    [Header("Transisi Iris (Fade Out lingkaran)")]
    [Tooltip("Drag objek IrisTransition dari scene ini. Kosong = langsung pindah scene tanpa transisi.")]
    public IrisTransition iris;
    public float irisCloseDuration = 0.8f;

    [Header("Skor Akhir (urutan harus sama)")]
    public BoxStorage[] storages;         // Drop Zone (maks 4)
    public TMP_Text[] finalScoreTexts;    // Satu teks untuk tiap Drop Zone
    public string[] storageNames;         // (Opsional) Nama tiap zona, misal "Player 1"
    public TMP_Text winnerText;           // (Opsional) Teks pemenang

    [Header("Nama Scene")]
    public string podiumSceneName = "Podium";

    private bool timeUpTriggered = false;

    void Start()
    {
        // Tampilkan angka awal (03:00)
        DisplayTime(timeRemaining);

        // Sembunyikan panel saat game baru mulai
        if (timeUpPanel != null)
        {
            timeUpPanel.SetActive(false);
        }

        // Jalankan timer SETELAH hitungan 3,2,1 dan teks GO! selesai
        StartCoroutine(StartAfterIntro());
    }

    IEnumerator StartAfterIntro()
    {
        // Selama hitungan + GO!, kamera menyetel InputLocked = true.
        // Coroutine ini berjalan per frame, jadi tetap jalan walau Time.timeScale = 0.
        while (DynamicMultiplayerCamera.InputLocked)
            yield return null;

        StartTimer();
    }

    void Update()
    {
        if (!timerIsRunning) return;

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            DisplayTime(timeRemaining);
        }
        else
        {
            TimeUp();
        }
    }

    void TimeUp()
    {
        if (timeUpTriggered) return;
        timeUpTriggered = true;

        timeRemaining = 0;
        timerIsRunning = false;
        DisplayTime(timeRemaining);

        // Freeze semua pergerakan game
        Time.timeScale = 0f;

        // Hitung & simpan skor (untuk teks di panel dan scene Podium)
        ShowFinalScore();

        Debug.Log("Waktu Habis! Game Berhenti Total.");

        StartCoroutine(TimeUpSequence());
    }

    // Panel fade-in -> tunggu beberapa detik -> iris menutup -> pindah ke scene Podium.
    // Semua pakai waktu nyata (unscaled) karena Time.timeScale = 0.
    IEnumerator TimeUpSequence()
    {
        if (timeUpPanel != null)
        {
            CanvasGroup cg = timeUpPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = timeUpPanel.AddComponent<CanvasGroup>();

            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            timeUpPanel.SetActive(true);

            float t = 0f;
            while (t < fadeInDuration)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(t / fadeInDuration);
                yield return null;
            }
            cg.alpha = 1f;
        }

        yield return new WaitForSecondsRealtime(showDuration);

        // Fade Out lingkaran, layar jadi hitam sebelum scene diganti
        if (iris != null)
            yield return iris.Close(irisCloseDuration);

        LoadScene(podiumSceneName);
    }

    void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("Nama scene Podium belum diisi di Inspector GameTimer!");
            return;
        }

        // Wajib dikembalikan ke 1, kalau tidak scene berikutnya ikut ter-freeze
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    // Jumlah pemain yang benar-benar ikut main.
    // Kalau tidak lewat Room/Lobby (testing langsung di scene ini), semua Drop Zone dianggap ikut.
    int GetActivePlayerCount()
    {
        int max = storages != null ? storages.Length : 0;
        int joined = RoomBridge.JoinedPlayers.Count;
        return joined > 0 ? Mathf.Min(joined, max) : max;
    }

    void ShowFinalScore()
    {
        if (storages == null || finalScoreTexts == null) return;

        int activeCount = GetActivePlayerCount();

        int highScore = -1;
        int winnerIndex = -1;
        bool isDraw = false;

        List<PlayerResult> results = new List<PlayerResult>();

        for (int i = 0; i < finalScoreTexts.Length; i++)
        {
            if (finalScoreTexts[i] == null) continue;

            // Sembunyikan teks untuk Drop Zone tanpa pemain
            if (i >= activeCount || storages[i] == null)
            {
                finalScoreTexts[i].gameObject.SetActive(false);
                continue;
            }

            finalScoreTexts[i].gameObject.SetActive(true);

            int score = storages[i].GetScore();
            finalScoreTexts[i].text = score.ToString();

            // Catat untuk scene Podium (playerIndex = urutan Drop Zone = urutan warna join)
            results.Add(new PlayerResult
            {
                playerIndex = i,
                playerName = GetZoneName(i),
                score = score
            });

            if (score > highScore)
            {
                highScore = score;
                winnerIndex = i;
                isDraw = false;
            }
            else if (score == highScore)
            {
                isDraw = true;
            }
        }

        // Simpan & urutkan ranking untuk scene Podium
        ScoreBridge.SetResults(results);

        if (winnerText != null)
        {
            if (isDraw)
            {
                winnerText.text = "SERI!";
            }
            else if (winnerIndex >= 0)
            {
                winnerText.text = "Pemenang: " + GetZoneName(winnerIndex);
            }
            else
            {
                winnerText.text = "";
            }
        }
    }

    string GetZoneName(int index)
    {
        if (storageNames != null && index < storageNames.Length && !string.IsNullOrEmpty(storageNames[index]))
        {
            return storageNames[index];
        }
        return "Zona " + (index + 1);
    }

    void DisplayTime(float timeToDisplay)
    {
        if (timeToDisplay < 0)
        {
            timeToDisplay = 0;
        }

        int minutes = Mathf.FloorToInt(timeToDisplay / 60);
        int seconds = Mathf.FloorToInt(timeToDisplay % 60);

        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void StartTimer()
    {
        timerIsRunning = true;

        // Pastikan game berjalan normal (jaga-jaga kalau sebelumnya di-freeze)
        Time.timeScale = 1f;
    }
}