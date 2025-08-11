using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArcadeBP
{
    public class SkidMarks : MonoBehaviour
    {
        private TrailRenderer skidMark;
        private ParticleSystem smoke;
        public ArcadeBikeController bikeController;
        private void Awake()
        {
            smoke = GetComponent<ParticleSystem>();
            skidMark = GetComponent<TrailRenderer>();
            skidMark.emitting = false;
            skidMark.startWidth = bikeController.skidWidth;

        }


        private void OnEnable()
        {
            skidMark.enabled = true;
        }
        private void OnDisable()
        {
            skidMark.enabled = false;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (bikeController.grounded())
            {

                if (Mathf.Abs(bikeController.bikeVelocity.x) > 10)
                {
                    skidMark.emitting = true;
                }
                else
                {
                    skidMark.emitting = false;
                }
            }
            else
            {
                skidMark.emitting = false;
            }

            // smoke
            if (skidMark.emitting == true)
            {
                smoke.Play();
            }
            else { smoke.Stop(); }

        }
    }
}
