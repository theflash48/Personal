using UnityEngine;

public class Zombie : MonoBehaviour
{
    [SerializeField] private Scriptable_Enemy data;
    [SerializeField] private int health;
    private int maxHealth;
    private int damage;
    private float speed;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        maxHealth = data.maxHealth;
        health = maxHealth;
        damage = data.damage;
        speed = data.speed;
        
        Instantiate(data.prefab, new Vector3(transform.position.x, transform.position.y, transform.position.z), Quaternion.identity);
        Destroy(gameObject);
    }
}
