using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{
    public float pushForce = 500f; // Tenaga dibesarkan
    public Vector3 direction = Vector3.forward;

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Box"))
        {
            Rigidbody rb = collision.rigidbody;

            if (rb != null && !rb.isKinematic)
            {
                // Paksa kotak "bangun" dari Physics Sleep
                rb.WakeUp();
                // Dorong kotak
                rb.AddForce(direction.normalized * pushForce, ForceMode.Force);
            }
        }
    }
}