using UnityEngine;
using TMPro; // Wajib ditambahkan untuk menggunakan TextMeshPro

public class BoxStorage : MonoBehaviour
{
    [Header("Pengaturan Tempat Penyimpanan")]
    [Tooltip("Posisi munculnya kotak saat dicuri dari truk ini (opsional, kosong = posisi Drop Zone).")]
    public Transform storageStartPoint;

    [Header("UI Skor 2D")]
    public TextMeshProUGUI scoreText; // Referensi untuk teks 2D (Canvas UI) skor

    // Skor = jumlah kotak yang sudah masuk truk ini.
    // Kotak fisiknya dikembalikan ke pool, jadi yang disimpan cukup angkanya saja.
    private int storedCount = 0;

    void Start()
    {
        UpdateScoreUI(); // Set angka menjadi 0 saat game dimulai
    }

    // Mengambil skor saat ini (jumlah kotak di zona ini)
    public int GetScore()
    {
        return storedCount;
    }

    // Dipanggil saat player menaruh kotak di Drop Zone
    public void AddBox(GameObject box)
    {
        storedCount++;

        // Kembalikan kotak ke pool (dinonaktifkan), nanti dipakai lagi di conveyor
        if (BoxPool.Instance != null && box.GetComponent<PooledBox>() != null)
            BoxPool.Instance.Release(box);
        else
            Destroy(box); // cadangan kalau kotak bukan berasal dari pool

        UpdateScoreUI();
    }

    // Dipanggil saat player mencuri kotak dari Drop Zone.
    // Kotak fisik sudah dikembalikan ke pool, jadi kotak curian diambil dari pool lagi.
    public GameObject RemoveLastBox()
    {
        if (storedCount <= 0) return null;
        if (BoxPool.Instance == null) return null;

        Vector3 pos = storageStartPoint != null ? storageStartPoint.position : transform.position;
        Quaternion rot = storageStartPoint != null ? storageStartPoint.rotation : transform.rotation;

        GameObject box = BoxPool.Instance.Get(pos, rot);
        if (box == null) return null; // pool habis dan tidak boleh bertambah -> batal curi

        storedCount--;
        UpdateScoreUI();
        return box;
    }

    // Fungsi untuk memperbarui teks angka di atas storage
    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = storedCount.ToString();
        }
    }
}