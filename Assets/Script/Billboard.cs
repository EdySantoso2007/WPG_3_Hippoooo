using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCam;

    void Start()
    {
        // Mencari kamera utama di dalam game secara otomatis
        mainCam = Camera.main; 
    }

    // LateUpdate berjalan setelah semua pergerakan lain selesai
    void LateUpdate()
    {
        if (mainCam != null)
        {
            // Memaksa punggung teks agar sejajar dengan punggung kamera
            // (Sehingga wajah teks selalu menatap langsung ke kamera)
            transform.forward = mainCam.transform.forward;
        }
    }
}