using UnityEngine;
using UnityEditor;
using System;

namespace ArcadeBP
{
    public class ArcadeBikeCreator : EditorWindow
    {

        GameObject preset;
        Transform VehicleBody;
        Transform Handle;
        Transform wheelFront;
        Transform wheelBack;

        MeshRenderer bodyMesh;
        MeshRenderer wheelMesh;

        private GameObject NewVehicle;


        [MenuItem("Tools/Arcade Bike Physics")]

        static void OpenWindow()
        {
            ArcadeBikeCreator vehicleCreatorWindow = (ArcadeBikeCreator)GetWindow(typeof(ArcadeBikeCreator));
            vehicleCreatorWindow.minSize = new Vector2(400, 300);
            vehicleCreatorWindow.Show();
        }

        private void OnGUI()
        {
            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = Color.green;
            GUILayout.Label("Arcade Bike Creator", style);
            preset = EditorGUILayout.ObjectField("Bike preset", preset, typeof(GameObject), true) as GameObject;
            GUILayout.Label("Your Vehicle", style);
            VehicleBody = EditorGUILayout.ObjectField("Bike Body", VehicleBody, typeof(Transform), true) as Transform;
            Handle = EditorGUILayout.ObjectField("Handle", Handle, typeof(Transform), true) as Transform;
            wheelFront = EditorGUILayout.ObjectField("wheel Front", wheelFront, typeof(Transform), true) as Transform;
            wheelBack = EditorGUILayout.ObjectField("wheel Back", wheelBack, typeof(Transform), true) as Transform;

            bodyMesh = EditorGUILayout.ObjectField("Body Mesh", bodyMesh, typeof(MeshRenderer), true) as MeshRenderer;
            wheelMesh = EditorGUILayout.ObjectField("Wheel Mesh", wheelMesh, typeof(MeshRenderer), true) as MeshRenderer;

            if (GUILayout.Button("Create Bike"))
            {
                createBike();
            }

        }

        private void adjustColliders()
        {
            if (NewVehicle.GetComponent<BoxCollider>())
            {
                NewVehicle.GetComponent<BoxCollider>().center = Vector3.zero;
                NewVehicle.GetComponent<BoxCollider>().size = bodyMesh.bounds.size;
            }

            if (NewVehicle.GetComponent<CapsuleCollider>())
            {
                NewVehicle.GetComponent<CapsuleCollider>().center = Vector3.zero;
                NewVehicle.GetComponent<CapsuleCollider>().height = bodyMesh.bounds.size.z;
                NewVehicle.GetComponent<CapsuleCollider>().radius = bodyMesh.bounds.size.x / 2;

            }

            Vector3 SpheareRBOffset = new Vector3(NewVehicle.transform.position.x,
                                                  wheelFront.position.y + bodyMesh.bounds.extents.x - wheelMesh.bounds.size.y / 2,
                                                  NewVehicle.transform.position.z - ((NewVehicle.transform.position.z - wheelBack.position.z) / 2));

            NewVehicle.GetComponent<ArcadeBikeController>().skidWidth = wheelMesh.bounds.size.x / 2;
            if (NewVehicle.transform.Find("SphereRB"))
            {
                NewVehicle.transform.Find("SphereRB").GetComponent<SphereCollider>().radius = bodyMesh.bounds.extents.x;
                NewVehicle.transform.Find("SphereRB").position = SpheareRBOffset;
            }

        }

        private void createBike()
        {
            NewVehicle = Instantiate(preset, bodyMesh.bounds.center, VehicleBody.rotation);
            var references = NewVehicle.GetComponent<BikeReferences>();


            if (references.WheelF)
            {
                GameObject.DestroyImmediate(references.WheelF.Find("WheelF Axel").GetChild(0).gameObject);
            }
            if (references.WheelB)
            {
                GameObject.DestroyImmediate(references.WheelB.Find("WheelB Axel").GetChild(0).gameObject);
            }

            references.Body.position = new Vector3(references.Body.position.x, wheelFront.position.y - wheelMesh.bounds.extents.y, references.Body.position.z);
            VehicleBody.parent = references.Body;

            //VehicleBody.localPosition = Vector3.zero;

            if (references.WheelF)
            {
                references.WheelF.position = wheelFront.position;
                references.WheelF.parent = wheelFront.parent;
                wheelFront.parent = references.WheelF.Find("WheelF Axel");
            }
            if (references.WheelB)
            {
                references.WheelB.position = wheelBack.position;
                references.WheelB.parent = wheelBack.parent;
                wheelBack.parent = references.WheelB.Find("WheelB Axel");
            }


            references.SkidmarkF.position = wheelFront.position - Vector3.up * (wheelMesh.bounds.size.y / 2 - 0.02f);
            references.SkidmarkB.position = wheelBack.position - Vector3.up * (wheelMesh.bounds.size.y / 2 - 0.02f);
            references.HandlePivot.parent = Handle;
            references.HandlePivot.localPosition = Vector3.zero;
            references.HandlePivot.localRotation = Quaternion.identity;
            references.HandlePivot.parent = Handle.parent;

            Handle.parent = references.HandlePivot;
            references.Animation_Points.HandIkTargets.parent = Handle;
            GameObject.DestroyImmediate(references.HandlePivot.GetChild(0).gameObject);
            Handle.SetSiblingIndex(0);
            NewVehicle.GetComponent<ArcadeBikeController>().Handle = Handle;
            GameObject.DestroyImmediate(references.Body.GetChild(0).gameObject);
            VehicleBody.SetSiblingIndex(0);
            adjustColliders();
        }
    }
}
