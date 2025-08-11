using UnityEngine;

public class BushReplacer : MonoBehaviour
{
    [Header("Opsi Sembunyikan")]
    public bool hideBush = true;
    public bool hideUndakan = true;

    void Start()
    {
        if (hideBush)
        {
            HideBushObjects();
        }

        if (hideUndakan)
        {
            HideUndakanTaggedObjects();
        }
    }
    private void Update()
    {

        if (hideBush)
        {
            HideBushObjects();
        }

        if (hideUndakan)
        {
            HideUndakanTaggedObjects();
        }
    }
    void HideBushObjects()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith("BUSH"))
            {
                obj.SetActive(false);
            }
        }
    }

    void HideUndakanTaggedObjects()
    {
        GameObject[] undakanObjects = GameObject.FindGameObjectsWithTag("Undakan");
        foreach (GameObject obj in undakanObjects)
        {
            obj.SetActive(false);
        }
    }
}
