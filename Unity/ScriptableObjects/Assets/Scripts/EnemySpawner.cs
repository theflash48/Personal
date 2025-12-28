using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public Scriptable_Enemy scriptableZombie;
    private GameObject prefabZombie;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        prefabZombie = scriptableZombie.prefab;
        Instantiate(prefabZombie, new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-10f, 10f)), Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
