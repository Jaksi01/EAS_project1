using UnityEngine;

public enum ElementType
{
    None,
    Pyro,
    Hydro,
    Electro
}

public interface IDamageable
{
    bool IsDead { get; }
    Transform Transform { get; }
    void TakeDamage(float amount, ElementType element, Vector3 hitSourcePos, bool isPlayerAttack = false);
}

/// <summary>
/// Utilitas sederhana untuk efek visual damage angka melayang dan reaksi elemen ala Genshin.
/// </summary>
public static class CombatFXHelper
{
    public static float EvaluateElementalReaction(
        ElementType currentAura,
        ElementType incoming,
        out string reactionLabel,
        out ElementType remainingAura)
    {
        reactionLabel = string.Empty;
        remainingAura = incoming != ElementType.None ? incoming : currentAura;

        if (currentAura == ElementType.None || incoming == ElementType.None || currentAura == incoming)
        {
            return 1f;
        }

        // Pyro + Hydro = Vaporize (1.75x)
        if ((currentAura == ElementType.Pyro && incoming == ElementType.Hydro) ||
            (currentAura == ElementType.Hydro && incoming == ElementType.Pyro))
        {
            reactionLabel = "VAPORIZE!";
            remainingAura = ElementType.None;
            return 1.75f;
        }

        // Electro + Pyro = Overloaded (1.5x + ledakan AoE di pemanggil)
        if ((currentAura == ElementType.Electro && incoming == ElementType.Pyro) ||
            (currentAura == ElementType.Pyro && incoming == ElementType.Electro))
        {
            reactionLabel = "OVERLOADED!";
            remainingAura = ElementType.None;
            return 1.5f;
        }

        // Electro + Hydro = Electro-Charged (1.35x)
        if ((currentAura == ElementType.Electro && incoming == ElementType.Hydro) ||
            (currentAura == ElementType.Hydro && incoming == ElementType.Electro))
        {
            reactionLabel = "ELECTRO-CHARGED!";
            remainingAura = ElementType.None;
            return 1.35f;
        }

        return 1f;
    }

    public static void SpawnFloatingText(Vector3 worldPos, string text, Color color, float scale = 1f)
    {
        GameObject go = new GameObject("FloatingDamageText");
        go.transform.position = worldPos + Vector3.up * 1.6f;
        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 36;
        tm.characterSize = 0.08f * scale;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = color;

        FloatingTextBehavior fx = go.AddComponent<FloatingTextBehavior>();
        fx.Init(color);
    }

    public static void SpawnBurstRing(Vector3 center, float radius, Color color, float duration = 0.45f)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "SkillRingFX";
        Object.Destroy(ring.GetComponent<Collider>());
        ring.transform.position = center + Vector3.up * 0.15f;
        ring.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);

        Renderer r = ring.GetComponent<Renderer>();
        if (r != null)
        {
            Shader urp = Shader.Find("Universal Render Pipeline/Lit");
            Material m = new Material(urp != null ? urp : Shader.Find("Standard"));
            m.color = color;
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color * 2f);
            r.sharedMaterial = m;
        }

        ExpandRingBehavior fx = ring.AddComponent<ExpandRingBehavior>();
        fx.Init(radius * 2f, duration);
    }
}

public class FloatingTextBehavior : MonoBehaviour
{
    private float _elapsed;
    private const float Duration = 0.9f;
    private TextMesh _tm;
    private Color _baseColor;

    public void Init(Color c)
    {
        _tm = GetComponent<TextMesh>();
        _baseColor = c;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        transform.position += Vector3.up * (1.2f * Time.deltaTime);
        if (Camera.main != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }
        if (_tm != null)
        {
            Color c = _baseColor;
            c.a = Mathf.Clamp01(1f - _elapsed / Duration);
            _tm.color = c;
        }
        if (_elapsed >= Duration) Destroy(gameObject);
    }
}

public class ExpandRingBehavior : MonoBehaviour
{
    private float _targetDiameter;
    private float _duration;
    private float _elapsed;

    public void Init(float targetDiameter, float duration)
    {
        _targetDiameter = targetDiameter;
        _duration = Mathf.Max(0.1f, duration);
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);
        float d = Mathf.Lerp(0.5f, _targetDiameter, t);
        transform.localScale = new Vector3(d, 0.04f, d);
        if (_elapsed >= _duration) Destroy(gameObject);
    }
}
