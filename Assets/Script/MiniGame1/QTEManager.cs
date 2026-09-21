using UnityEngine;
using System.Collections.Generic;

public class QTEManager : MonoBehaviour
{
    [Header("Masukkan 4 Prefab UI (UINoteA, X, B, Y)")]
    public GameObject[] notePrefabs;

    [Header("Wadah Container di dalam Canvas (QTE_Container)")]
    public Transform qteContainer;

    [Header("Pengaturan Tumpukan")]
    public int qteLength = 5; // Jumlah awal balok yang menumpuk
    public int batasMaksimal = 8; // Batas overflow sebelum balok bawah hancur

    private List<GameObject> activeNotes = new List<GameObject>();

    void Start()
    {
        // Munculkan tumpukan awal saat permainan dimulai
        for(int i = 0; i < qteLength; i++)
        {
            SpawnNote();
        }
    }

    void SpawnNote()
    {
        if (notePrefabs.Length == 0 || qteContainer == null) return;

        // Cek jika tumpukan sudah melebihi batas maksimal (Overflow)
        if (activeNotes.Count >= batasMaksimal)
        {
            Debug.Log("OVERFLOW! Tumpukan penuh, penalti!");
            GameObject balokHangus = activeNotes[0];
            activeNotes.RemoveAt(0);
            Destroy(balokHangus);
        }

        // Pilih salah satu prefab tombol secara acak (A, X, B, atau Y)
        int rnd = Random.Range(0, notePrefabs.Length);
        GameObject newNote = Instantiate(notePrefabs[rnd], qteContainer);

        // Karena pakai Vertical Layout Group, posisikan di urutan paling bawah
        newNote.transform.SetAsFirstSibling();
        activeNotes.Add(newNote);
    }

    // Dipanggil oleh RhythmController saat pemain menekan tombol (1=A, 2=X, 3=B, 4=Y)
    public void CheckInput(int buttonIndex)
    {
        if (activeNotes.Count == 0) return;

        // Ambil tombol yang berada di paling bawah tumpukan
        GameObject bottomNote = activeNotes[activeNotes.Count - 1];
        UINote noteData = bottomNote.GetComponent<UINote>();

        if (noteData != null)
        {
            if (noteData.noteType == buttonIndex)
            {
                Debug.Log("PERFECT HIT!");
                activeNotes.RemoveAt(activeNotes.Count - 1);
                Destroy(bottomNote);

                // Munculkan tombol baru untuk menjaga tumpukan
                SpawnNote();
            }
            else
            {
                Debug.Log("SALAH TOMBOL!");
                // Bisa ditambahkan penalti pengurangan skor di sini jika mau
            }
        }
    }
}
