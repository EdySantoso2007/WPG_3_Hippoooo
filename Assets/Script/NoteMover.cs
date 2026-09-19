using UnityEngine;

public class NoteMover : MonoBehaviour
{
    public float speed = 3f;

    void Update()
    {
        // Ubah menjadi Vector3.down agar nada jatuh dari atas ke bawah
        transform.Translate(Vector3.down * speed * Time.deltaTime);
    }
}