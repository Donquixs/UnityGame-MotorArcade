using UnityEngine;

public class RoadSignTrigger : MonoBehaviour
{
    public TurnDirection turnDirection; // Arah belok collider ini

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            RoadSignManager.Instance.ShowDirection(turnDirection);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            RoadSignManager.Instance.HideDirection();
        }
    }
}
