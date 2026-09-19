using UnityEngine;

public class NoteMover : MonoBehaviour
{
    public float speed = 3f; // Atur kecepatan naik nada di sini

    void Update()
    {
        // Vector3.up akan mendorong nada ke atas (Sumbu Y positif)
        transform.Translate(Vector3.up * speed * Time.deltaTime);
    }
}