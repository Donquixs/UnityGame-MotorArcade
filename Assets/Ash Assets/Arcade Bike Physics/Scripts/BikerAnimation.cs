using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArcadeBP
{
    public class BikerAnimation : MonoBehaviour
    {
        public IKSetting IKSettings;
        protected Animator animator;

        public float rootLeanAmount = 0.1f;
        public Transform RootPositionTarget;

        public bool spineForwardLean = false;
        public float splineLeanAmount = 1f;
        public Transform SplineTarget;
        public Vector3 splineTargetOffset = new Vector3(0, 3, 3);
        private ArcadeBikeController bikeController;

        [System.Serializable]
        public class IKSetting
        {
            public IKPoints IKPoints;
            public float HandSeperation = 0.34f;
            public Vector3 HandRoll;
            public float LegSeperation = 0.24f;
            public Vector3 FootRoll;

        }
        [System.Serializable]
        public class IKPoints
        {
            public Transform rightHand, leftHand;
            public Transform rightFoot, leftFoot;
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            bikeController = transform.root.GetComponent<ArcadeBikeController>();
        }

        private void Update()
        {
            float spineLean = 0f;
            if (bikeController.bikeVelocity.z > 0 && spineForwardLean)
            {
                spineLean = Mathf.Clamp(2 / (1 + 6 * (bikeController.bikeVelocity.z / bikeController.MaxSpeed)), 0, 2);
            }
            else if(bikeController.bikeVelocity.z < 0)
            {
                spineLean = 2f;
            }
            else
            {
                spineLean = 0f;
            }

            SplineTarget.localPosition = splineTargetOffset + new Vector3(Input.GetAxis("Horizontal") * splineLeanAmount,
                spineLean,
                0);
            SplineTarget.localRotation = Quaternion.identity;

            RootPositionTarget.localPosition =
                new Vector3(Input.GetAxis("Horizontal") * rootLeanAmount, RootPositionTarget.localPosition.y, RootPositionTarget.localPosition.z);
        }

        void OnAnimatorIK()
        {
            //AvatarIKHint.RightElbow = 
            //set ik pos
            animator.SetIKPosition(AvatarIKGoal.RightHand, IKSettings.IKPoints.rightHand.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, IKSettings.IKPoints.rightHand.rotation);
            animator.SetIKHintPosition(AvatarIKHint.RightElbow, IKSettings.IKPoints.rightHand.position - Vector3.up * 1f);

            animator.SetIKPosition(AvatarIKGoal.LeftHand, IKSettings.IKPoints.leftHand.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, IKSettings.IKPoints.leftHand.rotation);
            animator.SetIKHintPosition(AvatarIKHint.LeftElbow, IKSettings.IKPoints.leftHand.position - Vector3.up * 1f);

            animator.SetIKPosition(AvatarIKGoal.RightFoot, IKSettings.IKPoints.rightFoot.position);
            animator.SetIKRotation(AvatarIKGoal.RightFoot, IKSettings.IKPoints.rightFoot.rotation);

            animator.SetIKPosition(AvatarIKGoal.LeftFoot, IKSettings.IKPoints.leftFoot.position);
            animator.SetIKRotation(AvatarIKGoal.LeftFoot, IKSettings.IKPoints.leftFoot.rotation);


            //set weight
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1.0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1.0f);

            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1.0f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1.0f);

            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1.0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1.0f);

            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1.0f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1.0f);

        }

        private void OnDrawGizmos()
        {

            if (IKSettings.IKPoints.rightHand)
            {
                IKSettings.IKPoints.rightHand.localPosition = new Vector3(IKSettings.HandSeperation, 0, 0);
                IKSettings.IKPoints.rightHand.localRotation = Quaternion.Euler(IKSettings.HandRoll.x * 10, IKSettings.HandRoll.y * 10, IKSettings.HandRoll.z * 10);
            }

            if (IKSettings.IKPoints.leftHand)
            {
                IKSettings.IKPoints.leftHand.localPosition = new Vector3(-IKSettings.HandSeperation, 0, 0);
                IKSettings.IKPoints.leftHand.localRotation = Quaternion.Euler(IKSettings.HandRoll.x * 10, -IKSettings.HandRoll.y * 10, -IKSettings.HandRoll.z * 10);
            }
            if (IKSettings.IKPoints.rightFoot)
            {
                IKSettings.IKPoints.rightFoot.localPosition = new Vector3(IKSettings.LegSeperation, 0, 0);
                IKSettings.IKPoints.rightFoot.localRotation = Quaternion.Euler(IKSettings.FootRoll.x * 10, IKSettings.FootRoll.y * 10, IKSettings.FootRoll.z * 10);
            }

            if (IKSettings.IKPoints.leftFoot)
            {
                IKSettings.IKPoints.leftFoot.localPosition = new Vector3(-IKSettings.LegSeperation, 0, 0);
                IKSettings.IKPoints.leftFoot.localRotation = Quaternion.Euler(IKSettings.FootRoll.x * 10, -IKSettings.FootRoll.y * 10, -IKSettings.FootRoll.z * 10);
            }


            Gizmos.color = Color.magenta;
            if (IKSettings.IKPoints.rightHand)
            {
                Gizmos.DrawWireSphere(IKSettings.IKPoints.rightHand.position, 0.05f);
            }

            if (IKSettings.IKPoints.leftHand)
            {
                Gizmos.DrawWireSphere(IKSettings.IKPoints.leftHand.position, 0.05f);
            }


            Gizmos.color = Color.red;
            if (IKSettings.IKPoints.rightFoot)
            {
                Gizmos.DrawWireSphere(IKSettings.IKPoints.rightFoot.position, 0.05f);
            }

            if (IKSettings.IKPoints.leftFoot)
            {
                Gizmos.DrawWireSphere(IKSettings.IKPoints.leftFoot.position, 0.05f);
            }


            if (!Application.isPlaying && SplineTarget)
            {
                SplineTarget.localPosition = splineTargetOffset;
            }


            Gizmos.color = Color.cyan;
            if (SplineTarget)
            {
                Gizmos.DrawSphere(SplineTarget.position, 0.2f);
                Gizmos.DrawLine(SplineTarget.position, transform.position);
            }

        }
    }
}
