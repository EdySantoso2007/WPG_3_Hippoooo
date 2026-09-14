using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    private PlayerInput playerInput;
    private InputAction interactAction;

    private GameObject boxToPickUp;
    private GameObject carriedBox;
    private BoxStorage currentDropZone;

    public Transform holdPoint;

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        interactAction = playerInput.actions["Interact"];
    }

    void Update()
    {
        if (interactAction != null && interactAction.WasPressedThisFrame())
        {
            // Jika sedang di dalam area Drop Zone
            if (currentDropZone != null)
            {
                // Taruh Kotak
                if (carriedBox != null)
                {
                    currentDropZone.AddBox(carriedBox);

                    // Lepas kotak dari holdPoint agar tidak ikut bergerak mengikuti karakter
                    carriedBox.transform.SetParent(null);
                    carriedBox = null;
                }
                // Curi Kotak
                else
                {
                    GameObject stolenBox = currentDropZone.RemoveLastBox();
                    if (stolenBox != null)
                    {
                        carriedBox = stolenBox;
                        carriedBox.transform.position = holdPoint.position;
                        carriedBox.transform.SetParent(holdPoint);
                    }
                }
            }
            // Jika sedang di luar Drop Zone (Interaksi biasa dengan kotak di lantai)
            else
            {
                // Ambil kotak dari lantai
                if (carriedBox == null && boxToPickUp != null)
                {
                    carriedBox = boxToPickUp;
                    boxToPickUp = null; // Reset referensi agar tidak tersangkut

                    carriedBox.GetComponent<Rigidbody>().isKinematic = true;
                    carriedBox.GetComponent<Collider>().enabled = false;

                    carriedBox.transform.position = holdPoint.position;
                    carriedBox.transform.SetParent(holdPoint);
                }
                // Jatuhkan kotak ke lantai
                else if (carriedBox != null)
                {
                    carriedBox.transform.SetParent(null);

                    Rigidbody rb = carriedBox.GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = false;

                    Collider col = carriedBox.GetComponent<Collider>();
                    if (col != null) col.enabled = true;

                    carriedBox.tag = "Box";
                    carriedBox = null;
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Box"))
        {
            boxToPickUp = other.gameObject;
        }
        else if (other.CompareTag("DropZone"))
        {
            currentDropZone = other.GetComponent<BoxStorage>();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Box") && boxToPickUp == other.gameObject)
        {
            boxToPickUp = null;
        }
        else if (other.CompareTag("DropZone"))
        {
            BoxStorage zone = other.GetComponent<BoxStorage>();
            if (currentDropZone == zone)
            {
                currentDropZone = null;
            }
        }
    }

    // ---------------------------------------------------------
    // FUNGSI BARU: Dipanggil saat ditabrak (Dash) oleh pemain lain
    // ---------------------------------------------------------
    public void ForceDropBox(Vector3 throwForce)
    {
        if (carriedBox != null)
        {
            // Lepaskan kotak dari tangan
            carriedBox.transform.SetParent(null);

            Rigidbody rb = carriedBox.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                // Lempar kotaknya sesuai kekuatan pantulan dari pemain yang menabrak
                rb.AddForce(throwForce, ForceMode.Impulse);
            }

            Collider col = carriedBox.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            carriedBox.tag = "Box"; // Kembalikan tag-nya menjadi Box agar bisa diambil lagi
            carriedBox = null;
        }
    }
}