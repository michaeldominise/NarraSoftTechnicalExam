using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class Spawner<T> where T : Component
{
    [SerializeField] protected Transform spawnParent;

    protected List<KeyValuePair<int, T>> spawnedList = new();
    protected List<T> activeList = new();
    
    public List<T> ActiveList => activeList;
    public Transform SpawnParent => spawnParent;
    public event Action<T> OnSpawned;

    /// <summary>
    /// Override to define custom spawn position logic.
    /// </summary>
    protected virtual Vector3 GetSpawnPoint() => Vector3.zero;

    /// <summary>
    /// Spawns an instance of the given prefab. Reuses inactive instances if available.
    /// </summary>
    public virtual T Spawn(T prefab, Func<T, bool> condition = null, Action<T> onInitialize = null)
    {
        if (!prefab)
        {
            Debug.LogWarning($"{nameof(Spawner<T>)}: Tried to spawn a null prefab.");
            return null;
        }

        // Try to find an inactive, reusable object that matches the prefab and condition
        var reusable = spawnedList.FirstOrDefault(x =>
            x.Key == prefab.GetInstanceID() &&
            !x.Value.gameObject.activeSelf &&
            (condition?.Invoke(x.Value) ?? true));

        var item = reusable.Value;

        if (!item)
        {
            // Instantiate a new object if no reusable one is found
            item = MonoBehaviour.Instantiate(prefab, spawnParent);
        }
        else
            spawnedList.Remove(reusable);

        // Activate and initialize
        item.gameObject.SetActive(true);
        item.transform.localPosition = GetSpawnPoint();
        spawnedList.Add(new(prefab.GetInstanceID(), item));
        activeList.Add(item);

        onInitialize?.Invoke(item);
        OnSpawned?.Invoke(item);

        return item;
    }

    /// <summary>
    /// Despawns an active item after an optional delay.
    /// </summary>
    public async void Despawn(T spawnedItem, float setInactiveDelay = 0f)
    {
        if (spawnedItem == null) return;

        activeList.Remove(spawnedItem);

        spawnedItem.transform.SetParent(spawnParent);

        if (setInactiveDelay > 0)
            await Task.Delay(TimeSpan.FromSeconds(setInactiveDelay));

        if (spawnedItem)
            spawnedItem.gameObject.SetActive(false);
    }

    /// <summary>
    /// Clears all active and spawned instances.
    /// </summary>
    public virtual void Clear()
    {
        activeList.Clear();

        foreach (var pair in spawnedList)
        {
            if (pair.Value)
            {
                pair.Value.transform.SetParent(spawnParent);
                pair.Value.gameObject.SetActive(false);
            }
        }
    }
}
