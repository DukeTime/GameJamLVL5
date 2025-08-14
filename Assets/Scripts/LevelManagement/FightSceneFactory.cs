using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;


public class FightSceneFactory: MonoBehaviour
{
    public bool resetStats = false;
    
    [SerializeField] private PlayerController playerController;
    private GameObject _playerObj;
    
    [SerializeField] private GameObject[] enemiesPrefs;
    private List<GameObject> _enemiesObjs = new List<GameObject>();
    
    private void Awake()
    {
        Instantiating();
        
        ServiceLocator.Initialize();
        ServiceLocator.Current.Register(_playerObj.GetComponent<PlayerController>());
    }

    private void Start()
    {
        if (resetStats)
        {
            PlayerStats.ResetStats();
            RuneManager.ResetExcludedRunes();
            playerController.Data.UpdateStats();
        }
    }

    private void Instantiating()
    {
        InstantiatePlayer();   
        InstantiateEnemies();
    }
    
    private void InstantiatePlayer()
    {
        Transform spawnPoint = GameObject.FindGameObjectWithTag("PlayerSpawnPoint").transform;
        
        _playerObj = Instantiate(playerController.gameObject, spawnPoint.position, Quaternion.identity);
    }
    
    private void InstantiateEnemies()
    {
        List<GameObject> spawnPoints = new List<GameObject>(GameObject.FindGameObjectsWithTag("EnemySpawnPoint"));


        foreach (GameObject enemy in enemiesPrefs)
        {
            int randomIndex = Random.Range(0, spawnPoints.Count);
            Transform spawnPoint = spawnPoints[randomIndex].transform; 
            spawnPoints.RemoveAt(randomIndex);
            
            _enemiesObjs.Add(Instantiate(enemy, spawnPoint.position, Quaternion.identity));
        }
    }
}