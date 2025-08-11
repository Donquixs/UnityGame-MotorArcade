using UnityEngine;

public class AddMeshColliderToChildren : MonoBehaviour
{
    void Start()
    {
        // Ambil semua transform anak (termasuk diri sendiri)
        Transform[] allChildren = GetComponentsInChildren<Transform>();

        foreach (Transform child in allChildren)
        {
            // Cek apakah punya MeshFilter (perlu untuk MeshCollider)
            MeshFilter meshFilter = child.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                // Tambahkan MeshCollider jika belum ada
                if (child.GetComponent<MeshCollider>() == null)
                {
                    MeshCollider meshCollider = child.gameObject.AddComponent<MeshCollider>();
                    meshCollider.convex = false; // atau true jika diperlukan
                }
            }
        }

        Debug.Log("MeshCollider ditambahkan ke semua child yang memiliki MeshFilter.");
    }
}
