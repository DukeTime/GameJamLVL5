using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// ===== ������������ ���������� �������� (���� ����������� ServiceLocator) =====
/// (����� �� �������������� � ��� ��� fallback'����� �� ������ ������)
public interface IVfxService : IService
{
    GameObject PlaySmoke(Vector3 position, float duration, GameObject overridePrefab = null);
}
public interface ILevelFlowService : IService
{
    void RequestAdvance(string nextSceneName);
    void ConfirmAdvance();
}

/// ===== �������� ���������� ���: �����, ����� �� �����, �����, ������� ��� ������� =====
[DisallowMultipleComponent]
public class EncounterController : MonoBehaviour
{
    // ---- ��������������� ��������� ������ ������ ����� ----
    [System.Serializable] public class EnemyPack { public GameObject prefab; [Min(1)] public int count = 1; }

    [System.Serializable]
    public class Wave
    {
        [Tooltip("������ �����: ����� ������� � �������.")]
        public List<EnemyPack> enemies = new List<EnemyPack>();
        [Tooltip("����� ����� ���������� ���� ����� (���).")] public float preWaveDelay = 0.8f;
        [Tooltip("�������� ����� ���������� ������ ����� (0 = �� �����).")] public float perSpawnDelay = 0.05f;
    }

    [System.Serializable]
    public class Hatch          // ���� ������
    {
        [Tooltip("�������� ������ ����. ��� �������� ���������� = ����� ������.")]
        public Transform root;
        [Tooltip("����������� ��������� �� ������ ��� �������� �����.")] public float minDistanceToPlayer = 4f;
        [Header("Telegraph")] public GameObject smokePrefab; public float telegraphDuration = 0.6f;

        // ��� �������� �����
        List<Transform> _points;
        public IReadOnlyList<Transform> Points
        {
            get
            {
                if (_points == null)
                {
                    _points = new List<Transform>();
                    if (root != null)
                    {
                        foreach (Transform ch in root) _points.Add(ch);
                    }
                }
                return _points;
            }
        }

        public Vector3 PickPointFarFrom(Vector3 playerPos)
        {
            var pts = Points;
            if (pts == null || pts.Count == 0) return root != null ? root.position : Vector3.zero;

            // ������� ����� �������� �� ���������
            for (int i = 0; i < 12; i++)
            {
                var p = pts[Random.Range(0, pts.Count)].position;
                if (Vector2.Distance(playerPos, p) >= minDistanceToPlayer) return p;
            }
            // fallback � ����� �������
            float best = -1f; Vector3 bestP = pts[0].position;
            foreach (var t in pts)
            {
                float d = Vector2.Distance(playerPos, t.position);
                if (d > best) { best = d; bestP = t.position; }
            }
            return bestP;
        }
    }

    // ---- ��������� ----
    [Header("Hatches & Player")]
    [Tooltip("���� ������. ���� ����� � ������ ��� ������� � ����� 'Hatch' � �� ����� ��� �����.")]
    public List<Hatch> hatches = new List<Hatch>();
    [Tooltip("����� (���� �� ����� � ����� �� ���� Player).")]
    public Transform player;

    [Header("Waves")]
    public List<Wave> waves = new List<Wave>();
    [Tooltip("����� ����� �������� ����� ����� ��������� (���).")]
    public float afterWaveDelay = 1.0f;
    [Tooltip("��������� ��������� � ������ ��� ��� ������?")]
    public bool avoidNearestHatchToPlayer = true;

    [Header("End of Encounter (statue exit)")]
    [Tooltip("������/������� ������. ��� ��� �������� ������� ����� ��������� �����.")]
    public Transform statue;
    public GameObject arrowPrefab;
    public Vector3 arrowOffset = new Vector3(0, 1.6f, 0);
    public float interactRadius = 1.5f;
    [Tooltip("��� ��������� �����. ������ ������, ���� ������� ���������� ���� ������ ����� ������.")]
    public string nextSceneName = "";

    public PlayerController playerController;
    public GameObject _playerObj;

    // ---- Runtime ----
    readonly List<GameObject> _spawnedThisWave = new();
    GameObject _arrowInstance;
    bool _exitActive;

    void Awake()
    {
        // ���� ����� �� ������ � ������� �� �������� � ����� "Hatch"
        if (hatches == null || hatches.Count == 0)
        {
            var hatchRoots = GameObject.FindGameObjectsWithTag("Hatch");
            hatches = new List<Hatch>(hatchRoots.Length);
            foreach (var hr in hatchRoots)
                hatches.Add(new Hatch { root = hr.transform, minDistanceToPlayer = 4f });
        }
    }

    void Start()
    {
        player = ServiceLocator.Current.Get<PlayerController>().gameObject.transform;
        
        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p) player = p.transform;
        }
        
        StartCoroutine(RunEncounter());
    }

    void Update()
    {
        // ��������� �������������� �� ������� (������� ��� ������������)
        if (_exitActive && statue != null && player != null)
        {
            if (Vector2.Distance(player.position, statue.position) <= interactRadius &&
                Input.GetKeyDown(KeyCode.E))
            {
                // ���� ���� ������ � ������ ����, ����� ������ �������� (���� ��� ����� �� �����)
                try
                {
                    var flow = ServiceLocator.Current.Get<ILevelFlowService>();
                    flow.RequestAdvance(nextSceneName);
                }
                catch
                {
                    if (!string.IsNullOrEmpty(nextSceneName))
                        SceneManager.LoadScene(nextSceneName);
                }
            }
        }
    }

    IEnumerator RunEncounter()
    {
        for (int w = 0; w < waves.Count; w++)
        {
            // �������� ����� ������
            if (w > 0 && afterWaveDelay > 0f) yield return new WaitForSeconds(afterWaveDelay);
            if (waves[w].preWaveDelay > 0f) yield return new WaitForSeconds(waves[w].preWaveDelay);

            _spawnedThisWave.Clear();
            yield return SpawnWave(waves[w]);

            // ����� ��������
            yield return WaitUntilWaveCleared();
        }

        // ����� ��� � �������� �������
        ActivateExitArrow();
    }

    IEnumerator SpawnWave(Wave wave)
    {
        if (hatches == null || hatches.Count == 0) yield break;

        // ��������� ��������� ���
        int nearest = -1;
        if (player != null && avoidNearestHatchToPlayer)
        {
            float best = float.MaxValue;
            for (int i = 0; i < hatches.Count; i++)
            {
                if (hatches[i].root == null) continue;
                float d = Vector2.Distance(player.position, hatches[i].root.position);
                if (d < best) { best = d; nearest = i; }
            }
        }

        var usable = new List<Hatch>(hatches.Count);
        for (int i = 0; i < hatches.Count; i++)
        {
            if (i == nearest && avoidNearestHatchToPlayer) continue;
            if (hatches[i].root != null) usable.Add(hatches[i]);
        }
        if (usable.Count == 0) usable.AddRange(hatches);

        foreach (var pack in wave.enemies)
        {
            if (pack.prefab == null || pack.count <= 0) continue;

            for (int i = 0; i < pack.count; i++)
            {
                var hatch = usable[Random.Range(0, usable.Count)];
                Vector3 pos = hatch.PickPointFarFrom(player != null ? player.position : Vector3.zero);

                // �������� (�����): ����� ������ ���� ����, ����� ��������
                bool usedService = false;
                if (hatch.telegraphDuration > 0f)
                {
                    try
                    {
                        var vfx = ServiceLocator.Current.Get<IVfxService>();
                        vfx.PlaySmoke(pos, hatch.telegraphDuration, hatch.smokePrefab);
                        usedService = true;
                    }
                    catch { /* ������� ��� � ���� */ }

                    if (!usedService && hatch.smokePrefab != null)
                    {
                        var smoke = Instantiate(hatch.smokePrefab, pos, Quaternion.identity);
                        Destroy(smoke, hatch.telegraphDuration + 0.25f);
                    }

                    yield return new WaitForSeconds(hatch.telegraphDuration);
                }

                var enemy = Instantiate(pack.prefab, pos, Quaternion.identity);
                _spawnedThisWave.Add(enemy);

                if (wave.perSpawnDelay > 0f)
                    yield return new WaitForSeconds(wave.perSpawnDelay);
            }
        }
    }

    IEnumerator WaitUntilWaveCleared()
    {
        while (true)
        {
            bool anyAlive = false;
            for (int i = _spawnedThisWave.Count - 1; i >= 0; i--)
            {
                if (_spawnedThisWave[i] == null) { _spawnedThisWave.RemoveAt(i); continue; }
                anyAlive = true;
            }
            if (!anyAlive) yield break;
            yield return null;
        }
    }

    void ActivateExitArrow()
    {
        if (statue == null) return;
        _exitActive = true;

        if (arrowPrefab != null && _arrowInstance == null)
        {
            _arrowInstance = Instantiate(arrowPrefab, statue.position + arrowOffset, Quaternion.identity);
            _arrowInstance.transform.SetParent(statue);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (statue != null)
        {
            Gizmos.color = new Color(1f, 0.8f, 0.1f, 0.35f);
            Gizmos.DrawWireSphere(statue.position, interactRadius);
        }

        if (hatches != null)
        {
            Gizmos.color = Color.magenta;
            foreach (var h in hatches)
            {
                if (h != null && h.root != null)
                    Gizmos.DrawLine(transform.position, h.root.position);
            }
        }
    }
#endif
}
