using UnityEngine;
using UnityEngine.UI;
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
    public GameObject timeUpPanel;        // Panel berisi Image (sprite) "Waktu Habis" + teks skor

    [Header("Skor Akhir (urutan harus sama)")]
    public BoxStorage[] storages;         // Drop Zone (maks 4)
    public TMP_Text[] finalScoreTexts;    // Satu teks untuk tiap Drop Zone
    public string[] storageNames;         // (Opsional) Nama tiap zona, misal "Player 1"
    public TMP_Text winnerText;           // (Opsional) Teks pemenang

    [Header("Tombol Setelah Waktu Habis")]
    public Button backToMenuButton;
    public Button nextGameButton;

    [Header("Nama Scene")]
    public string menuSceneName = "MainMenu";
    public string nextGameSceneName = "";

    void Start()
    {
        // Tampilkan angka awal (03:00)
        DisplayTime(timeRemaining);

        // Sembunyikan panel & tombol saat game baru mulai
        if (timeUpPanel != null)
        {
            timeUpPanel.SetActive(false);
        }
        SetButtonsActive(false);

        // Pasang fungsi ke tombol
        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.RemoveAllListeners();
            backToMenuButton.onClick.AddListener(BackToMenu);
        }

        if (nextGameButton != null)
        {
            nextGameButton.onClick.RemoveAllListeners();
            nextGameButton.onClick.AddListener(NextGame);
        }

        // Langsung jalankan timer
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
        timeRemaining = 0;
        timerIsRunning = false;
        DisplayTime(timeRemaining);

        // Freeze semua pergerakan game
        Time.timeScale = 0f;

        // Munculkan sprite UI waktu habis + skor akhir + tombol
        if (timeUpPanel != null)
        {
            timeUpPanel.SetActive(true);
        }

        ShowFinalScore();
        SetButtonsActive(true);

        Debug.Log("Waktu Habis! Game Berhenti Total.");
    }

    void SetButtonsActive(bool active)
    {
        if (backToMenuButton != null)
        {
            backToMenuButton.gameObject.SetActive(active);
        }

        if (nextGameButton != null)
        {
            nextGameButton.gameObject.SetActive(active);
        }
    }

    public void BackToMenu()
    {
        LoadScene(menuSceneName);
    }

    public void NextGame()
    {
        LoadScene(nextGameSceneName);
    }

    void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("Nama scene belum diisi di Inspector GameTimer!");
            return;
        }

        // Wajib dikembalikan ke 1, kalau tidak scene berikutnya ikut ter-freeze
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    void ShowFinalScore()
    {
        if (storages == null || finalScoreTexts == null) return;

        int highScore = -1;
        int winnerIndex = -1;
        bool isDraw = false;

        for (int i = 0; i < finalScoreTexts.Length; i++)
        {
            if (finalScoreTexts[i] == null) continue;

            // Kalau teks lebih banyak dari Drop Zone, sembunyikan sisanya
            if (i >= storages.Length || storages[i] == null)
            {
                finalScoreTexts[i].gameObject.SetActive(false);
                continue;
            }

            finalScoreTexts[i].gameObject.SetActive(true);

            int score = storages[i].GetScore();
            finalScoreTexts[i].text = score.ToString();

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