using UnityEngine;
using System.Collections.Generic;
using TMPro; // Wajib ditambahkan untuk menggunakan TextMeshPro

public class BoxStorage : MonoBehaviour
{
    [Header("Pengaturan Tempat Penyimpanan")]
    public Transform storageStartPoint;
    public float spacing = 1.2f;
    public int boxesPerRow = 3;

    [Header("UI Skor 2D")]
    public TextMeshProUGUI scoreText; // Referensi untuk teks 2D (Canvas UI) skor

    // Menyimpan daftar kotak agar bisa ditarik kembali
    private List<GameObject> storedBoxes = new List<GameObject>();

    void Start()
    {
        UpdateScoreUI(); // Set angka menjadi 0 saat game dimulai
    }

    // Dipanggil saat player menaruh kotak di Drop Zone
    public void AddBox(GameObject box)
    {
        // Matikan fisika & ubah tag
        box.GetComponent<Rigidbody>().isKinematic = true;
        box.GetComponent<Collider>().enabled = false;
        box.tag = "Untagged"; // Hilangkan tag agar tidak bisa diambil acak

        int count = storedBoxes.Count;
        int row = count / boxesPerRow;
        int colIndex = count % boxesPerRow;

        Vector3 targetPos = storageStartPoint.position
                          + (storageStartPoint.right * (colIndex * spacing))
                          + (storageStartPoint.forward * (row * spacing));

        box.transform.position = targetPos;
        box.transform.rotation = storageStartPoint.rotation;

        // Masukkan ke daftar memori
        storedBoxes.Add(box);

        // Perbarui tampilan angka skor bertambah
        UpdateScoreUI();
    }

    // Dipanggil saat player mencuri kotak dari Drop Zone
    public GameObject RemoveLastBox()
    {
        if (storedBoxes.Count > 0)
        {
            // Ambil kotak urutan paling akhir (yang paling baru ditaruh)
            int lastIndex = storedBoxes.Count - 1;
            GameObject boxToTake = storedBoxes[lastIndex];

            // Hapus dari memori grid
            storedBoxes.RemoveAt(lastIndex);

            // Perbarui tampilan angka skor berkurang
            UpdateScoreUI();

            return boxToTake;
        }
        return null; // Jika tidak ada kotak sama sekali
    }

    // Fungsi untuk memperbarui teks angka di atas storage
    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = storedBoxes.Count.ToString();
        }
    }
}