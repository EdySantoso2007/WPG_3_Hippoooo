using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [Header("Pengaturan Waktu")]
    public float timeRemaining = 180f; // 180 detik = 3 menit
    public bool timerIsRunning = false;

    [Header("UI Referensi")]
    public TMP_Text timeText;
    public GameObject playButton;
    public GameObject timeUpText; // Tulisan Waktu Habis

    void Start()
    {
        // Tampilkan angka awal (03:00)
        DisplayTime(timeRemaining);

        // Sembunyikan tulisan waktu habis saat game baru mulai
        if (timeUpText != null)
        {
            timeUpText.SetActive(false);
        }

        // Tombol Play tidak dipakai lagi karena timer langsung jalan
        if (playButton != null)
        {
            playButton.SetActive(false);
        }

        // Langsung jalankan timer tanpa menunggu tombol ditekan
        StartTimer();
    }

    void Update()
    {
        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                // Kurangi waktu setiap detiknya (berdasarkan frame)
                timeRemaining -= Time.deltaTime;
                DisplayTime(timeRemaining);
            }
            else
            {
                // Waktu habis
                timeRemaining = 0;
                timerIsRunning = false;
                DisplayTime(timeRemaining);

                // MENGHENTIKAN SEMUA PERGERAKAN GAME (Freeze)
                Time.timeScale = 0f;

                // Munculkan tulisan WAKTU HABIS
                if (timeUpText != null)
                {
                    timeUpText.SetActive(true);
                }

                Debug.Log("Waktu Habis! Game Berhenti Total.");
            }
        }
    }

    void DisplayTime(float timeToDisplay)
    {
        if (timeToDisplay < 0)
        {
            timeToDisplay = 0;
        }

        // Menghitung jumlah menit dan detik
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        // Menampilkan format angka dengan dua digit, misal: 03:00
        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // Tetap dipertahankan (misal masih dipanggil dari tempat lain / testing),
    // tapi sekarang juga dipanggil otomatis dari Start()
    public void StartTimer()
    {
        timerIsRunning = true;

        // Pastikan game berjalan normal (jaga-jaga kalau sebelumnya di-freeze)
        Time.timeScale = 1f;

        if (playButton != null)
        {
            playButton.SetActive(false);
        }
    }
}