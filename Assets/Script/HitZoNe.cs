using UnityEngine;

public class HitZone : MonoBehaviour
{
    private GameObject activeNote = null;

    void OnTriggerEnter(Collider other)
    {
        // Pastikan prefab Nada milikmu menggunakan Tag "Note"
        if (other.CompareTag("Note"))
        {
            activeNote = other.gameObject;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Note") && other.gameObject == activeNote)
        {
            activeNote = null;
            Debug.Log("MISS! Nada terlewat.");
        }
    }

    public void AttemptHit()
    {
        if (activeNote != null)
        {
            Debug.Log("PERFECT HIT!");
            Destroy(activeNote);
            activeNote = null;
        }
        else
        {
            Debug.Log("MISS! Memencet terlalu cepat/saat kosong.");
        }
    }
}