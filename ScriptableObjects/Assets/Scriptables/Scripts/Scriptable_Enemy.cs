using UnityEngine;

[CreateAssetMenu(fileName = "Scriptable_Enemy", menuName = "Scriptable Objects/Scriptable_Enemy")]
public class Scriptable_Enemy : ScriptableObject
{
    public string name;
    public Sprite sprite;
    public int health;
    public int maxHealth;
    public int damage;
    public float speed;
    public GameObject prefab;
    public string description;
}

