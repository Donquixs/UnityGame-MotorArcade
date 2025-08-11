using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class BatterySystem : MonoBehaviour
{
    [Header("Battery Settings")]
    public float maxBattery = 100f;
    private float currentBattery;
    public float batteryDrainBase = 0.5f; // Konsumsi dasar baterai per detik
    public float batteryDrainMultiplier = 2.0f; // Skala konsumsi berdasarkan kedalaman gas
    public float batteryDrainPerLap = 5f; // Pengurangan baterai per lap

    [Header("UI")]
    public TextMeshProUGUI batteryText; // UI untuk menampilkan status baterai

    [Header("Player Input")]
    public PlayerInput playerInput; // Referensi ke PlayerInput
    private InputAction accelerateAction;

    private int lapCount = 0; // Jumlah lap yang sudah ditempuh

    void Start()
    {
        // Set baterai ke nilai maksimum saat game mulai
        currentBattery = maxBattery;

        // Ambil action dari PlayerInput
        accelerateAction = playerInput.actions["Accelerate"];
    }

    void Update()
    {
        // Baca nilai input akselerasi (0 - 1)
        float accelerationInput = accelerateAction.ReadValue<float>();

        if (accelerationInput > 0.7f)
        {
            // Kurangi baterai lebih cepat jika gas ditarik lebih dalam
            currentBattery -= (batteryDrainBase + (accelerationInput * batteryDrainMultiplier)) * Time.deltaTime;
        }
        else if(accelerationInput <=0.6f)
        {
            currentBattery -= batteryDrainBase * Time.deltaTime;
        }
       

        // Pastikan baterai tidak kurang dari 0
        currentBattery = Mathf.Max(0, currentBattery);

        // Update UI TextMeshPro
        if (batteryText != null)
        {
            batteryText.text = $"Battery: {Mathf.FloorToInt(currentBattery)}%";

        }

        // Jika baterai habis, lakukan sesuatu (misalnya, matikan motor)
        if (currentBattery <= 0)
        {
            Debug.Log("Baterai habis! Motor berhenti.");
        }
    }

    // Dipanggil saat pemain menyelesaikan satu lap
    public void OnLapCompleted()
    {
        lapCount++;
        currentBattery -= batteryDrainPerLap;
        currentBattery = Mathf.Max(0, currentBattery);

        Debug.Log($"Lap {lapCount} selesai. Baterai tersisa: {currentBattery}%");
    }
}
