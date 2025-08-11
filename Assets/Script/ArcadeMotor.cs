using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeBP
{
    public class ArcadeMotor : MonoBehaviour
    {
        public enum groundCheck { rayCast, sphereCaste };
        public enum MovementMode { Velocity, AngularVelocity };
        public MovementMode movementMode;
        public groundCheck GroundCheck;
        public LayerMask drivableSurface;

        public float MaxSpeed, acceleration, turn;
        public Rigidbody rb, bikeBody;

        [Header("Gravity Settings")]
        public float customGravity = 9.81f; // Default gravitasi

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


        public bool allowBraking = true; // Default boleh ngerem

        public bool allowOnlyAccelerate = false;


        public float skidWidth;

        private float radius, horizontalInput, verticalInput;
        private Vector3 origin;
        private PlayerInput playerInput;
        private InputAction moveAction, accelerateAction, brakeAction, reverseAction, MoveKeyboard;

        private float brakeHoldTime = 0f;
        private float reverseTriggerDuration = 2f; // Durasi menahan brake
        private bool hasTriggeredReverse = false;
        private Vector3 lastPosition;
        private float stuckThreshold = 0.1f; // Batas untuk menganggap 'tidak bergerak'


        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            moveAction = playerInput.actions["Move"];
            accelerateAction = playerInput.actions["Accelerate"];
            brakeAction = playerInput.actions["Brake"];
            reverseAction = playerInput.actions["Reverse"]; // Tambahkan Reverse
            MoveKeyboard = playerInput.actions["MoveKeyboard"];
        }

        private void Start()
        {
            radius = rb.GetComponent<SphereCollider>().radius;
            if (movementMode == MovementMode.AngularVelocity)
            {
                Physics.defaultMaxAngularSpeed = 150;
            }
            rb.centerOfMass = Vector3.zero;
            lastPosition = rb.position;

        }

        private void Update()
        {
            float keyboardInput = MoveKeyboard.ReadValue<float>();
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            // Pilih input horizontal yang aktif
            if (Mathf.Abs(moveInput.x) > 0.1f) // stick aktif?
            {
                horizontalInput = moveInput.x;
            }
            else
            {
                horizontalInput = keyboardInput;
            }

            // ✅ Hanya baca input gas jika allowOnlyAccelerate aktif
            if (allowOnlyAccelerate)
            {
                verticalInput = accelerateAction.ReadValue<float>();
            }
            else
            {
                verticalInput = accelerateAction.ReadValue<float>();

                // ✅ Reverse hanya bisa jika allowBraking true
                if (reverseAction.WasPressedThisFrame())
                {
                    ReversePosition();
                }
            }
            // Cek apakah brake sedang ditekan
            if (brakeAction.ReadValue<float>() > 0.1f)
            {
                float distanceMoved = Vector3.Distance(rb.position, lastPosition);

                // Jika nyaris tidak bergerak, tambahkan waktu tahan
                if (distanceMoved < stuckThreshold)
                {
                    brakeHoldTime += Time.deltaTime;

                    // Kalau sudah lebih dari 3 detik dan belum pernah trigger
                    if (brakeHoldTime >= reverseTriggerDuration && !hasTriggeredReverse)
                    {
                        StartCoroutine(SmoothReverse(10f, 1.5f));
                        hasTriggeredReverse = true;
                    }
                }
                else
                {
                    // Bergerak, reset waktu
                    brakeHoldTime = 0f;
                    hasTriggeredReverse = false;
                }

                lastPosition = rb.position;
            }
            else
            {
                // Brake dilepas, reset timer
                brakeHoldTime = 0f;
                hasTriggeredReverse = false;
            }

            Visuals();
            AudioManager();
        }

        public void AudioManager()
        {
            engineSound.pitch = Mathf.Lerp(minPitch, MaxPitch, Mathf.Abs(bikeVelocity.z) / MaxSpeed);
            SkidSound.mute = !(Mathf.Abs(bikeVelocity.x) > 10 && grounded());
        }

        void FixedUpdate()
        {
            bikeVelocity = bikeBody.transform.InverseTransformDirection(bikeBody.velocity);

            if (Mathf.Abs(bikeVelocity.x) > 0)
            {
                frictionMaterial.dynamicFriction = frictionCurve.Evaluate(Mathf.Abs(bikeVelocity.x / 100));
            }

            if (grounded())
            {
                float sign = Mathf.Sign(bikeVelocity.z);
                float TurnMultiplyer = turnCurve.Evaluate(bikeVelocity.magnitude / MaxSpeed);
                bikeBody.AddTorque(Vector3.up * horizontalInput * sign * turn * 10 * TurnMultiplyer);

                // Hanya aktifkan rem kalau allowOnlyAccelerate = false
                bool isBraking = !allowOnlyAccelerate && brakeAction.ReadValue<float>() > 0.1f;
                bool isAccelerating = verticalInput > 0.1f;


                // Kunci rotasi saat ngerem
                rb.constraints = isBraking ? RigidbodyConstraints.FreezeRotationX : RigidbodyConstraints.None;

                if (movementMode == MovementMode.AngularVelocity)
                {
                    if (isAccelerating)
                    {
                        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, bikeBody.transform.right * verticalInput * MaxSpeed / radius, acceleration * Time.deltaTime);
                    }
                    else if (isBraking && !isAccelerating)
                    {
                        // MUNDUR saat ngerem tapi ga ngegas
                        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, -bikeBody.transform.right * MaxSpeed / radius, acceleration * Time.deltaTime);
                    }
                }
                else if (movementMode == MovementMode.Velocity)
                {
                    if (isAccelerating && !isBraking)
                    {
                        rb.velocity = Vector3.Lerp(rb.velocity, bikeBody.transform.forward * verticalInput * MaxSpeed, acceleration / 10 * Time.deltaTime);
                    }
                    else if (isBraking && !isAccelerating)
                    {
                        // MUNDUR saat ngerem tapi ga ngegas
                        rb.velocity = Vector3.Lerp(rb.velocity, -bikeBody.transform.forward * MaxSpeed * 0.5f, acceleration / 10 * Time.deltaTime);
                    }
                }

                // Perataan ke permukaan
                bikeBody.MoveRotation(Quaternion.Slerp(bikeBody.rotation, Quaternion.FromToRotation(bikeBody.transform.up, hit.normal) * bikeBody.transform.rotation, 0.09f));
            }
            else
            {
                bikeBody.AddForce(Vector3.down * customGravity, ForceMode.Acceleration);
                bikeBody.MoveRotation(Quaternion.Slerp(bikeBody.rotation, Quaternion.FromToRotation(bikeBody.transform.up, Vector3.up) * bikeBody.transform.rotation, 0.02f));
            }
        }


        public void ReversePosition()
        {
            if (grounded()) // Hanya bisa mundur jika di darat
            {
                StartCoroutine(SmoothReverse(5f, 0.5f)); // Mundur sejauh 5m dalam 0.5 detik
            }

        }

        private IEnumerator SmoothReverse(float distance, float duration)
        {
            Vector3 start = rb.position;
            Vector3 target = start - bikeBody.transform.forward * distance; // Hitung target mundur
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                rb.MovePosition(Vector3.Lerp(start, target, elapsedTime / duration)); // Lerp untuk pergerakan halus
                elapsedTime += Time.deltaTime;
                yield return null; // Tunggu frame berikutnya
            }

            rb.MovePosition(target); // Pastikan posisi akhir tepat
            rb.velocity = Vector3.zero; // Hentikan gerakan setelah mundur
        }


        public void Visuals()
        {
            Handle.localRotation = Quaternion.Slerp(Handle.localRotation, Quaternion.Euler(Handle.localRotation.eulerAngles.x,
                                   20 * horizontalInput, Handle.localRotation.eulerAngles.z), 15f * Time.deltaTime);

            Wheels[0].localRotation = rb.transform.localRotation;
            Wheels[1].localRotation = rb.transform.localRotation;

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

        public bool grounded()
        {
            origin = rb.position + rb.GetComponent<SphereCollider>().radius * Vector3.up;
            var direction = -transform.up;
            var maxdistance = rb.GetComponent<SphereCollider>().radius + 0.2f;

            if (GroundCheck == groundCheck.rayCast)
            {
                return Physics.Raycast(rb.position, Vector3.down, out hit, maxdistance, drivableSurface);
            }
            else if (GroundCheck == groundCheck.sphereCaste)
            {
                return Physics.SphereCast(origin, radius + 0.1f, direction, out hit, maxdistance, drivableSurface);
            }
            return false;
        }


    }
}
