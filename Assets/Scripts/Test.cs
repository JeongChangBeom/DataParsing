using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Test : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private GameObject prefab;

    [Header("Test Option")]
    [SerializeField] private int prewarm = 100;
    [SerializeField] private int burstSpawnCount = 200;
    [SerializeField] private float spawnInterval = 0.05f;
    [SerializeField] private float lifeTime = 0.5f;

    [Header("Loop")]
    [SerializeField] private bool loop = true;
    [SerializeField] private float loopDelay = 1.0f;

    private readonly List<GameObject> _alive = new List<GameObject>();

    private int _spawnedTotal;
    private int _despawnedTotal;

    private void Start()
    {
        if (prefab == null)
        {
            Debug.LogError("[PoolTestRunner] Prefab is null.");
            return;
        }

        PoolManager.Instance.CreatePoolIfNeeded(prefab, prewarm, 0, true, null);

        StartCoroutine(TestLoop());
    }

    private IEnumerator TestLoop()
    {
        do
        {
            yield return StartCoroutine(BurstSpawnAndAutoDespawn());
            yield return new WaitForSeconds(loopDelay);

            PrintSummary();

        } while (loop == true);
    }

    private IEnumerator BurstSpawnAndAutoDespawn()
    {
        _alive.Clear();

        for (int i = 0; i < burstSpawnCount; i++)
        {
            Vector3 pos = transform.position + Random.insideUnitSphere * 3f;
            pos.y = 0f;

            GameObject go = PoolManager.Instance.Spawn(prefab, pos, Quaternion.identity, null);
            if (go != null)
            {
                _alive.Add(go);
                _spawnedTotal++;
                StartCoroutine(AutoDespawn(go, lifeTime));
            }

            if (spawnInterval > 0f)
            {
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        float wait = lifeTime + 0.2f;
        yield return new WaitForSeconds(wait);
    }

    private IEnumerator AutoDespawn(GameObject go, float t)
    {
        yield return new WaitForSeconds(t);

        if (go != null)
        {
            PoolManager.Instance.Despawn(go);
            _despawnedTotal++;
        }
    }

    private void PrintSummary()
    {
        int activeCount = CountActiveInScene(prefab.name);

        Debug.Log(
            $"[PoolTestRunner] SpawnedTotal={_spawnedTotal}, DespawnedTotal={_despawnedTotal}, " +
            $"ActiveLikePrefabName={activeCount}"
        );
    }

    private int CountActiveInScene(string nameContains)
    {
        GameObject[] all = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        int count = 0;
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];

            if (go.activeInHierarchy == false)
            {
                continue;
            }

            if (go.name.Contains(nameContains) == true)
            {
                count++;
            }
        }

        return count;
    }
}