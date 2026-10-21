using UnityEngine;

/// <summary>
/// Jalur tengah conveyor berupa deretan titik (waypoint). Dipakai ConveyorBelt supaya kotak / player
/// mengikuti jalur yang sama, termasuk di belokan, tanpa bergantung pada sambungan antar collider.
/// Letakkan titik-titik di GARIS TENGAH belt, urut dari awal sampai ujung.
/// </summary>
public class ConveyorPath : MonoBehaviour
{
    [Tooltip("Titik-titik jalur, urut dari awal conveyor sampai ujung. Ketinggian (Y) diabaikan.")]
    public Transform[] points;

    static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }

    /// <summary>Cari titik terdekat di jalur dari 'position', dan arah jalur (tangent) di titik itu.</summary>
    public bool Sample(Vector3 position, out Vector3 closest, out Vector3 tangent)
    {
        closest = position;
        tangent = Vector3.zero;
        if (points == null || points.Length < 2) return false;

        Vector3 p = Flat(position);
        float best = float.MaxValue;
        bool found = false;

        for (int i = 0; i < points.Length - 1; i++)
        {
            if (points[i] == null || points[i + 1] == null) continue;

            Vector3 a = Flat(points[i].position);
            Vector3 b = Flat(points[i + 1].position);
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 0.000001f) continue;

            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2);
            Vector3 c = a + ab * t;
            float d = (p - c).sqrMagnitude;

            if (d < best)
            {
                best = d;
                closest = c;
                tangent = ab / Mathf.Sqrt(len2);
                found = true;
            }
        }
        return found;
    }

    /// <summary>Index titik tujuan berikutnya untuk objek yang berada di 'position' (segmen terdekat).</summary>
    public int GetNextPointIndex(Vector3 position)
    {
        if (points == null || points.Length < 2) return 0;

        Vector3 p = Flat(position);
        float best = float.MaxValue;
        int bestIndex = 1;

        for (int i = 0; i < points.Length - 1; i++)
        {
            if (points[i] == null || points[i + 1] == null) continue;

            Vector3 a = Flat(points[i].position);
            Vector3 b = Flat(points[i + 1].position);
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 0.000001f) continue;

            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2);
            float d = (p - (a + ab * t)).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestIndex = i + 1;
            }
        }
        return bestIndex;
    }

    /// <summary>Panjang total jalur (bidang datar).</summary>
    public float TotalLength
    {
        get
        {
            if (points == null) return 0f;
            float total = 0f;
            for (int i = 0; i < points.Length - 1; i++)
            {
                if (points[i] == null || points[i + 1] == null) continue;
                total += (Flat(points[i + 1].position) - Flat(points[i].position)).magnitude;
            }
            return total;
        }
    }

    /// <summary>Jarak dari awal jalur sampai titik terdekat dengan 'position'.</summary>
    public float GetProgress(Vector3 position)
    {
        if (points == null) return 0f;

        Vector3 p = Flat(position);
        float best = float.MaxValue;
        float result = 0f;
        float accumulated = 0f;

        for (int i = 0; i < points.Length - 1; i++)
        {
            if (points[i] == null || points[i + 1] == null) continue;

            Vector3 a = Flat(points[i].position);
            Vector3 ab = Flat(points[i + 1].position) - a;
            float len = ab.magnitude;
            if (len < 0.001f) continue;

            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / (len * len));
            float d = (p - (a + ab * t)).sqrMagnitude;
            if (d < best)
            {
                best = d;
                result = accumulated + t * len;
            }
            accumulated += len;
        }
        return result;
    }

    /// <summary>Posisi di jalur pada jarak 's' dari awal, beserta arah jalurnya.</summary>
    public Vector3 GetPointAt(float s, out Vector3 tangent)
    {
        tangent = Vector3.forward;
        Vector3 last = transform.position;
        if (points == null) return last;

        float remaining = Mathf.Max(0f, s);

        for (int i = 0; i < points.Length - 1; i++)
        {
            if (points[i] == null || points[i + 1] == null) continue;

            Vector3 a = Flat(points[i].position);
            Vector3 ab = Flat(points[i + 1].position) - a;
            float len = ab.magnitude;
            if (len < 0.001f) continue;

            tangent = ab / len;
            last = a + ab;

            if (remaining <= len) return a + tangent * remaining;
            remaining -= len;
        }
        return last;
    }

    // Gambar jalur di Scene view
    void OnDrawGizmos()
    {
        if (points == null) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null) continue;
            Gizmos.DrawSphere(points[i].position, 0.12f);
            if (i + 1 < points.Length && points[i + 1] != null)
                Gizmos.DrawLine(points[i].position, points[i + 1].position);
        }
    }
}