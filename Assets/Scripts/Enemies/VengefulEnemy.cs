using UnityEngine;

public class VengefulEnemy : MeleeEnemy {

    /// <summary>
    /// 
    /// Vengeful enemy is a melee enemy that spawns another enemy on death
    /// 
    /// </summary>

    [Header("Settings")]
    [SerializeField][Tooltip("Spawn this on death")] private Enemy deathSpawn;
    //[SerializeField][Tooltip("Spawn deathSpawn this many times")][Min(1)] private int numEnemiesToSpawn;
    //[SerializeField][Tooltip("Time to wait before first spawn")][Min(0)] private float firstSpawnDelay;
    //[SerializeField][Tooltip("Time between each spawn after the first")][Min(0.05f)] private float spawnInterval;
    bool hasSpawned = false;

    public new void Die() {

        if (!hasSpawned) {

            //StartCoroutine(HandleSpawning());

            roundManager.SpawnEnemy(deathSpawn, transform.position, Quaternion.identity);

            hasSpawned = true;

        }

        base.Die();

    }

    /*  private IEnumerator HandleSpawning() {

          base.Die();

          yield return new WaitForSeconds(firstSpawnDelay);

          for (int i = 0; i < numEnemiesToSpawn; i++) {

              Enemy spawned = Instantiate(deathSpawn, transform.position, Quaternion.identity);
              if (roundManager != null)
                  roundManager.LogSpawnedEnemy(spawned);

              yield return new WaitForSeconds(spawnInterval);

          }

      }*/

}
