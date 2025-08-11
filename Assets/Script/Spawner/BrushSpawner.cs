using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class BrushSpawner : MonoBehaviour
{
    [Header("Main Settings")]
    public GameObject treePrefab;
    public Terrain terrain;
    public float brushSize = 5f;
    public bool enableBrushGizmo = true;

    [Header("Rotation Settings")]
    public bool randomRotationY = true;
    [Range(0f, 360f)] public float fixedRotationY = 0f;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!enableBrushGizmo || terrain == null) return;

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(hit.point, brushSize);
        }
    }
#endif

    public void SpawnTreeAt(Vector3 worldPosition)
    {
        Vector3 terrainPos = terrain.GetPosition();
        Vector3 localPos = worldPosition - terrainPos;

        float height = terrain.SampleHeight(worldPosition);
        Vector3 spawnPos = new Vector3(worldPosition.x, height + terrainPos.y, worldPosition.z);

        GameObject tree = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(treePrefab);
        tree.transform.position = spawnPos;

        // Opsional: mengikuti kontur permukaan terrain
        Vector3 normal = terrain.terrainData.GetInterpolatedNormal(
            localPos.x / terrain.terrainData.size.x,
            localPos.z / terrain.terrainData.size.z
        );
        tree.transform.up = normal;

        // Rotasi Y
        float yRotation = randomRotationY ? Random.Range(0f, 360f) : fixedRotationY;
        tree.transform.Rotate(0f, yRotation, 0f, Space.Self);

        tree.transform.SetParent(this.transform);
    }

    public void RemoveTreeNear(Vector3 worldPosition, float radius)
    {
        Transform toDelete = null;
        float closestDistance = float.MaxValue;

        foreach (Transform child in transform)
        {
            float distance = Vector3.Distance(worldPosition, child.position);
            if (distance < radius && distance < closestDistance)
            {
                toDelete = child;
                closestDistance = distance;
            }
        }

        if (toDelete != null)
        {
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(toDelete.gameObject);
#else
            DestroyImmediate(toDelete.gameObject);
#endif
        }
    }
}
