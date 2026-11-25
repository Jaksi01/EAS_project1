using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Mengatur alur permainan (Conquest 3 Village), HUD pemain, serta panel live
/// telemetri DDA + PCG untuk demonstrasi Project EAS.
/// </summary>
public class GameLoopManager : MonoBehaviour
{
    public static GameLoopManager Instance { get; private set; }

    private readonly List<TerritoryCaptureZone> _territories = new List<TerritoryCaptureZone>();
    private bool _victoryAchieved;
    private float _matchStartTime;

    private void Awake()
    {
        Instance = this;
        _matchStartTime = Time.time;
        ApplyVisualQualityAndLighting();
    }

    /// <summary>
    /// Mengembalikan nuansa atmosfer asli Polytope Lowpoly Nature Pack (sinar matahari 50,-30,0,
    /// skybox ambient depth, fog kejauhan lembut, ACES Tonemapping + Bloom + ColorAdjustments + Vignette)
    /// sekaligus mempertahankan ketajaman 4x MSAA dan jarak tree billboard 300m.
    /// </summary>
    public static void ApplyVisualQualityAndLighting()
    {
        // 1. Fog atmosfer khas Polytope: foreground hingga 35m bersih tajam, lalu memudar lembut ke kabut langit
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.74f, 0.90f, 0.95f, 1f);
        RenderSettings.fogStartDistance = 35f;
        RenderSettings.fogEndDistance = 285f;

        // 2. Ambient Skybox agar kontras bayangan terlihat alami & bervolume (tidak datar/kaku)
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.05f;

        // 3. Directional Light sesuai orientasi asli Environment_Free (50, -30, 0) dengan shadow kontras alami
        Light sun = RenderSettings.sun;
        if (sun == null)
        {
            Light[] lights = FindObjectsByType<Light>();
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i].type == LightType.Directional)
                {
                    sun = lights[i];
                    break;
                }
            }
        }
        if (sun != null)
        {
            RenderSettings.sun = sun;
            sun.color = new Color(1.0f, 0.955f, 0.84f, 1f);
            sun.intensity = 1.38f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.86f;
            sun.shadowBias = 0.03f;
            sun.shadowNormalBias = 0.32f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // 4. URP Pipeline & Quality: tetap tajam (4x MSAA, renderScale 1.0, shadow distance 145m)
        QualitySettings.antiAliasing = 4;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.lodBias = 2f;
        QualitySettings.shadowDistance = 145f;

        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
        {
            urp.renderScale = 1f;
            urp.msaaSampleCount = 4;
            urp.shadowDistance = 145f;
            urp.shadowCascadeCount = 4;
        }

        // 5. Kamera utama: gunakan 4x MSAA hardware tanpa post-AA blur + aktifkan Post-Processing
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, 1000f);
            cam.allowMSAA = true;
            UniversalAdditionalCameraData camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData != null)
            {
                camData.renderPostProcessing = true;
                camData.antialiasing = AntialiasingMode.None;
                camData.renderShadows = true;
            }
        }

        // 6. Terrain: ikuti jarak asli Environment_Free (300m tree billboard, 200m detail grass)
        Terrain[] terrains = Terrain.activeTerrains;
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain t = terrains[i];
            if (t == null) continue;
            t.treeDistance = 1500f;
            t.treeBillboardDistance = 300f;
            t.treeCrossFadeLength = 20f;
            t.detailObjectDistance = 200f;
            t.heightmapPixelError = 3f;
            t.basemapDistance = 1000f;
        }

        // 7. Post-Processing saat Play (ACES Tonemapping + Bloom + ColorAdjustments + Vignette ala Polytope promo)
        if (Application.isPlaying)
        {
            ApplyRuntimePolytopeVolume();
        }
    }

    private static void ApplyRuntimePolytopeVolume()
    {
        GameObject volGo = GameObject.Find("--- EAS_POLYTOPE_POSTPROCESS ---");
        if (volGo == null) volGo = new GameObject("--- EAS_POLYTOPE_POSTPROCESS ---");

        Volume vol = volGo.GetComponent<Volume>();
        if (vol == null) vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 50f;
        vol.weight = 1f;

        VolumeProfile profile = vol.profile;
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            vol.profile = profile;
        }

        if (!profile.TryGet(out Tonemapping tone)) tone = profile.Add<Tonemapping>(true);
        tone.active = true;
        tone.mode.Override(TonemappingMode.ACES);

        if (!profile.TryGet(out Bloom bloom)) bloom = profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.threshold.Override(0.88f);
        bloom.intensity.Override(0.45f);
        bloom.scatter.Override(0.68f);
        bloom.highQualityFiltering.Override(true);

        if (!profile.TryGet(out ColorAdjustments colorAdj)) colorAdj = profile.Add<ColorAdjustments>(true);
        colorAdj.active = true;
        colorAdj.postExposure.Override(0.10f);
        colorAdj.contrast.Override(14f);
        colorAdj.saturation.Override(18f);

        if (!profile.TryGet(out Vignette vig)) vig = profile.Add<Vignette>(true);
        vig.active = true;
        vig.intensity.Override(0.24f);
        vig.smoothness.Override(0.36f);
    }

    public void RegisterTerritory(TerritoryCaptureZone zone)
    {
        if (!_territories.Contains(zone)) _territories.Add(zone);
    }

    public void NotifyTerritoryCaptured(TerritoryCaptureZone zone)
    {
        int captured = 0;
        for (int i = 0; i < _territories.Count; i++)
        {
            if (_territories[i] != null && _territories[i].IsCaptured) captured++;
        }
        if (_territories.Count > 0 && captured >= _territories.Count)
        {
            _victoryAchieved = true;
        }
    }

    private void Update()
    {
        bool regenPcg = false;
        bool restart = false;
        bool interactPressed = false;
        bool recallPressed = false;
        bool cycleTimePressed = false;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current ?? InputSystem.GetDevice<Keyboard>();
        if (kb != null)
        {
            if (!kb.enabled) InputSystem.EnableDevice(kb);
            regenPcg |= kb.f5Key.wasPressedThisFrame;
            restart |= kb.f6Key.wasPressedThisFrame;
            interactPressed |= kb.eKey.wasPressedThisFrame;
            recallPressed |= kb.rKey.wasPressedThisFrame;
            cycleTimePressed |= kb.tKey.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        regenPcg |= Input.GetKeyDown(KeyCode.F5);
        restart |= Input.GetKeyDown(KeyCode.F6);
        interactPressed |= Input.GetKeyDown(KeyCode.E);
        recallPressed |= Input.GetKeyDown(KeyCode.R);
        cycleTimePressed |= Input.GetKeyDown(KeyCode.T);
#endif

        if (cycleTimePressed && DayNightCycleManager.Instance != null)
        {
            DayNightCycleManager.Instance.CycleToNextPhase();
        }

        ThirdPersonPlayerController player = ThirdPersonPlayerController.Instance;
        if (player != null && !player.IsDead)
        {
            if (interactPressed)
            {
                VillageNPCController targetNpc = VillageNPCController.FindNearestPromptTarget(player.transform.position, 3.8f);
                if (targetNpc != null)
                {
                    if (targetNpc.IsRecruited) targetNpc.Dismiss();
                    else targetNpc.Recruit();
                }
            }

            if (recallPressed && VillageNPCController.RecruitedCount > 0)
            {
                VillageNPCController.RecallAllCompanions(player.transform.position);
            }
        }

        if (regenPcg && PCGWorldEcosystemManager.Instance != null)
        {
            PCGWorldEcosystemManager.Instance.GenerateEcosystem(clearExisting: true);
            if (ThirdPersonPlayerController.Instance != null)
            {
                CombatFXHelper.SpawnFloatingText(ThirdPersonPlayerController.Instance.transform.position, "PCG REGENERATED!", Color.yellow, 1.2f);
            }
        }

        if (restart)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void OnGUI()
    {
        DrawPlayerAndConquestHud();
        DrawCompanionsHud();
        DrawInteractPrompt();
        if (DDAManager.Instance == null || DDAManager.Instance.showDdaHud)
        {
            DrawDdaPcgTelemetryHud();
        }
        if (_victoryAchieved)
        {
            DrawVictoryBanner();
        }
    }

    private void DrawPlayerAndConquestHud()
    {
        ThirdPersonPlayerController p = ThirdPersonPlayerController.Instance;
        GUI.Box(new Rect(14, 14, 375, 246), "");

        GUIStyle header = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 };
        header.normal.textColor = Color.white;
        GUI.Label(new Rect(24, 20, 355, 24), p != null ? $"Active: {p.ActiveCharacterName}" : "Player", header);

        if (p != null)
        {
            DrawBar(new Rect(24, 48, 350, 18), p.CurrentHp / p.maxHp, new Color(0.22f, 0.82f, 0.32f),
                $"HP: {p.CurrentHp:F0} / {p.maxHp:F0}");
            DrawBar(new Rect(24, 70, 350, 14), p.CurrentStamina / p.maxStamina, new Color(0.95f, 0.78f, 0.18f),
                $"Stamina: {p.CurrentStamina:F0}");
        }

        // Progress perebutan village
        int cap = 0;
        for (int i = 0; i < _territories.Count; i++)
        {
            if (_territories[i] != null && _territories[i].IsCaptured) cap++;
        }
        GUI.Label(new Rect(24, 98, 350, 20), $"Villages Conquered: {cap} / {_territories.Count}", header);

        float y = 122;
        for (int i = 0; i < _territories.Count; i++)
        {
            TerritoryCaptureZone z = _territories[i];
            if (z == null) continue;
            string distStr = p != null ? $"{Vector3.Distance(p.transform.position, z.transform.position):F0}m" : "-";
            string state = z.IsCaptured ? "CAPTURED" : $"{distStr} ({z.AliveDefenderCount} guards)";
            GUI.Label(new Rect(28, y, 350, 18), $"• {z.territoryName}: {state}");
            y += 18;
        }

        DayNightCycleManager sky = DayNightCycleManager.Instance;
        if (sky != null)
        {
            GUI.Label(new Rect(24, 188, 355, 20), $"Waktu: {sky.FormattedClock} ({sky.CurrentPhaseLabel}) | [T] Ganti Waktu");
        }

        GUI.Label(new Rect(24, 214, 360, 20), "LClick: Combo | Q: Heavy | RClick: Dash | E: Ajak NPC | 1/2: Swap");
        GUI.Label(new Rect(24, 232, 360, 20), "Shift: Sprint | Space: Jump | R: Recall | T: Waktu | Tab: Cursor");
    }

    private void DrawCompanionsHud()
    {
        int recruited = VillageNPCController.RecruitedCount;
        if (recruited <= 0) return;

        float boxH = 34f + recruited * 18f;
        GUI.Box(new Rect(14, 268, 375, boxH), "");
        GUIStyle title = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 12 };
        title.normal.textColor = new Color(0.45f, 1f, 0.65f);
        GUI.Label(new Rect(24, 272, 355, 20), $"ALLY COMPANIONS ({recruited})", title);

        float y = 294f;
        for (int i = 0; i < VillageNPCController.AllNpcs.Count; i++)
        {
            VillageNPCController npc = VillageNPCController.AllNpcs[i];
            if (npc == null || !npc.IsRecruited) continue;
            int hpPct = Mathf.Clamp(Mathf.RoundToInt((npc.CurrentHp / Mathf.Max(1f, npc.maxHp)) * 100f), 0, 100);
            GUI.Label(new Rect(28, y, 350, 18), $"• {npc.displayName}: {hpPct}% ({npc.CurrentActivityText})");
            y += 18f;
        }
    }

    private void DrawInteractPrompt()
    {
        ThirdPersonPlayerController p = ThirdPersonPlayerController.Instance;
        if (p == null || p.IsDead) return;

        VillageNPCController nearNpc = VillageNPCController.FindNearestPromptTarget(p.transform.position, 3.8f);
        if (nearNpc == null) return;

        string text = nearNpc.IsRecruited
            ? $"[E] Pulangkan {nearNpc.displayName} ke desa"
            : $"[E] Ajak {nearNpc.displayName} bertarung bersama";

        float w = 360f, h = 36f;
        Rect r = new Rect((Screen.width - w) * 0.5f, Screen.height - 96f, w, h);
        GUI.Box(r, "");
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = new Color(1f, 0.92f, 0.45f);
        GUI.Label(r, text, style);
    }

    private void DrawDdaPcgTelemetryHud()
    {
        DDAManager dda = DDAManager.Instance;
        PCGWorldEcosystemManager pcg = PCGWorldEcosystemManager.Instance;
        if (dda == null) return;

        float x = Screen.width - 375f;
        GUI.Box(new Rect(x, 14, 360, 255), "");

        GUIStyle title = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 13 };
        title.normal.textColor = new Color(1f, 0.88f, 0.35f);
        GUI.Label(new Rect(x + 12, 20, 340, 22), "EAS LIVE TELEMETRY (DDA <-> PCG <-> AI)", title);

        GUI.Label(new Rect(x + 12, 44, 340, 20),
            $"Difficulty: {dda.difficultyIndex:F2} [{dda.CurrentTier}] | Perf: {dda.LastPerformanceScore:F2}");
        GUI.Label(new Rect(x + 12, 64, 340, 20),
            $"Accuracy: {(dda.Accuracy * 100f):F0}% | Active Enemies: {EnemyAIController.ActiveEnemies.Count}");

        GUI.Label(new Rect(x + 12, 88, 340, 20), "--- PCG Parameters (Adapted by DDA) ---");
        GUI.Label(new Rect(x + 12, 108, 340, 20),
            $"Forest Packs: {dda.ForestPackCount} (Max {dda.MaxMonstersPerPack}/pack) | Active: {(pcg != null ? pcg.ActiveForestMonsterCount : 0)}");
        GUI.Label(new Rect(x + 12, 128, 340, 20),
            $"Brute Ratio: {(dda.BruteRatio * 100f):F0}% | Ranged: {(dda.RangedRatio * 100f):F0}%");
        GUI.Label(new Rect(x + 12, 148, 340, 20),
            $"Heal Spawn: {dda.HealMushroomCount} | Ore Spawn: {dda.EnergyOreCount} | Respawn: {dda.RespawnInterval:F0}s");

        GUI.Label(new Rect(x + 12, 172, 340, 20), "--- Game AI Behavior (Adapted by DDA) ---");
        GUI.Label(new Rect(x + 12, 192, 340, 20),
            $"Sight: {dda.AISightRange:F1}m ({dda.AISightAngle:F0}°) | Atk CD: {dda.AIAttackCooldown:F2}s | Flee < {(dda.AIFleeHpThreshold * 100f):F0}%");
        GUI.Label(new Rect(x + 12, 212, 340, 20),
            $"Tactics (Flank & Cover): {(dda.AIUseTacticalBehavior ? "ACTIVE" : "OFF")}");
        GUI.Label(new Rect(x + 12, 232, 340, 20),
            $"Last: {dda.LastAdaptationReason}");
        GUI.Label(new Rect(x + 12, 250, 340, 20),
            "F1: AI Labels | F2: HUD | F3/F4: Demo Easy/Hard | F5: Regen PCG");
    }

    private void DrawVictoryBanner()
    {
        float w = 420f, h = 130f;
        Rect r = new Rect((Screen.width - w) * 0.5f, 80f, w, h);
        GUI.Box(r, "");
        GUIStyle big = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 20, alignment = TextAnchor.MiddleCenter };
        big.normal.textColor = new Color(0.35f, 1f, 0.6f);
        GUI.Label(new Rect(r.x, r.y + 16, w, 34), "ALL VILLAGES CONQUERED!", big);
        GUI.Label(new Rect(r.x + 20, r.y + 56, w - 40, 24),
            $"Clear Time: {(Time.time - _matchStartTime):F1}s | Press F6 to Restart", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
    }

    private static void DrawBar(Rect rect, float fill01, Color fillColor, string text)
    {
        Color prev = GUI.color;
        GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(rect.x + 1, rect.y + 1, Mathf.Max(0f, (rect.width - 2f) * Mathf.Clamp01(fill01)), rect.height - 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(rect.x + 6, rect.y - 1, rect.width - 12, rect.height + 2), text);
        GUI.color = prev;
    }
}
