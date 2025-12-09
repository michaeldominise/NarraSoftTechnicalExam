using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SC_Spawner : MonoBehaviour
{
    public static SC_Spawner Instance { get; private set; }
    
    [SerializeField] private Spawner<Transform> bgTileSpawner;
    [SerializeField] private Spawner<SC_Gem> gemSpawner;
    [SerializeField] private Spawner<Transform> destroyEffectSpawner;

    private void Awake() => Instance = this;

    public Transform SpawnBGTile(Vector2Int position, Transform parent)
    {
        return bgTileSpawner.Spawn(SC_GameVariables.Instance.bgTilePrefabs.transform, onInitialize: _bgTile =>
        {
            _bgTile.transform.SetParent(parent);
            _bgTile.transform.localPosition = (Vector2)position;
            _bgTile.transform.localRotation = Quaternion.identity;
            _bgTile.name = "BG Tile - " + position.x + ", " + position.y;
        });
    }
    
    public SC_Gem SpawnGem(SC_Gem gemToSpawn, Vector2Int position, float spawnYPos, Transform parent)
    {
        return gemSpawner.Spawn(gemToSpawn, onInitialize: _gem =>
        {
            _gem.transform.SetParent(parent);
            _gem.transform.localPosition = new(position.x, spawnYPos);
            _gem.transform.localRotation = Quaternion.identity;
            _gem.name = "Gem - " + position.x + ", " + position.y;
            _gem.SetupGem(position);
        });
    }

    public Transform SpawnDestroyParticle(Transform destroyEffect, Vector2Int position)
    {
        return destroyEffectSpawner.Spawn(destroyEffect, 
            onInitialize: _effect =>
            {
                _effect.localPosition = (Vector2)position;
                _effect.localRotation = Quaternion.identity;
            });
    }
    
    public void DespawnGem(SC_Gem _GemToDespawn) => gemSpawner.Despawn(_GemToDespawn);
    public void DespawnBGTile(Transform bgTile) => bgTileSpawner.Despawn(bgTile);
    public void DespawnParticle(Transform particle) => destroyEffectSpawner.Despawn(particle);
}
