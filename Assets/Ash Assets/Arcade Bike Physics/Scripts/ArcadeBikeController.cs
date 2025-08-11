using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArcadeBP
{
    public class ArcadeBikeController : MonoBehaviour
    {
        public enum groundCheck { rayCast, sphereCaste };
        public enum MovementMode { Velocity, AngularVelocity };
        public MovementMode movementMode;
        public groundCheck GroundCheck;
        public LayerMask drivableSurface;

        public float MaxSpeed, accelaration, turn;
        public Rigidbody rb, bikeBody;

        [HideInInspector]
        public RaycastHit hit;
        public AnimationCurve frictionCurve;
        public AnimationCurve turnCurve;
        public AnimationCurve leanCurve;
        public PhysicMaterial frictionMaterial;
        [Header("Visuals")]
        public Transform BodyMesh;
        public Transform Handle;
        public Transform[] Wheels = new Transform[2];
        [HideInInspector]
        public Vector3 bikeVelocity;

        [Range(-70, 70)]
        public float BodyTilt;
        [Header("Audio settings")]
        public AudioSource engineSound;
        [Range(0, 1)]
        public float minPitch;
        [Range(1, 5)]
        public float MaxPitch;
        public AudioSource SkidSound;

        public float skidWidth;


        private float radius, horizontalInput, verticalInput;
        private Vector3 origin;

        private void Start()
        {
            radius = rb.GetComponent<SphereCollider>().radius;
            if (movementMode == MovementMode.AngularVelocity)
            {
                Physics.defaultMaxAngularSpeed = 150;
            }
            rb.centerOfMass = Vector3.zero;
        }
        private void Update()
        {
            horizontalInput = Input.GetAxis("Horizontal"); //turning input
            verticalInput = Input.GetAxis("Vertical");     //accelaration input
            Visuals();
            AudioManager();

        }
        public void AudioManager()
        {
            engineSound.pitch = Mathf.Lerp(minPitch, MaxPitch, Mathf.Abs(bikeVelocity.z) / MaxSpeed);
            if (Mathf.Abs(bikeVelocity.x) > 10 && grounded())
            {
                SkidSound.mute = false;
            }
            else
            {
                SkidSound.mute = true;
            }
        }


        void FixedUpdate()
        {
            bikeVelocity = bikeBody.transform.InverseTransformDirection(bikeBody.velocity);

            if (Mathf.Abs(bikeVelocity.x) > 0)
            {
                //changes friction according to sideways speed of bike
                frictionMaterial.dynamicFriction = frictionCurve.Evaluate(Mathf.Abs(bikeVelocity.x / 100));
            }


            if (grounded())
            {
                //turnlogic
                float sign = Mathf.Sign(bikeVelocity.z);
                float TurnMultiplyer = turnCurve.Evaluate(bikeVelocity.magnitude / MaxSpeed);
                if (verticalInput > 0.1f || bikeVelocity.z > 1)
                {
                    bikeBody.AddTorque(Vector3.up * horizontalInput * sign * turn * 10 * TurnMultiplyer);
                }
                else if (verticalInput < -0.1f || bikeVelocity.z < -1)
                {
                    bikeBody.AddTorque(Vector3.up * horizontalInput * sign * turn * 10 * TurnMultiplyer);
                }

                //brakelogic
                if (Input.GetAxis("Jump") > 0.1f)
                {
                    rb.constraints = RigidbodyConstraints.FreezeRotationX;
                }
                else
                {
                    rb.constraints = RigidbodyConstraints.None;
                }

                //accelaration logic

                if (movementMode == MovementMode.AngularVelocity)
                {
                    if (Mathf.Abs(verticalInput) > 0.1f)
                    {
                        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, bikeBody.transform.right * verticalInput * MaxSpeed / radius, accelaration * Time.deltaTime);
                    }
                }
                else if (movementMode == MovementMode.Velocity)
                {
                    if (Mathf.Abs(verticalInput) > 0.1f && Input.GetAxis("Jump") < 0.1f)
                    {
                        rb.velocity = Vector3.Lerp(rb.velocity, bikeBody.transform.forward * verticalInput * MaxSpeed, accelaration / 10 * Time.deltaTime);
                    }
                }

                //body tilt
                bikeBody.MoveRotation(Quaternion.Slerp(bikeBody.rotation, Quaternion.FromToRotation(bikeBody.transform.up, hit.normal) * bikeBody.transform.rotation, 0.09f));
            }
            else
            {
                bikeBody.MoveRotation(Quaternion.Slerp(bikeBody.rotation, Quaternion.FromToRotation(bikeBody.transform.up, Vector3.up) * bikeBody.transform.rotation, 0.02f));
            }

        }
        public void Visuals()
        {
            Handle.localRotation = Quaternion.Slerp(Handle.localRotation, Quaternion.Euler(Handle.localRotation.eulerAngles.x,
                                   20 * horizontalInput, Handle.localRotation.eulerAngles.z), 15f * Time.deltaTime);

            Wheels[0].localRotation = rb.transform.localRotation;
            Wheels[1].localRotation = rb.transform.localRotation;

            //Body
            if (bikeVelocity.z > 1)
            {
                BodyMesh.localRotation = Quaternion.Slerp(BodyMesh.localRotation, Quaternion.Euler(0,
                                   BodyMesh.localRotation.eulerAngles.y, BodyTilt * horizontalInput * leanCurve.Evaluate(bikeVelocity.z / MaxSpeed)), 4f * Time.deltaTime);
            }
            else
            {
                BodyMesh.localRotation = Quaternion.Slerp(BodyMesh.localRotation, Quaternion.Euler(0, 0, 0), 4f * Time.deltaTime);
            }



        }

        public bool grounded() //checks for if vehicle is grounded or not
        {
            origin = rb.position + rb.GetComponent<SphereCollider>().radius * Vector3.up;
            var direction = -transform.up;
            var maxdistance = rb.GetComponent<SphereCollider>().radius + 0.2f;

            if (GroundCheck == groundCheck.rayCast)
            {
                if (Physics.Raycast(rb.position, Vector3.down, out hit, maxdistance, drivableSurface))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            else if (GroundCheck == groundCheck.sphereCaste)
            {
                if (Physics.SphereCast(origin, radius + 0.1f, direction, out hit, maxdistance, drivableSurface))
                {
                    return true;

                }
                else
                {
                    return false;
                }
            }
            else { return false; }
        }

        private void OnDrawGizmos()
        {
            //debug gizmos
            radius = rb.GetComponent<SphereCollider>().radius;
            float width = 0.02f;
            if (!Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(rb.transform.position + ((radius + width) * Vector3.down), new Vector3(2 * radius, 2 * width, 4 * radius));
                if (GetComponent<BoxCollider>())
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider>().size);
                }
                if (GetComponent<CapsuleCollider>())
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(transform.position, GetComponent<CapsuleCollider>().bounds.size);
                }

            }

        }

    }
}
