using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pasang di GameObject kosong yang punya BoxCollider (centang Is Trigger).
/// Cukup atur ukuran BoxCollider-nya untuk menentukan area zona.
/// Tidak butuh Rigidbody karena pengecekan dilakukan oleh DynamicMultiplayerCamera.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CameraZone : MonoBehaviour
{
    public enum ZoneType { Near, Far }

    [Tooltip("Near = kamera dekat. Far = kamera jauh (prioritas lebih tinggi dari Near).")]
    public ZoneType type = ZoneType.Near;

    [Tooltip("Isi > 0 untuk mengganti jarak kamera default zona ini. 0 = pakai nilai dari kamera.")]
    public float customDistance = 0f;

    // Daftar semua zona aktif, supaya kamera tidak perlu mencari lewat Find
    public static readonly List<CameraZone> All = new List<CameraZone>();

    private Collider col;

    void Awake()
    {
        col = GetComponent<Collider>();
    }

    void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    // Cek posisi di lantai saja (X dan Z). Tinggi (Y) diabaikan.
    public bool Contains(Vector3 worldPos)
    {
        if (col == null) col = GetComponent<Collider>();
        Bounds b = col.bounds;
        return worldPos.x >= b.min.x && worldPos.x <= b.max.x &&
               worldPos.z >= b.min.z && worldPos.z <= b.max.z;
    }

    void OnDrawGizmos()
    {
        var c = GetComponent<Collider>();
        if (c == null) return;

        Gizmos.color = type == ZoneType.Far
            ? new Color(1f, 0.3f, 0.3f, 0.35f)
            : new Color(0.3f, 0.6f, 1f, 0.35f);

        Bounds b = c.bounds;
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 1f);
        Gizmos.DrawWireCube(b.center, b.size);
    }
}