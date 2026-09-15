using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [Header("Pengaturan Waktu")]
    public float timeRemaining = 180f;
    public bool timerIsRunning = false;

    [Header("UI Referensi")]
    public TMP_Text timeText;
    public GameObject playButton;

    void Start()
    {
        // Memastikan waktu game berjalan normal di awal
        Time.timeScale = 1f;
        DisplayTime(timeRemaining);
    }

    void Update()
    {
        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                DisplayTime(timeRemaining);
            }
            else
            {
                // Waktu Habis!
                timeRemaining = 0;
                timerIsRunning = false;
                DisplayTime(timeRemaining);

                // MENGHENTIKAN SEMUA PERGERAKAN GAME
                Time.timeScale = 0f;

                Debug.Log("Waktu Habis! Game Berhenti Total.");
            }
        }
    }

    void DisplayTime(float timeToDisplay)
    {
        if (timeToDisplay < 0) timeToDisplay = 0;

        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void StartTimer()
    {
        timerIsRunning = true;

        // Memastikan game bergerak saat tombol Play ditekan
        Time.timeScale = 1f;

        if (playButton != null)
        {
            playButton.SetActive(false);
        }
    }
}