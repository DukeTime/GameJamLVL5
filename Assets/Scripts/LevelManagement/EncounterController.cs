using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// ===== Опциональные интерфейсы сервисов (если используешь ServiceLocator) =====
/// (Можно не регистрировать — код сам fallback'нется на прямые вызовы)
public interface IVfxService : IService
{
    GameObject PlaySmoke(Vector3 position, float duration, GameObject overridePrefab = null);
}
public interface ILevelFlowService : IService
{
    void RequestAdvance(string nextSceneName);
    void ConfirmAdvance();
}

/// ===== Основной контроллер боя: волны, спавн из люков, дымка, стрелка над статуей =====
[DisallowMultipleComponent]
public class EncounterController : MonoBehaviour
{
    // ---- Вспомогательные описатели внутри одного файла ----
    [System.Serializable] public class EnemyPack { public GameObject prefab; [Min(1)] public int count = 1; }

    [System.Serializable]
    public class Wave
    {
        [Tooltip("Состав волны: какие префабы и сколько.")]
        public List<EnemyPack> enemies = new List<EnemyPack>();
        [Tooltip("Пауза перед появлением ЭТОЙ волны (сек).")] public float preWaveDelay = 0.8f;
        [Tooltip("Интервал между инстансами внутри волны (0 = всё сразу).")] public float perSpawnDelay = 0.05f;
    }

    [System.Serializable]
    public class Hatch          // «Люк» спавна
    {
        [Tooltip("Корневой объект люка. Его дочерние трансформы = точки спавна.")]
        public Transform root;
        [Tooltip("Минимальная дистанция до игрока для валидной точки.")] public float minDistanceToPlayer = 4f;
        [Header("Telegraph")] public GameObject smokePrefab; public float telegraphDuration = 0.6f;

        // Кеш дочерних точек
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

            // попытки найти валидную по дистанции
            for (int i = 0; i < 12; i++)
            {
                var p = pts[Random.Range(0, pts.Count)].position;
                if (Vector2.Distance(playerPos, p) >= minDistanceToPlayer) return p;
            }
            // fallback — самая дальняя
            float best = -1f; Vector3 bestP = pts[0].position;
            foreach (var t in pts)
            {
                float d = Vector2.Distance(playerPos, t.position);
                if (d > best) { best = d; bestP = t.position; }
            }
            return bestP;
        }
    }

    // ---- Настройки ----
    [Header("Hatches & Player")]
    [Tooltip("Люки спавна. Если пусто — возьмём все объекты с тэгом 'Hatch' и их детей как точки.")]
    public List<Hatch> hatches = new List<Hatch>();
    [Tooltip("Игрок (если не задан — найдём по тегу Player).")]
    public Transform player;

    [Header("Waves")]
    public List<Wave> waves = new List<Wave>();
    [Tooltip("Пауза после зачистки волны перед следующей (сек).")]
    public float afterWaveDelay = 1.0f;
    [Tooltip("Исключать ближайший к игроку люк при спавне?")]
    public bool avoidNearestHatchToPlayer = true;

    [Header("End of Encounter (statue exit)")]
    [Tooltip("Статуя/триггер выхода. Над ней появится стрелка после последней волны.")]
    public Transform statue;
    public GameObject arrowPrefab;
    public Vector3 arrowOffset = new Vector3(0, 1.6f, 0);
    public float interactRadius = 1.5f;
    [Tooltip("Имя следующей сцены. Оставь пустым, если переход обработает твой диалог через сервис.")]
    public string nextSceneName = "";

    // ---- Runtime ----
    readonly List<GameObject> _spawnedThisWave = new();
    GameObject _arrowInstance;
    bool _exitActive;

    void Awake()
    {
        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p) player = p.transform;
        }

        // Если хэтчи не заданы — собрать из объектов с тегом "Hatch"
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
        StartCoroutine(RunEncounter());
    }

    void Update()
    {
        // обработка взаимодействия со статуей (стрелка уже активирована)
        if (_exitActive && statue != null && player != null)
        {
            if (Vector2.Distance(player.position, statue.position) <= interactRadius &&
                Input.GetKeyDown(KeyCode.E))
            {
                // Если есть сервис — отдаем туда, иначе грузим напрямую (если имя сцены не пусто)
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
            // задержки перед волной
            if (w > 0 && afterWaveDelay > 0f) yield return new WaitForSeconds(afterWaveDelay);
            if (waves[w].preWaveDelay > 0f) yield return new WaitForSeconds(waves[w].preWaveDelay);

            _spawnedThisWave.Clear();
            yield return SpawnWave(waves[w]);

            // ждать зачистки
            yield return WaitUntilWaveCleared();
        }

        // конец боя — включаем стрелку
        ActivateExitArrow();
    }

    IEnumerator SpawnWave(Wave wave)
    {
        if (hatches == null || hatches.Count == 0) yield break;

        // исключить ближайший люк
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

                // Телеграф (дымка): через сервис если есть, иначе напрямую
                bool usedService = false;
                if (hatch.telegraphDuration > 0f)
                {
                    try
                    {
                        var vfx = ServiceLocator.Current.Get<IVfxService>();
                        vfx.PlaySmoke(pos, hatch.telegraphDuration, hatch.smokePrefab);
                        usedService = true;
                    }
                    catch { /* сервиса нет — норм */ }

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
