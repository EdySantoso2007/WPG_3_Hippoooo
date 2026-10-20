using UnityEngine;

/// <summary>
/// Dipasang otomatis oleh BoxPool (boleh juga dipasang manual di prefab kotak).
/// Mengembalikan kotak ke pool kalau jatuh keluar map.
/// </summary>
public class PooledBox : MonoBehaviour
{
    [HideInInspector] public bool InPool;

    [Tooltip("Kalau posisi Y kotak lebih rendah dari ini, kotak dikembalikan ke pool.")]
    public float killY = -20f;

    private float nextCheck;

    void Update()
    {
        // Cek tiap 0.5 detik saja, tidak perlu tiap frame
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + 0.5f;

        if (transform.position.y < killY && BoxPool.Instance != null)
            BoxPool.Instance.Release(gameObject);
    }
}