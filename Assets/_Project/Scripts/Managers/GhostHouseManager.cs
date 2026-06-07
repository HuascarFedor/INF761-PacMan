using UnityEngine;

public class GhostHouseManager : MonoBehaviour
{
    public static GhostHouseManager Instance { get; private set; }

    [Header("Umbrales canónicos (pellets comidos)")]
    [SerializeField] private int blinkyThreshold = 0;
    [SerializeField] private int pinkyThreshold = 0;
    [SerializeField] private int inkyThreshold = 30;
    [SerializeField] private int clydeThreshold = 60;

    [Header("Referencias")]
    [SerializeField] private GhostBase blinky;
    [SerializeField] private GhostBase pinky;
    [SerializeField] private GhostBase inky;
    [SerializeField] private GhostBase clyde;

    private bool blinkyReleased = false;
    private bool pinkyReleased = false;
    private bool inkyReleased = false;
    private bool clydeReleased = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Blinky comienza ya fuera de la casa.
        if (blinky != null) { blinky.LeaveHouse(); blinkyReleased = true; }
        OnPelletEaten(0); // Pinky sale inmediatamente (umbral 0).
    }

    public void OnPelletEaten(int pelletsEaten)
    {
        if (!pinkyReleased && pelletsEaten >= pinkyThreshold) { pinky.LeaveHouse(); pinkyReleased = true; }
        if (!inkyReleased && pelletsEaten >= inkyThreshold) { inky.LeaveHouse(); inkyReleased = true; }
        if (!clydeReleased && pelletsEaten >= clydeThreshold) { clyde.LeaveHouse(); clydeReleased = true; }
    }

    public void ResetReleaseState(int currentPellets)
    {
        blinkyReleased = false;
        pinkyReleased = false;
        inkyReleased = false;
        clydeReleased = false;

        if (blinky != null) { blinky.LeaveHouse(); blinkyReleased = true; }
        OnPelletEaten(currentPellets);
    }
}
