using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.Animations.Rigging;

namespace ArcadeBP
{
    public class BikerAnimationCreator : EditorWindow
    {
        GameObject character;
        GameObject ConfiguredBike;

        [MenuItem("Tools/Biker Animation Creator")]

        static void OpenWindow()
        {
            BikerAnimationCreator BikerAnimationCreatorWindow = (BikerAnimationCreator)GetWindow(typeof(BikerAnimationCreator));
            BikerAnimationCreatorWindow.minSize = new Vector2(400, 300);
            BikerAnimationCreatorWindow.Show();
        }

        private void OnGUI()
        {
            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = Color.green;

            GUILayout.Label("Biker Animation Creator", style);

            character = EditorGUILayout.ObjectField("Biker", character, typeof(GameObject), true) as GameObject;

            ConfiguredBike = EditorGUILayout.ObjectField("Configured Bike", ConfiguredBike, typeof(GameObject), true) as GameObject;

            if (GUILayout.Button("Configure Biker"))
            {
                configureBiker();
            }

        }


        private void configureBiker()
        {
            BikeReferences bikereferences = ConfiguredBike.GetComponent<BikeReferences>();
            character.transform.parent = bikereferences.Body;
            character.transform.localPosition = Vector3.zero;


            BikerAnimation bikerAnimation = character.GetComponent<BikerAnimation>();


            bikerAnimation.IKSettings.IKPoints.rightHand = bikereferences.Animation_Points.rightHand;
            bikerAnimation.IKSettings.IKPoints.leftHand = bikereferences.Animation_Points.leftHand;
            bikerAnimation.IKSettings.IKPoints.rightFoot = bikereferences.Animation_Points.rightFoot;
            bikerAnimation.IKSettings.IKPoints.leftFoot = bikereferences.Animation_Points.leftFoot;

            bikerAnimation.SplineTarget = bikereferences.Animation_Points.SpineTarget;
            bikerAnimation.RootPositionTarget = bikereferences.Animation_Points.RootPositionTarget;

        }
    }
}
