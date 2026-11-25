using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Dynamic Difficulty Adjustment (DDA) Manager.
/// Mengukur performa pemain secara real-time, menghitung DifficultyIndex [0..1],
/// dan mengubah beberapa parameter PCG maupun perilaku Game AI sekaligus.
/// </summary>
public class DDAManager : MonoBehaviour
{
    public static DDAManager Instance { get; private set; }

    public enum DifficultyTier
    {
        Relaxed,
        Balanced,
        Intense,
        Nightmare
    }

    [Header("DDA State")]
    [Range(0f, 1f)] public float difficultyIndex = 0.40f;
    public float evaluationInterval = 16f;
    public bool showDdaHud = true;
    public bool showAiDebugLabels = true;

    // Telemetry performa pemain (sliding window)
    private float _damageTakenWindow;
    private int _shotsFired;
    private int _shotsHit;
    private int _dodgesSucceeded;
    private int _killsCount;
    private float _killTimeAccumulator;
    private int _deathsCount;
    private float _evalTimer;
    private float _lastPerformanceScore = 0.5f;
    private string _lastAdaptationReason = "Initial Balanced State";

    public event Action<float> OnDifficultyChanged;

    // --- Parameter PCG yang dikendalikan DDA (> 1 parameter berubah!) ---
    public int ForestPackCount => Mathf.RoundToInt(Mathf.Lerp(4f, 12f, difficultyIndex));
    public int MaxMonstersPerPack => Mathf.RoundToInt(Mathf.Lerp(2f, 4f, difficultyIndex));
    public float BruteRatio => Mathf.Lerp(0.10f, 0.48f, difficultyIndex);
    public float RangedRatio => Mathf.Lerp(0.15f, 0.38f, difficultyIndex);
    public int HealMushroomCount => Mathf.RoundToInt(Mathf.Lerp(18f, 5f, difficultyIndex));
    public int EnergyOreCount => Mathf.RoundToInt(Mathf.Lerp(14f, 6f, difficultyIndex));
    public float RespawnInterval => Mathf.Lerp(38f, 16f, difficultyIndex);

    // --- Parameter Game AI yang dikendalikan DDA (mengubah behavior, bukan cuma angka!) ---
    public float AISightRange => Mathf.Lerp(10f, 22f, difficultyIndex);
    public float AISightAngle => Mathf.Lerp(85f, 145f, difficultyIndex);
    public float AIAttackCooldown => Mathf.Lerp(2.35f, 1.05f, difficultyIndex);
    public float AIFleeHpThreshold => Mathf.Lerp(0.40f, 0.12f, difficultyIndex);
    public float AIMemoryDuration => Mathf.Lerp(2.5f, 7.5f, difficultyIndex);
    public bool AIUseTacticalBehavior => difficultyIndex >= 0.42f; // Mengaktifkan Flanking & TakeCover

    public DifficultyTier CurrentTier
    {
        get
        {
            if (difficultyIndex < 0.28f) return DifficultyTier.Relaxed;
            if (difficultyIndex < 0.56f) return DifficultyTier.Balanced;
            if (difficultyIndex < 0.80f) return DifficultyTier.Intense;
            return DifficultyTier.Nightmare;
        }
    }

    public float Accuracy => _shotsFired > 0 ? (float)_shotsHit / _shotsFired : 0.65f;
    public float LastPerformanceScore => _lastPerformanceScore;
    public string LastAdaptationReason => _lastAdaptationReason;

    private void Awake()
    {
        Instance = this;
        _evalTimer = evaluationInterval;
    }

    private void Update()
    {
        HandleDebugHotkeys();

        _evalTimer -= Time.deltaTime;
        if (_evalTimer <= 0f)
        {
            _evalTimer = evaluationInterval;
            EvaluateAndAdapt("Periodic Performance Check");
        }
    }

    private void HandleDebugHotkeys()
    {
        bool toggleAi = false;
        bool toggleHud = false;
        bool forceEasy = false;
        bool forceHard = false;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current ?? InputSystem.GetDevice<Keyboard>();
        if (kb != null)
        {
            if (!kb.enabled) InputSystem.EnableDevice(kb);
            toggleAi |= kb.f1Key.wasPressedThisFrame;
            toggleHud |= kb.f2Key.wasPressedThisFrame;
            forceEasy |= kb.f3Key.wasPressedThisFrame;
            forceHard |= kb.f4Key.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        toggleAi |= Input.GetKeyDown(KeyCode.F1);
        toggleHud |= Input.GetKeyDown(KeyCode.F2);
        forceEasy |= Input.GetKeyDown(KeyCode.F3);
        forceHard |= Input.GetKeyDown(KeyCode.F4);
#endif

        if (toggleAi) showAiDebugLabels = !showAiDebugLabels;
        if (toggleHud) showDdaHud = !showDdaHud;

        if (forceEasy)
        {
            difficultyIndex = 0.15f;
            _lastPerformanceScore = 0.22f;
            _lastAdaptationReason = "Demo Override: Player Struggling -> Difficulty DOWN";
            ApplyAdaptation();
        }
        else if (forceHard)
        {
            difficultyIndex = 0.88f;
            _lastPerformanceScore = 0.91f;
            _lastAdaptationReason = "Demo Override: Player Dominant -> Difficulty UP";
            ApplyAdaptation();
        }
    }

    public void RecordPlayerDamageTaken(float dmg) => _damageTakenWindow += dmg;
    public void RecordPlayerShot(bool hit) { _shotsFired++; if (hit) _shotsHit++; }
    public void RecordSuccessfulDodge() => _dodgesSucceeded++;
    public void RecordPlayerDeath()
    {
        _deathsCount++;
        difficultyIndex = Mathf.Clamp01(difficultyIndex - 0.18f);
        _lastAdaptationReason = "Player Knocked Out -> Difficulty Reduced";
        ApplyAdaptation();
    }

    public void RecordEnemyKilled(float engagementSeconds)
    {
        _killsCount++;
        _killTimeAccumulator += Mathf.Clamp(engagementSeconds, 1f, 30f);
        if (_killsCount % 3 == 0)
        {
            EvaluateAndAdapt("Combat Milestone (3 Kills)");
        }
    }

    public void OnTerritoryCaptured()
    {
        EvaluateAndAdapt("Territory Captured");
    }

    public void EvaluateAndAdapt(string trigger)
    {
        ThirdPersonPlayerController p = ThirdPersonPlayerController.Instance;
        float hpRatio = p != null ? p.CurrentHp / Mathf.Max(1f, p.maxHp) : 1f;
        float acc = Accuracy;
        float dmgScore = 1f - Mathf.Clamp01(_damageTakenWindow / 120f);
        float avgTtk = _killsCount > 0 ? _killTimeAccumulator / _killsCount : 8f;
        float ttkScore = 1f - Mathf.Clamp01((avgTtk - 3f) / 15f);
        float dodgeBonus = Mathf.Clamp01(_dodgesSucceeded * 0.08f);

        float perf = (hpRatio * 0.35f) + (dmgScore * 0.25f) + (acc * 0.20f) + (ttkScore * 0.15f) + (dodgeBonus * 0.05f);
        _lastPerformanceScore = Mathf.Clamp01(perf);

        float prev = difficultyIndex;
        if (_lastPerformanceScore > 0.65f)
        {
            difficultyIndex = Mathf.Clamp01(difficultyIndex + 0.12f);
            _lastAdaptationReason = $"{trigger}: High Perf ({_lastPerformanceScore:F2}) -> Difficulty UP";
        }
        else if (_lastPerformanceScore < 0.40f)
        {
            difficultyIndex = Mathf.Clamp01(difficultyIndex - 0.12f);
            _lastAdaptationReason = $"{trigger}: Low Perf ({_lastPerformanceScore:F2}) -> Difficulty DOWN";
        }
        else
        {
            _lastAdaptationReason = $"{trigger}: Steady Perf ({_lastPerformanceScore:F2}) -> Maintained";
        }

        // Decay window untuk pembacaan periode berikutnya
        _damageTakenWindow *= 0.35f;
        _dodgesSucceeded = Mathf.Max(0, _dodgesSucceeded - 1);

        if (!Mathf.Approximately(prev, difficultyIndex))
        {
            ApplyAdaptation();
        }
    }

    private void ApplyAdaptation()
    {
        OnDifficultyChanged?.Invoke(difficultyIndex);
        if (PCGWorldEcosystemManager.Instance != null)
        {
            PCGWorldEcosystemManager.Instance.OnDifficultyUpdated();
        }
    }
}
