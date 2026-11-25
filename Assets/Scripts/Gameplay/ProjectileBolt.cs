using UnityEngine;

/// <summary>
/// Proyektil sihir untuk serangan jarak jauh pemain (Skill E) maupun Imp Caster AI.
/// </summary>
public class ProjectileBolt : MonoBehaviour
{
    public float speed = 18f;
    public float damage = 14f;
    public float lifeTime = 3.5f;
    public ElementType element = ElementType.Pyro;
    public bool fromPlayer;

    private Vector3 _dir;
    private float _elapsed;

    public void Launch(Vector3 direction, float dmg, ElementType elem, bool isFromPlayer, Color tint)
    {
        _dir = direction.normalized;
        damage = dmg;
        element = elem;
        fromPlayer = isFromPlayer;

        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            Shader urp = Shader.Find("Universal Render Pipeline/Lit");
            Material m = new Material(urp != null ? urp : Shader.Find("Standard"));
            m.color = tint;
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", tint * 2.5f);
            r.sharedMaterial = m;
        }
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        if (_elapsed >= lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        float step = speed * Time.deltaTime;
        if (Physics.Raycast(transform.position, _dir, out RaycastHit hit, step + 0.35f, ~0, QueryTriggerInteraction.Ignore))
        {
            HandleHit(hit.collider, hit.point);
            return;
        }

        transform.position += _dir * step;
    }

    private void HandleHit(Collider col, Vector3 point)
    {
        if (fromPlayer)
        {
            EnemyAIController enemy = col.GetComponentInParent<EnemyAIController>();
            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeDamage(damage, element, transform.position, true);
                if (DDAManager.Instance != null) DDAManager.Instance.RecordPlayerShot(hit: true);
                CombatFXHelper.SpawnBurstRing(point, 1.4f, new Color(1f, 0.5f, 0.1f), 0.25f);
                Destroy(gameObject);
                return;
            }
            if (col.GetComponentInParent<ThirdPersonPlayerController>() != null) return;
            if (DDAManager.Instance != null) DDAManager.Instance.RecordPlayerShot(hit: false);
        }
        else
        {
            ThirdPersonPlayerController player = col.GetComponentInParent<ThirdPersonPlayerController>();
            if (player != null && !player.IsDead)
            {
                player.TakeDamage(damage, element, transform.position, false);
                Destroy(gameObject);
                return;
            }
            VillageNPCController ally = col.GetComponentInParent<VillageNPCController>();
            if (ally != null)
            {
                if (ally.IsRecruited && !ally.IsDowned)
                {
                    ally.TakeDamage(damage, element, transform.position, false);
                    Destroy(gameObject);
                }
                return;
            }
            if (col.GetComponentInParent<EnemyAIController>() != null) return;
        }

        Destroy(gameObject);
    }
}
