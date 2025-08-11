using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BikeController : MonoBehaviour
{
    RaycastHit hit;
    float moveInput, steerInput,rayLenght,currentVelocityOffset;
    [HideInInspector] public Vector3 velocity;

    public GameObject Handle;
    public float maxSpeed, acceleration,steerStrenght,gravity,bikeXTiltIncrement = 0.9f
        ,zTiltAngle = 45f, handleRotVal =30f,handleRotSpeed = .15f,minSkidVelocity =10f;
    [Range(1f, 10f)]
    public float brakingFactor;

    public LayerMask driveableSurface;

    public AudioSource engineSound;
    public AudioSource SkidSound;
    [Range(0, 1)] public float minPitch;
    [Range(0, 5)] public float maxPitch;
    public Rigidbody sphereRB,BikeBody;
    public bool isGrounded;
    // Start is called before the first frame update
    void Start()
    {
       sphereRB.transform.parent = null;
        BikeBody.transform.parent = null ;

        rayLenght = sphereRB.GetComponent<SphereCollider>().radius + 0.2f;

        SkidSound.mute = true;
    }

    // Update is called once per frame
    void Update()
    {
        moveInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");

        transform.position = sphereRB.transform.position;
        
        velocity = BikeBody.transform.InverseTransformDirection(BikeBody.velocity);
        currentVelocityOffset = velocity.z/maxSpeed;
    }

    private void FixedUpdate()
    {
        Movement();
        EngineSound();
        SkidMarks();
    }

    void Movement()
    {
        if(Grounded())
        {
            if(!Input.GetKey(KeyCode.Space))
            {
                Acceleration();
               
            }
            Rotation();
            Brake();
        }
        else
        {
            Gravity();
        }

        BikeTilt();
    }

    void Acceleration()
    {
        sphereRB.velocity = Vector3.Lerp(sphereRB.velocity,maxSpeed*moveInput* transform.forward, Time.fixedDeltaTime * acceleration);
    }

    void Rotation()
    {
        transform.Rotate(0, steerInput * currentVelocityOffset * steerStrenght * Time.fixedDeltaTime, 0, Space.World);

        Handle.transform.localRotation = Quaternion.Slerp(Handle.transform.localRotation, 
            Quaternion.Euler(Handle.transform.localRotation.eulerAngles.x, handleRotVal * steerInput, Handle.transform.localRotation.eulerAngles.z), handleRotSpeed);
    }

    void BikeTilt()
    {
        float xRot = (Quaternion.FromToRotation(BikeBody.transform.up,hit.normal)*BikeBody.transform.rotation).eulerAngles.x;
        float zRot = 0;

        if(currentVelocityOffset >0)
        {
            zRot = -zTiltAngle * steerInput * currentVelocityOffset;
        }
       

        Quaternion targetRot = Quaternion.Slerp(BikeBody.transform.rotation, Quaternion.Euler(xRot, transform.eulerAngles.y, zRot),bikeXTiltIncrement);

        Quaternion newRotation = Quaternion.Euler(targetRot.eulerAngles.x,transform.eulerAngles.y,targetRot.eulerAngles.z);

        BikeBody.MoveRotation(newRotation);
    }
    void Brake()
    {
        if(Input.GetKey(KeyCode.Space))
        {
            sphereRB.velocity *=brakingFactor/10;
        }
    }

    bool Grounded()
    {
        float radius = rayLenght - 0.02f;
        Vector3 origin = sphereRB.transform.position + radius * Vector3.up;

        if(Physics.SphereCast(origin,radius +0.02f,-transform.up,out hit,rayLenght,driveableSurface))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void Gravity()
    {
        sphereRB.AddForce(gravity*Vector3.down,ForceMode.Acceleration);
    }

    void SkidMarks()
    {
        if(Grounded()&& Mathf.Abs(velocity.x) > minSkidVelocity)
        {
            SkidSound.mute = false;
        }
        else
        {
            SkidSound.mute = true;
        }
    }
    void EngineSound()
    {
        engineSound.pitch = Mathf.Lerp(minPitch,maxPitch,Mathf.Abs(currentVelocityOffset));
    }
}
