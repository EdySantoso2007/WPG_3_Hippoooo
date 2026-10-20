using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Object pool untuk kotak. Kotak dibuat di awal lalu dipakai ulang,
/// jadi tidak ada Instantiate terus-menerus saat game berjalan.
/// Taruh script ini di 1 GameObject kosong bernama "BoxPool" di scene.
/// </summary>
public class BoxPool : MonoBehaviour
{
    public static BoxPool Instance { get; private set; }

    [Header("Pengaturan Pool")]
    public GameObject boxPrefab;      // Prefab kotak (harus punya Rigidbody & Collider)
    public int initialSize = 20;      // Jumlah kotak yang dibuat di awal

    [Tooltip("Kalau semua kotak sedang terpakai: true = buat kotak baru, false = jangan spawn dulu.")]
    public bool canExpand = true;

    private readonly Stack<GameObject> available = new Stack<GameObject>();
    private int totalCreated;
    private bool warnedExpand;

    void Awake()
    {
        Instance = this;

        if (boxPrefab == null)
        {
            Debug.LogError("[BoxPool] Box Prefab belum diisi!");
            return;
        }

        for (int i = 0; i < initialSize; i++)
            available.Push(CreateNew());
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private GameObject CreateNew()
    {
        GameObject go = Instantiate(boxPrefab, transform);
        go.SetActive(false);
        totalCreated++;

        PooledBox pb = go.GetComponent<PooledBox>();
        if (pb == null) pb = go.AddComponent<PooledBox>();
        pb.InPool = true;

        return go;
    }

    /// <summary>Ambil kotak dari pool dan letakkan di posisi tertentu. Null jika pool habis dan canExpand = false.</summary>
    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject box = null;

        // Lewati entri yang ternyata sudah dihancurkan
        while (available.Count > 0 && box == null)
            box = available.Pop();

        if (box == null)
        {
            if (!canExpand) return null;

            box = CreateNew();

            if (!warnedExpand)
            {
                warnedExpand = true;
                Debug.LogWarning("[BoxPool] Pool habis, membuat kotak baru. " +
                                 "Naikkan Initial Size kalau ini sering terjadi.");
            }
        }

        // Reset kondisi kotak seperti baru
        box.transform.SetParent(null);
        box.transform.SetPositionAndRotation(position, rotation);
        box.tag = "Box";

        Rigidbody rb = box.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;           // harus sebelum set velocity
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Collider col = box.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        box.GetComponent<PooledBox>().InPool = false;
        box.SetActive(true);
        return box;
    }

    /// <summary>Kembalikan kotak ke pool (dinonaktifkan, bukan dihancurkan).</summary>
    public void Release(GameObject box)
    {
        if (box == null) return;

        PooledBox pb = box.GetComponent<PooledBox>();
        if (pb == null || pb.InPool) return; // bukan kotak pool / sudah dikembalikan

        pb.InPool = true;
        box.SetActive(false);
        box.transform.SetParent(transform);
        box.tag = "Box";
        available.Push(box);
    }
}