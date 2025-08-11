using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Speedometer : MonoBehaviour
{
    public Rigidbody target;
    public ArcadeBP.ArcadeMotor arcadeMotor; // Referensi ke ArcadeMotor

    public float minSpeedArrowAngle;
    public float maxSpeedArrowAngle;

    [Header("UI")]
    public TMP_Text speedLabel;
    public RectTransform arrow;

    private float speed = 0.0f;

    private void Update()
    {
        // Ambil kecepatan dan konversi ke KM/H
        speed = target.velocity.magnitude;// * 3.6f;

        if (speedLabel != null)
            speedLabel.text = ((int)speed).ToString() + " km/h";

        // Ambil maxSpeed dari ArcadeMotor, bukan dari variabel lokal
        float currentMaxSpeed = arcadeMotor != null ? arcadeMotor.MaxSpeed : 100f;

        if (arrow != null)
        {
            float t = Mathf.InverseLerp(0, currentMaxSpeed, speed); // Normalisasi
            arrow.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(minSpeedArrowAngle, maxSpeedArrowAngle, t));
        }
    }
}
