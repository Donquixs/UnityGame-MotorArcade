using UnityEngine;

public class FinishLineTrigger : MonoBehaviour
{
    public LapTimer lap;
    public Transform finishDirection;
    public Collider finishCollider;
    public Transform respawnPoint; // Titik respawn di depan garis finish
    private bool firstPass = true; // Mencegah trigger pertama langsung menambah lap

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 directionToPlayer = other.transform.position - transform.position;
            float dot = Vector3.Dot(finishDirection.forward, directionToPlayer.normalized);

            Debug.Log($"Dot Value: {dot} (1 = Benar, -1 = Salah)");

            if (dot > 0) // Masuk dari arah yang benar
            {
                Debug.Log("✅ Player melewati garis finish dengan benar!");

                if (lap.raceStarted)
                {
                    if (firstPass)
                    {
                        firstPass = false; // Abaikan trigger pertama
                    }
                    else
                    {
                        lap.NextLap();
                    }
                }
            }
            else // Masuk dari arah yang salah (Mundur)
            {
                Debug.Log("❌ Player mencoba melewati garis dari arah yang salah!");

                // Jadikan Collider Solid
                finishCollider.isTrigger = false;

                // Pindahkan pemain ke titik respawn
                other.transform.position = respawnPoint.position;
                other.transform.rotation = respawnPoint.rotation;

                // Reset velocity supaya tidak tetap bergerak mundur
                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Balikkan Collider ke Trigger setelah player keluar
            finishCollider.isTrigger = true;
        }
    }
    private void OnDrawGizmos()
    {
        if (finishDirection != null)
        {
            // Gambar garis ke arah finishDirection.forward
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + finishDirection.forward * 5f);

            // Gambar garis ke arah berlawanan
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position - finishDirection.forward * 5f);
        }
    }
}
