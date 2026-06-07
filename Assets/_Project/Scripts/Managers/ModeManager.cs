using UnityEngine;

public class ModeManager : MonoBehaviour
{
    public static ModeManager Instance { get; private set; }

    [System.Serializable]
    public struct Phase
    {
        public bool isScatter;     // true=Scatter, false=Chase
        public float duration;     // segundos (-1 = permanente)
    }

    [Header("Fases canónicas del arcade (Nivel 1)")]
    [SerializeField]
    private Phase[] phases = new Phase[]
    {
        new Phase { isScatter = true,  duration = 7f  },
        new Phase { isScatter = false, duration = 20f },
        new Phase { isScatter = true,  duration = 7f  },
        new Phase { isScatter = false, duration = 20f },
        new Phase { isScatter = true,  duration = 5f  },
        new Phase { isScatter = false, duration = 20f },
        new Phase { isScatter = true,  duration = 5f  },
        new Phase { isScatter = false, duration = -1f } // Chase permanente
    };

    private int currentPhase = 0;
    private float phaseTimer = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        ApplyCurrentPhase();
    }

    private void Update()
    {
        if (phases[currentPhase].duration < 0f) return; // fase permanente

        phaseTimer += Time.deltaTime;
        if (phaseTimer >= phases[currentPhase].duration)
        {
            currentPhase++;
            phaseTimer = 0f;
            if (currentPhase >= phases.Length) currentPhase = phases.Length - 1;
            ApplyCurrentPhase();
        }
    }

    private void ApplyCurrentPhase()
    {
        bool scatter = phases[currentPhase].isScatter;
        Debug.Log($"<color=yellow>Mode: {(scatter ? "SCATTER" : "CHASE")}</color>  (fase {currentPhase})");

        foreach (var g in GameManager.Instance.Ghosts)
        {
            if (scatter) g.EnterScatter();
            else g.EnterChase();
        }
    }

    public bool IsScatter => phases[currentPhase].isScatter;
}
