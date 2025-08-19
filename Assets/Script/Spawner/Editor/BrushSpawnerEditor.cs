using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BrushSpawner))]
public class BrushSpawnerEditor : Editor
{
    void OnSceneGUI()
    {
        BrushSpawner brush = (BrushSpawner)target;

        // Jangan aktifkan Scene input kalau brush dimatikan
        if (!brush.enableBrushGizmo)
            return;

        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (brush.treePrefab != null && brush.terrain != null)
                {
                    Undo.RegisterFullObjectHierarchyUndo(brush.gameObject, "Modify Trees");

                    if (e.shift)
                    {
                        brush.RemoveTreeNear(hit.point, brush.brushSize);
                    }
                    else
                    {
                        brush.SpawnTreeAt(hit.point);
                    }

                    e.Use();
                }
            }
        }

        HandleUtility.Repaint();
    }
}
