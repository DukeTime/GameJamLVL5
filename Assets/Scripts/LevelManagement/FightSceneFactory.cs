using UnityEngine;


public class FightSceneFactory: MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    private GameObject _playerObj;
    
    private void Awake()
    {
        Instantiating();
        
        ServiceLocator.Initialize();
        ServiceLocator.Current.Register(_playerObj.GetComponent<PlayerController>());
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
        // спавн врагов на их спавнпоинтыы
    }
}