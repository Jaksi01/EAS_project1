using UnityEngine;

/// <summary>
/// Item pickup hasil PCG (Jamur Heal & Ore Energy) yang merespons kedekatan pemain.
/// </summary>
public class PickupItem : MonoBehaviour
{
    public enum PickupType
    {
        HealMushroom,
        EnergyOre
    }

    public PickupType pickupType = PickupType.HealMushroom;
    public float amount = 25f;
    public float pickupRadius = 2.0f;

    private float _bobSeed;

    private void Start()
    {
        _bobSeed = Random.Range(0f, 6.28f);
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, 45f * Time.deltaTime, Space.World);
        float bob = Mathf.Sin(Time.time * 2.5f + _bobSeed) * 0.002f;
        transform.position += new Vector3(0f, bob, 0f);

        ThirdPersonPlayerController player = ThirdPersonPlayerController.Instance;
        if (player == null || player.IsDead) return;

        Vector3 a = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 b = new Vector3(player.transform.position.x, 0f, player.transform.position.z);
        if (Vector3.Distance(a, b) <= pickupRadius)
        {
            if (pickupType == PickupType.HealMushroom)
            {
                if (player.CurrentHp < player.maxHp)
                {
                    player.Heal(amount);
                    CombatFXHelper.SpawnFloatingText(transform.position, $"+{amount:F0} HP", Color.green, 0.9f);
                    Destroy(gameObject);
                }
            }
            else
            {
                player.GainEnergy(amount);
                CombatFXHelper.SpawnFloatingText(transform.position, $"+{amount:F0} ENERGY", new Color(0.3f, 0.85f, 1f), 0.9f);
                Destroy(gameObject);
            }
        }
    }
}
