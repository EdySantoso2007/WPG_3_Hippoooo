using UnityEngine;

public class NoteDestroyer : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // Jika yang menabrak zona ini adalah Nada, langsung hancurkan!
        if (other.CompareTag("Note"))
        {
            Destroy(other.gameObject);
        }
    }
}