using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Mengatur siklus langit dan pencahayaan dinamis (Pagi, Siang, Sore, Malam).
/// - Siang menggunakan Skybox bawaan Polytope.
/// - Pagi, Sore, dan Malam menggunakan aset Skybox dari Assets/SkyAssets.
/// Tekan [T] saat Play untuk langsung berpindah fase waktu.
/// </summary>
public class DayNightCycleManager : MonoBehaviour
{
    public static DayNightCycleManager Instance { get; private set; }

    public enum TimePhase
    {
        Morning,
        Day,
        Evening,
        Night
    }

    [Header("Skybox Materials")]
    public Material morningSkybox;
    public Material daySkybox;
    public Material eveningSkybox;
    public Material nightSkybox;

    [Header("Cycle Settings")]
    public bool autoCycle = true;
    [Tooltip("Durasi 1 siklus penuh 24 jam dalam detik nyata.")]
    public float fullDayDurationSeconds = 180f;
    [Range(0f, 24f)]
    public float timeOfDayHours = 11.5f;
    [Tooltip("Kecepatan putaran awan/langit perlahan (derajat/detik) jika shader mendukung _Rotation.")]
    public float skyRotationSpeed = 0.5f;

    [Header("Scene References")]
    public Light sunLight;

    private Material _rtMorning;
    private Material _rtDay;
    private Material _rtEvening;
    private Material _rtNight;
    private Material _activeRuntimeSky;
    private float _activeBaseExposure = 1f;

    private TimePhase _currentPhase = TimePhase.Day;
    private bool _phaseInitialized;
    private float _skyFadeTimer;
    private float _skyAngle;

    private struct AtmosphereKey
    {
        public float hour;
        public Color lightColor;
        public float lightIntensity;
        public Quaternion lightRot;
        public float shadowStrength;
        public Color fogColor;
        public float fogStart;
        public float fogEnd;
        public float ambientIntensity;
        public float nightFactor;
    }

    private static readonly AtmosphereKey[] Keys =
    {
        // Pagi (06:30)
        new AtmosphereKey
        {
            hour = 6.5f,
            lightColor = new Color(1.00f, 0.85f, 0.66f, 1f),
            lightIntensity = 1.15f,
            lightRot = Quaternion.Euler(22f, -68f, 0f),
            shadowStrength = 0.78f,
            fogColor = new Color(0.82f, 0.86f, 0.92f, 1f),
            fogStart = 36f,
            fogEnd = 265f,
            ambientIntensity = 0.95f,
            nightFactor = 0f
        },
        // Siang (12:30) - Mengikuti setting asli Polytope Environment_Free
        new AtmosphereKey
        {
            hour = 12.5f,
            lightColor = new Color(1.00f, 0.955f, 0.84f, 1f),
            lightIntensity = 1.38f,
            lightRot = Quaternion.Euler(50f, -30f, 0f),
            shadowStrength = 0.86f,
            fogColor = new Color(0.74f, 0.90f, 0.95f, 1f),
            fogStart = 35f,
            fogEnd = 285f,
            ambientIntensity = 1.05f,
            nightFactor = 0f
        },
        // Sore (17:30)
        new AtmosphereKey
        {
            hour = 17.5f,
            lightColor = new Color(1.00f, 0.58f, 0.30f, 1f),
            lightIntensity = 1.08f,
            lightRot = Quaternion.Euler(16f, 38f, 0f),
            shadowStrength = 0.85f,
            fogColor = new Color(0.84f, 0.58f, 0.48f, 1f),
            fogStart = 32f,
            fogEnd = 245f,
            ambientIntensity = 0.88f,
            nightFactor = 0.15f
        },
        // Malam (23:30)
        new AtmosphereKey
        {
            hour = 23.5f,
            lightColor = new Color(0.55f, 0.72f, 1.00f, 1f),
            lightIntensity = 0.58f,
            lightRot = Quaternion.Euler(42f, -15f, 0f),
            shadowStrength = 0.62f,
            fogColor = new Color(0.09f, 0.14f, 0.24f, 1f),
            fogStart = 28f,
            fogEnd = 205f,
            ambientIntensity = 0.68f,
            nightFactor = 1f
        }
    };

    public TimePhase CurrentPhase => _currentPhase;

    public string CurrentPhaseLabel
    {
        get
        {
            switch (_currentPhase)
            {
                case TimePhase.Morning: return "Pagi";
                case TimePhase.Day: return "Siang";
                case TimePhase.Evening: return "Sore";
                default: return "Malam";
            }
        }
    }

    public string FormattedClock
    {
        get
        {
            float wrapped = Mathf.Repeat(timeOfDayHours, 24f);
            int h = Mathf.FloorToInt(wrapped);
            int m = Mathf.FloorToInt((wrapped - h) * 60f);
            return $"{h:00}:{m:00}";
        }
    }

    private void Awake()
    {
        Instance = this;

        if (daySkybox == null)
        {
            daySkybox = RenderSettings.skybox;
        }

#if UNITY_EDITOR
        // Auto-resolve aset SkyAssets asli jika referensi di scene sempat kosong/terlepas
        if (morningSkybox == null)
            morningSkybox = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/SkyAssets/Materials/StylizedSkyboxes_unity_panoramas_02_golden_hour_PanoSky.mat");
        if (eveningSkybox == null)
            eveningSkybox = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/SkyAssets/Materials/AlienWorldsSkyboxes_unity_panoramas_12_tidally_locked_twilight_PanoSky.mat");
        if (nightSkybox == null)
            nightSkybox = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/SkyAssets/Materials/StylizedSkyboxes_unity_panoramas_12_moonlit_night_PanoSky.mat");
#endif

        if (sunLight == null)
        {
            sunLight = FindDirectionalLight();
        }

        _rtDay = CloneMaterialSafe(daySkybox);
        _rtMorning = CloneMaterialSafe(morningSkybox != null ? morningSkybox : daySkybox);
        _rtEvening = CloneMaterialSafe(eveningSkybox != null ? eveningSkybox : daySkybox);
        _rtNight = CloneMaterialSafe(nightSkybox != null ? nightSkybox : daySkybox);
    }

    private void Start()
    {
        ApplyCurrentTime(forceSkyboxSwap: true);
    }

    private void Update()
    {
        if (autoCycle && fullDayDurationSeconds > 1f)
        {
            timeOfDayHours = Mathf.Repeat(timeOfDayHours + (24f / fullDayDurationSeconds) * Time.deltaTime, 24f);
        }

        _skyAngle = Mathf.Repeat(_skyAngle + skyRotationSpeed * Time.deltaTime, 360f);
        ApplyCurrentTime(forceSkyboxSwap: false);
    }

    private void OnDestroy()
    {
        DestroyRuntimeMat(ref _rtMorning);
        DestroyRuntimeMat(ref _rtDay);
        DestroyRuntimeMat(ref _rtEvening);
        DestroyRuntimeMat(ref _rtNight);
    }

    public void CycleToNextPhase()
    {
        TimePhase next = (TimePhase)(((int)_currentPhase + 1) % 4);
        SetPhase(next);
        if (ThirdPersonPlayerController.Instance != null)
        {
            CombatFXHelper.SpawnFloatingText(
                ThirdPersonPlayerController.Instance.transform.position + Vector3.up * 0.9f,
                $"WAKTU: {CurrentPhaseLabel.ToUpper()} ({FormattedClock})",
                new Color(1f, 0.92f, 0.55f),
                1.1f);
        }
    }

    public void SetPhase(TimePhase phase)
    {
        switch (phase)
        {
            case TimePhase.Morning: timeOfDayHours = 7.0f; break;
            case TimePhase.Day: timeOfDayHours = 12.0f; break;
            case TimePhase.Evening: timeOfDayHours = 17.25f; break;
            case TimePhase.Night: timeOfDayHours = 22.5f; break;
        }
        ApplyCurrentTime(forceSkyboxSwap: true);
    }

    private void ApplyCurrentTime(bool forceSkyboxSwap)
    {
        float h = Mathf.Repeat(timeOfDayHours, 24f);
        TimePhase phase = DeterminePhase(h);

        if (forceSkyboxSwap || !_phaseInitialized || phase != _currentPhase)
        {
            _currentPhase = phase;
            _phaseInitialized = true;
            SwapSkyboxForPhase(phase);
        }

        // Interpolasi halus antar 4 keyframe atmosfer (melingkar lewat tengah malam)
        EvaluateAtmosphere(h, out AtmosphereKey blended);

        if (sunLight == null) sunLight = FindDirectionalLight();
        if (sunLight != null)
        {
            RenderSettings.sun = sunLight;
            sunLight.color = blended.lightColor;
            sunLight.intensity = blended.lightIntensity;
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = blended.shadowStrength;
            sunLight.shadowBias = 0.03f;
            sunLight.shadowNormalBias = 0.32f;
            sunLight.transform.rotation = blended.lightRot;
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = blended.fogColor;
        RenderSettings.fogStartDistance = blended.fogStart;
        RenderSettings.fogEndDistance = blended.fogEnd;

        if (blended.nightFactor >= 0.45f)
        {
            // Malam memakai Trilight biru dingin agar karakter & monster tetap terlihat jelas saat bertarung
            float t = Mathf.InverseLerp(0.45f, 1f, blended.nightFactor);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(new Color(0.42f, 0.48f, 0.62f), new Color(0.18f, 0.25f, 0.40f), t);
            RenderSettings.ambientEquatorColor = Color.Lerp(new Color(0.32f, 0.36f, 0.44f), new Color(0.13f, 0.18f, 0.28f), t);
            RenderSettings.ambientGroundColor = Color.Lerp(new Color(0.16f, 0.18f, 0.16f), new Color(0.08f, 0.10f, 0.15f), t);
            RenderSettings.ambientIntensity = blended.ambientIntensity;
        }
        else
        {
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = blended.ambientIntensity;
        }

        // Putaran awan perlahan & easing exposure saat transisi skybox
        if (_activeRuntimeSky != null)
        {
            if (_activeRuntimeSky.HasProperty("_Rotation"))
            {
                _activeRuntimeSky.SetFloat("_Rotation", _skyAngle);
            }
            if (_skyFadeTimer > 0f && _activeRuntimeSky.HasProperty("_Exposure"))
            {
                _skyFadeTimer = Mathf.Max(0f, _skyFadeTimer - Time.deltaTime);
                float ease = 1f - (_skyFadeTimer / 0.65f);
                _activeRuntimeSky.SetFloat("_Exposure", Mathf.Lerp(_activeBaseExposure * 0.72f, _activeBaseExposure, ease));
            }
        }
    }

    private void SwapSkyboxForPhase(TimePhase phase)
    {
        Material target = _rtDay;
        switch (phase)
        {
            case TimePhase.Morning: target = _rtMorning ?? _rtDay; break;
            case TimePhase.Day: target = _rtDay; break;
            case TimePhase.Evening: target = _rtEvening ?? _rtDay; break;
            case TimePhase.Night: target = _rtNight ?? _rtDay; break;
        }

        if (target == null) return;
        _activeRuntimeSky = target;
        _activeBaseExposure = target.HasProperty("_Exposure") ? target.GetFloat("_Exposure") : 1f;
        _skyFadeTimer = 0.65f;

        RenderSettings.skybox = target;
        DynamicGI.UpdateEnvironment();
    }

    private static TimePhase DeterminePhase(float hour)
    {
        if (hour >= 5.0f && hour < 9.0f) return TimePhase.Morning;
        if (hour >= 9.0f && hour < 15.5f) return TimePhase.Day;
        if (hour >= 15.5f && hour < 19.0f) return TimePhase.Evening;
        return TimePhase.Night;
    }

    private static void EvaluateAtmosphere(float hour, out AtmosphereKey result)
    {
        // Cari dua keyframe yang mengapit 'hour' secara melingkar (0..24)
        int n = Keys.Length;
        int aIdx = n - 1;
        int bIdx = 0;

        for (int i = 0; i < n; i++)
        {
            int next = (i + 1) % n;
            float hA = Keys[i].hour;
            float hB = Keys[next].hour;
            if (hB > hA)
            {
                if (hour >= hA && hour <= hB)
                {
                    aIdx = i;
                    bIdx = next;
                    break;
                }
            }
            else
            {
                // Melewati tengah malam (23.5 -> 6.5)
                if (hour >= hA || hour <= hB)
                {
                    aIdx = i;
                    bIdx = next;
                    break;
                }
            }
        }

        AtmosphereKey a = Keys[aIdx];
        AtmosphereKey b = Keys[bIdx];

        float span = b.hour >= a.hour ? (b.hour - a.hour) : (24f - a.hour + b.hour);
        float delta = hour >= a.hour ? (hour - a.hour) : (24f - a.hour + hour);
        float t = span > 0.001f ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(delta / span)) : 0f;

        result = new AtmosphereKey
        {
            hour = hour,
            lightColor = Color.Lerp(a.lightColor, b.lightColor, t),
            lightIntensity = Mathf.Lerp(a.lightIntensity, b.lightIntensity, t),
            lightRot = Quaternion.Slerp(a.lightRot, b.lightRot, t),
            shadowStrength = Mathf.Lerp(a.shadowStrength, b.shadowStrength, t),
            fogColor = Color.Lerp(a.fogColor, b.fogColor, t),
            fogStart = Mathf.Lerp(a.fogStart, b.fogStart, t),
            fogEnd = Mathf.Lerp(a.fogEnd, b.fogEnd, t),
            ambientIntensity = Mathf.Lerp(a.ambientIntensity, b.ambientIntensity, t),
            nightFactor = Mathf.Lerp(a.nightFactor, b.nightFactor, t)
        };
    }

    private static Light FindDirectionalLight()
    {
        if (RenderSettings.sun != null) return RenderSettings.sun;
        Light[] lights = FindObjectsByType<Light>();
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null && lights[i].type == LightType.Directional) return lights[i];
        }
        return null;
    }

    private static Material CloneMaterialSafe(Material src)
    {
        return src != null ? new Material(src) : null;
    }

    private static void DestroyRuntimeMat(ref Material mat)
    {
        if (mat != null)
        {
            Destroy(mat);
            mat = null;
        }
    }
}
