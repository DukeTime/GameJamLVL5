using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;

    void Start()
    {
        SpawnEnemy();
    }

    public void SpawnEnemy()
    {
        int index = Random.Range(0, spawnPoints.Length);
        Instantiate(enemyPrefab, spawnPoints[index].position, Quaternion.identity);
    }
}
