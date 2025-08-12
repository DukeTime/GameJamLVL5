using UnityEngine;


public class BaseSceneFactory: MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private string startTitle = "Game scene";
    [SerializeField] private int currentState = 1;
    
    private GameObject _playerObj;
    
    private void Awake()
    {
        Instantiating();
        
        ServiceLocator.Initialize();
        ServiceLocator.Current.Register(_playerObj.GetComponent<PlayerController>());
    }

    private void Start()
    {
        GlobalGameController.Instance.sceneProgress = currentState;
        
        StartCoroutine(ViewManager.Instance.ShowTitle(startTitle));
    }

    private void Instantiating()
    {
        InstantiatePlayer();
    }
    
    private void InstantiatePlayer()
    {
        Transform spawnPoint = GameObject.FindGameObjectWithTag("PlayerSpawnPoint").transform;
        
        _playerObj = Instantiate(playerController.gameObject, spawnPoint.position, Quaternion.identity);
    }
}