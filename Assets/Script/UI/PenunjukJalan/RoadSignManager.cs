using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoadSignManager : MonoBehaviour
{
    public static RoadSignManager Instance;

    [Header("UI Direction Settings")]
    [Tooltip("UI Image untuk arah belok/nanjak.")]
    public GameObject directionUIImage;

    [Tooltip("Urutan sprite: 0 = Kiri, 1 = Kanan, 2 = Nanjak, 3 = Berkelok")]
    public Sprite[] directionSprites;

    [Tooltip("Sprite untuk wrong way / putar balik.")]
    public Sprite wrongWaySprite;

    [Header("Wrong Way Detection")]
    [Tooltip("Daftar checkpoint yang membentuk arah lintasan.")]
    public List<Transform> trackForwardReference;

    [Tooltip("Transform dari player.")]
    public Transform playerTransform;

    [Tooltip("Waktu antar pengecekan wrong way.")]
    public float wrongWayCheckInterval = 0.5f;

    [Tooltip("Ambang batas arah salah, nilai Dot < threshold berarti salah.")]
    public float wrongWayThreshold = 0.3f;

    [Tooltip("Berapa lama arah salah harus terdeteksi agar dianggap valid.")]
    public float wrongWayDurationThreshold = 2f;

    [Header("Debug Settings")]
    [Tooltip("Aktifkan untuk melihat log debug.")]
    public bool debugMode = false;

    private int currentCheckpointIndex = 0;
    private float wrongWayTimer = 0f;
    private bool isWrongWay = false;
    private Coroutine blinkCoroutine;
    private Vector3 lastPlayerPosition;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GameObject[] checkpoints = GameObject.FindGameObjectsWithTag("Checkpoint");

        System.Array.Sort(checkpoints, (a, b) => ExtractNumber(a.name).CompareTo(ExtractNumber(b.name)));

        trackForwardReference = new List<Transform>();
        foreach (var cp in checkpoints)
        {
            trackForwardReference.Add(cp.transform);
        }

        if (debugMode)
            Debug.Log($"[RoadSignManager] Checkpoints found: {trackForwardReference.Count}");

        HideDirection();

        if (playerTransform != null && trackForwardReference.Count > 1)
        {
            StartCoroutine(CheckWrongWayRoutine());
        }

        lastPlayerPosition = playerTransform.position;
    }

    private int ExtractNumber(string name)
    {
        System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(name, @"\d+");
        if (match.Success)
        {
            return int.Parse(match.Value);
        }
        return 0; // Untuk nama seperti "Sphere" tanpa angka
    }

    public void ShowDirection(TurnDirection direction)
    {
        if (isWrongWay) return;

        int index = (int)direction;
        if (index >= 0 && index < directionSprites.Length)
        {
            directionUIImage.GetComponent<Image>().sprite = directionSprites[index];
            directionUIImage.GetComponent<Image>().enabled = true;

            if (debugMode)
                Debug.Log($"[RoadSignManager] ShowDirection: {direction}");
        }
    }

    public void HideDirection()
    {
        if (!isWrongWay)
        {
            directionUIImage.GetComponent<Image>().sprite = null;
            directionUIImage.GetComponent<Image>().enabled = false;

            if (debugMode)
                Debug.Log("[RoadSignManager] HideDirection");
        }
    }

    private IEnumerator CheckWrongWayRoutine()
    {
        yield return new WaitForSeconds(1.5f); // kasih waktu untuk start dulu

        while (true)
        {
            yield return new WaitForSeconds(wrongWayCheckInterval);

            int closestIndex = FindClosestCheckpointIndex(playerTransform.position);
            int nextIndex = (closestIndex + 1) % trackForwardReference.Count;

            Vector3 currentPos = trackForwardReference[closestIndex].position;
            Vector3 nextPos = trackForwardReference[nextIndex].position;

            float distToNext = Vector3.Distance(playerTransform.position, nextPos);

            // Update checkpoint jika cukup dekat
            if (distToNext < 3f && currentCheckpointIndex != nextIndex)
            {
                currentCheckpointIndex = nextIndex;
                wrongWayTimer = 0f;
                SetWrongWay(false);

                if (debugMode)
                    Debug.Log($"[RoadSignManager] Player mendekati checkpoint {currentCheckpointIndex} (benar)");
            }
            else
            {
                Vector3 moveDir = Vector3.ProjectOnPlane(playerTransform.position - lastPlayerPosition, Vector3.up).normalized;
                Vector3 trackDir = Vector3.ProjectOnPlane(nextPos - currentPos, Vector3.up).normalized;

                // Skip kalau gerakan sangat kecil (idle)
                if (moveDir.magnitude < 0.01f)
                {
                    if (debugMode) Debug.Log("[RoadSignManager] Gerakan terlalu kecil, skip deteksi arah.");
                    continue;
                }

                float dotTrack = Vector3.Dot(moveDir, trackDir); // arah gerak relatif terhadap lintasan
                float dotForward = Vector3.Dot(playerTransform.forward, trackDir); // arah hadap relatif terhadap lintasan
                bool isMovingBackward = Vector3.Dot(moveDir, playerTransform.forward) < -0.3f;

                if (debugMode)
                {
                    Debug.Log($"[RoadSignManager] Dot Track: {dotTrack:F2}, Dot Forward: {dotForward:F2}, Mundur: {isMovingBackward}");
                    Debug.DrawRay(playerTransform.position, moveDir * 3f, Color.green, 1f);
                    Debug.DrawRay(playerTransform.position, trackDir * 3f, Color.red, 1f);
                }

                // Salah arah hanya jika:
                // - Tidak sedang mundur
                // - Gerak tidak searah dengan lintasan
                // - Dan sudah cukup lama
                if (!isMovingBackward && dotTrack < wrongWayThreshold && dotForward < 0.3f)
                {
                    wrongWayTimer += wrongWayCheckInterval;

                    if (debugMode)
                        Debug.Log($"[RoadSignManager] Deteksi salah arah... Timer: {wrongWayTimer}");

                    if (wrongWayTimer >= wrongWayDurationThreshold)
                    {
                        SetWrongWay(true);

                        if (debugMode)
                            Debug.Log("[RoadSignManager] Salah arah dikonfirmasi.");
                    }
                }
                else
                {
                    if (isWrongWay)
                    {
                        if (debugMode) Debug.Log("[RoadSignManager] Arah dibenarkan, reset wrong way.");
                        SetWrongWay(false);
                    }

                    wrongWayTimer = 0f;
                }

                lastPlayerPosition = playerTransform.position;
            }
        }
    }


    private int FindClosestCheckpointIndex(Vector3 position)
    {
        int closestIndex = 0;
        float minDistance = float.MaxValue;

        for (int i = 0; i < trackForwardReference.Count; i++)
        {
            float distance = Vector3.Distance(position, trackForwardReference[i].position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }


    private void SetWrongWay(bool state)
    {
        if (isWrongWay == state) return;

        isWrongWay = state;

        if (isWrongWay)
        {
            HideDirection();
            if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
            blinkCoroutine = StartCoroutine(BlinkWrongWayUI());

            if (debugMode)
                Debug.Log("[RoadSignManager] Menampilkan UI wrong way");
        }
        else
        {
            if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
            directionUIImage.GetComponent<Image>().sprite = null;
            directionUIImage.GetComponent<Image>().enabled = false;

            if (debugMode)
                Debug.Log("[RoadSignManager] Wrong way dibatalkan");
        }
    }

    private IEnumerator BlinkWrongWayUI()
    {
        while (true)
        {
            directionUIImage.GetComponent<Image>().sprite = wrongWaySprite;
            directionUIImage.GetComponent<Image>().enabled = true;
            yield return new WaitForSeconds(0.5f);
            directionUIImage.GetComponent<Image>().enabled = false;
            yield return new WaitForSeconds(0.5f);
        }
    }
}
