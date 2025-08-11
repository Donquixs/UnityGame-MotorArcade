using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArcadeBP
{
    public class BikeReferences : MonoBehaviour
    {
        public Transform SkidmarkF;
        public Transform SkidmarkB;
        public Transform WheelF;
        public Transform WheelB;
        public Transform Body;
        public Transform HandlePivot;

        [System.Serializable]
        public class AnimationPoints
        {
            public Transform HandIkTargets, LegIkTargets;
            public Transform rightHand, leftHand;
            public Transform rightFoot, leftFoot;
            public Transform RootPositionTarget, SpineTarget;
        }
        public AnimationPoints Animation_Points;

    }
}
