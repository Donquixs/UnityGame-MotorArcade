using UnityEngine;

public class TriggerPlayAnimation : MonoBehaviour
{
    public Animator targetAnimator;
    public string triggeringTag = "Player";
    public string animationStateName = "YourAnimationState"; // Ganti dengan nama state di Animator

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(triggeringTag))
        {
            if (targetAnimator != null)
            {
                //targetAnimator.ResetTrigger("Play"); // Opsional, untuk berjaga-jaga
                targetAnimator.SetTrigger("Play");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(triggeringTag))
        {
            if (targetAnimator != null)
            {
                // Reset ke awal animasi agar bisa dimainkan lagi saat masuk kembali
                //targetAnimator.Play(animationStateName, -1, 0f);
                targetAnimator.SetTrigger("Stop");
            }
        }
    }
}
