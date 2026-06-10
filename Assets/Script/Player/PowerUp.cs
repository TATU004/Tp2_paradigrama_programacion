using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum Type { Shield, Heal, SuperBuff }
    public Type powerUpType;

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(gameObject.name + " 被物体撞击了，撞击者是: " + other.name + "，它的Tag是: " + other.tag);
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                switch (powerUpType)
                {
                    case Type.Shield: player.ActivateShield(6f); break;
                    case Type.Heal: player.Heal(20); break;
                    case Type.SuperBuff: player.ActivateSuperBuff(10f); break;
                }
                Destroy(gameObject);
            }
        }
    }
}