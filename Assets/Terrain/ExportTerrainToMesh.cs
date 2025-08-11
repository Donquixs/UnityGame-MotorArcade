using UnityEngine;
using System.IO;

public class ExportTerrainToMesh : MonoBehaviour
{
    public Terrain terrain;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Export();
        }
    }

    void Export()
    {
        if (terrain == null)
        {
            Debug.LogError("Terrain belum diset.");
            return;
        }

        TerrainData data = terrain.terrainData;
        int width = data.heightmapResolution;
        int height = data.heightmapResolution;
        float[,] heights = data.GetHeights(0, 0, width, height);

        Vector3 meshScale = data.size;
        meshScale = new Vector3(meshScale.x / (width - 1), meshScale.y, meshScale.z / (height - 1));

        Vector3[] vertices = new Vector3[width * height];
        Vector2[] uv = new Vector2[width * height];
        int[] triangles = new int[(width - 1) * (height - 1) * 6];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                float heightValue = heights[y, x];
                vertices[index] = Vector3.Scale(new Vector3(x, heightValue, y), meshScale);
                uv[index] = new Vector2((float)x / width, (float)y / height);
            }
        }

        int t = 0;
        for (int y = 0; y < height - 1; y++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                int i = y * width + x;
                triangles[t++] = i;
                triangles[t++] = i + width;
                triangles[t++] = i + width + 1;

                triangles[t++] = i;
                triangles[t++] = i + width + 1;
                triangles[t++] = i + 1;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "TerrainMesh";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();

        string filePath = Path.Combine(Application.dataPath, "ExportedTerrain.obj");
        SaveMeshToObj(mesh, filePath);
        Debug.Log("Terrain berhasil disimpan di: " + filePath);
    }

    void SaveMeshToObj(Mesh mesh, string filename)
    {
        using (StreamWriter sw = new StreamWriter(filename))
        {
            sw.WriteLine("o Terrain");

            foreach (Vector3 v in mesh.vertices)
                sw.WriteLine($"v {v.x} {v.y} {v.z}");

            foreach (Vector3 n in mesh.normals)
                sw.WriteLine($"vn {n.x} {n.y} {n.z}");

            foreach (Vector2 u in mesh.uv)
                sw.WriteLine($"vt {u.x} {u.y}");

            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                int a = mesh.triangles[i] + 1;
                int b = mesh.triangles[i + 1] + 1;
                int c = mesh.triangles[i + 2] + 1;
                sw.WriteLine($"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}");
            }
        }
    }
}
