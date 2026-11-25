using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Zona perebutan wilayah pada Menhir di masing-masing village target.
/// Jumlah & komposisi penjaga village di-generate menggunakan PCG + DDA.
/// </summary>
public class TerritoryCaptureZone : MonoBehaviour
{
    public string territoryName = "Enemy Village";
    public float activationRadius = 42f;
    public float captureRadius = 7.5f;
    public float captureDuration = 6f;

    private bool _garrisonSpawned;
    private bool _captured;
    private float _captureProgress;
    private readonly List<EnemyAIController> _defenders = new List<EnemyAIController>();
    private TextMesh _statusLabel;
    private Light _beaconLight;

    public bool IsCaptured => _captured;
    public float CaptureProgress => _captureProgress;
    public int AliveDefenderCount
    {
        get
        {
            int count = 0;
            for (int i = _defenders.Count - 1; i >= 0; i--)
            {
                if (_defenders[i] == null || _defenders[i].IsDead) _defenders.RemoveAt(i);
                else count++;
            }
            return count;
        }
    }

    private void Start()
    {
        CreateBeaconVisuals();
        if (GameLoopManager.Instance != null) GameLoopManager.Instance.RegisterTerritory(this);
    }

    private void Update()
    {
        UpdateLabelOrientation();

        if (_captured)
        {
            UpdateLabelText();
            return;
        }

        ThirdPersonPlayerController player = ThirdPersonPlayerController.Instance;
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.transform.position);
        if (!_garrisonSpawned && dist <= activationRadius)
        {
            SpawnVillageGarrison();
        }

        int alive = AliveDefenderCount;
        if (_garrisonSpawned && alive == 0 && dist <= captureRadius)
        {
            _captureProgress = Mathf.Min(1f, _captureProgress + Time.deltaTime / captureDuration);
            if (_captureProgress >= 1f)
            {
                CompleteCapture();
            }
        }

        UpdateLabelText();
    }

    private void SpawnVillageGarrison()
    {
        _garrisonSpawned = true;
        PCGWorldEcosystemManager pcg = PCGWorldEcosystemManager.Instance;
        if (pcg == null) return;

        DDAManager dda = DDAManager.Instance;
        int count = dda != null ? Mathf.RoundToInt(Mathf.Lerp(3f, 7f, dda.difficultyIndex)) : 4;

        for (int i = 0; i < count; i++)
        {
            float angle = (i / (float)count) * Mathf.PI * 2f;
            Vector3 desired = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Random.Range(4.5f, 10f);
            if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 6f, NavMesh.AllAreas))
            {
                EnemyAIController enemy = pcg.SpawnAdaptedEnemyAt(hit.position, i == count - 1);
                if (enemy != null)
                {
                    enemy.ownerTerritory = this;
                    enemy.patrolRadius = 9f;
                    _defenders.Add(enemy);
                }
            }
        }
    }

    public void NotifyDefenderKilled(EnemyAIController enemy)
    {
        _defenders.Remove(enemy);
    }

    private void CompleteCapture()
    {
        _captured = true;
        _captureProgress = 1f;
        if (_beaconLight != null) _beaconLight.color = new Color(0.2f, 0.85f, 1f);

        CombatFXHelper.SpawnBurstRing(transform.position, captureRadius, new Color(0.2f, 0.85f, 1f), 0.7f);
        CombatFXHelper.SpawnFloatingText(transform.position + Vector3.up * 1.2f, $"{territoryName.ToUpper()} CAPTURED!", Color.cyan, 1.4f);

        if (ThirdPersonPlayerController.Instance != null)
        {
            ThirdPersonPlayerController.Instance.Heal(40f);
            ThirdPersonPlayerController.Instance.GainEnergy(40f);
        }
        if (DDAManager.Instance != null) DDAManager.Instance.OnTerritoryCaptured();
        if (GameLoopManager.Instance != null) GameLoopManager.Instance.NotifyTerritoryCaptured(this);
    }

    private void CreateBeaconVisuals()
    {
        GameObject lbl = new GameObject("Territory_Status_Label");
        lbl.transform.SetParent(transform);
        lbl.transform.localPosition = new Vector3(0f, 5.2f, 0f);
        _statusLabel = lbl.AddComponent<TextMesh>();
        _statusLabel.fontSize = 32;
        _statusLabel.characterSize = 0.12f;
        _statusLabel.alignment = TextAlignment.Center;
        _statusLabel.anchor = TextAnchor.LowerCenter;

        GameObject lightGo = new GameObject("Beacon_Light");
        lightGo.transform.SetParent(transform);
        lightGo.transform.localPosition = new Vector3(0f, 3.0f, 0f);
        _beaconLight = lightGo.AddComponent<Light>();
        _beaconLight.type = LightType.Point;
        _beaconLight.range = 14f;
        _beaconLight.intensity = 4f;
        _beaconLight.color = new Color(1f, 0.25f, 0.2f);
    }

    private void UpdateLabelOrientation()
    {
        if (_statusLabel != null && Camera.main != null)
        {
            _statusLabel.transform.rotation = Quaternion.LookRotation(_statusLabel.transform.position - Camera.main.transform.position);
        }
    }

    private void UpdateLabelText()
    {
        if (_statusLabel == null) return;
        if (_captured)
        {
            _statusLabel.text = $"[CAPTURED]\n{territoryName}";
            _statusLabel.color = new Color(0.3f, 0.95f, 1f);
        }
        else if (!_garrisonSpawned)
        {
            _statusLabel.text = $"[ENEMY TERRITORY]\n{territoryName}\nApproach to Engage";
            _statusLabel.color = new Color(1f, 0.45f, 0.35f);
        }
        else
        {
            int alive = AliveDefenderCount;
            if (alive > 0)
            {
                _statusLabel.text = $"[CONTESTED] {territoryName}\nDefenders Left: {alive}";
                _statusLabel.color = new Color(1f, 0.55f, 0.2f);
            }
            else
            {
                int pct = Mathf.RoundToInt(_captureProgress * 100f);
                _statusLabel.text = $"[STAND NEAR MENHIR]\nCapturing {territoryName}: {pct}%";
                _statusLabel.color = Color.yellow;
            }
        }
    }
}
