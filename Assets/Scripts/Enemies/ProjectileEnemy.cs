using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class ProjectileEnemy : Enemy {

    [Header("References")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private GameObject projectileOrigin;
    private ProjectileManager projectileManager;
    private Coroutine reloadCoroutine;

    [Header("Settings")]
    [SerializeField][Min(0)] private float baseProjectileSpeed;
    [SerializeField][Min(1)] private float roundProjectileSpeedIncrement;
    [SerializeField][Min(0)] private float maxProjectileSpeed;
    [SerializeField] private bool infiniteAmmo;
    [SerializeField][Tooltip("Number of times enemy can fire before reload. Reload time set by exhaustionTime")][Range(0, 100)] private int maxAmmo;
    [SerializeField][Tooltip("Number of times to fire per attack. Must be less than ammo")][Min(1)] private int burst;
    [SerializeField][Tooltip("Delay between each time this enemy fires within one attack")][Min(0)] private float burstInterval;
    [SerializeField][Tooltip("Every time the enemy attacks, this many projectiles are fired, and 1 ammo is used. Min/max.")] private Vector2Int projectilesPerShot;
    [SerializeField][Tooltip("When using multiple projectiles, they will be fired within this arc (in degrees).")][Range(0, 180)] private float projectileArc;
    [SerializeField][Tooltip("Max unexpected angular offset in degrees")][Min(0)] private float bloom;
    [SerializeField][Tooltip("Set to false to only show while attack animation is playing, set to true to always show while in \"attacking mode\"")] private bool alwaysShowAttackingPopup;
    [SerializeField] private bool canMoveWhileReloading;
    [SerializeField] private bool canMoveWhileFiring;
    [SerializeField] private bool canAdjustAimDuringBurst;
    [SerializeField][Tooltip("If enabled, stunning this enemy while it is reloading causes it to finish reloading when stune is done. Otherwise, reload is paused during stun")] private bool stunCompletesReload;
    [SerializeField][Tooltip("ProjectileEnemy normally only exhausts to reload, but this bool additionaly enables the exhaustion behavior from MeleeEnemy")] private bool exhaustOnDodgeWindup;
    [SerializeField][Min(0)][Tooltip("First attack will be delayed by a value from 0 to this")] private float attackRandomnessRange;
    private float projectileSpeed;
    private int currentAmmo;

    private new void Start() {

        base.Start();

        projectileManager = FindAnyObjectByType<ProjectileManager>();

        projectileSpeed = Mathf.Min(baseProjectileSpeed + (roundProjectileSpeedIncrement * (GameData.GetRoundNumber() - 1)), maxProjectileSpeed); // projectile speed is capped at maxProjectileSpeed
        projectilePrefab.SetDamage(damage);
        projectilePrefab.SetSpeed(projectileSpeed);
        projectilePrefab.SetTargetLayer(Projectile.ProjectileTarget.Player);

        currentAmmo = maxAmmo;

    }

    private new void FixedUpdate() {

        base.FixedUpdate();

        // if player is in range and this enemy is not reloading, attempt to attack
        // ammo is maxAmmo, so either ammo has to be available or max ammo has to be 0 (meaning inf ammo)
        // additional stun check fixes an issue where the stun does not apply properly 
        // when hitting a reloading enemy and stunCompletesReload is enabled
        if (reloadCoroutine == null && enemyState != EnemyState.Stunned && isPlayerInRange)
            Attack();

    }

    protected override IEnumerator HandleAttack() {

        canAttack = false;
        enemyState = EnemyState.Attacking; // set enemy state to attacking
        bool attackPerformed = false;
        float animLength;

        if (alwaysShowAttackingPopup && currentAttackingPopup == null)
            currentAttackingPopup = popups.Play(attackingPopup, attackingPopupLocation, true);

        if (!hasDoneFirstAttack) {

            float elapsed = 0f;
            float totalWait = firstAttackDelay + Random.Range(0, attackRandomnessRange);

            while (elapsed < totalWait) {

                yield return new WaitForSeconds(Time.deltaTime);
                elapsed += Time.deltaTime;

                if (!isPlayerInRange)
                    break;

            }

            hasDoneFirstAttack = true;

        }

        // if player leaves range before the firstAttackDelay ends then dont do this 
        if (isPlayerInRange) {

            attackPerformed = true;

            aiPath.canMove = canMoveWhileFiring; // prevent enemy from moving while shooting
            // if alwaysShowAttackingPopup is true this wont be called
            if (currentAttackingPopup == null)
                currentAttackingPopup = popups.Play(attackingPopup, attackingPopupLocation, true);

            animLength = PlayAnimation(Animation.WindUp); // play wind up animation
            //transform.right = GetPlayerDirection();
            yield return new WaitForSeconds(animLength); // wait for wind up animation to finish

        }

        // if player is not in range then attack fails, if the behavior is enabled
        if (!isPlayerInRange && exhaustOnDodgeWindup) {

            // if player left range before firstAttackDelay ended then dont do this 
            if (attackPerformed) {

                enemyState = EnemyState.Exhausted; // set enemy state to exhausted
                PlayAnimation(Animation.Exhausted); // play exhausted animation

                yield return new WaitForSeconds(exhaustionTime); // wait for exhaustion time before allowing another attack

            }

            enemyState = EnemyState.Walking; // set enemy state back to walking
            aiPath.canMove = true; // allow enemy to move again since exhaustion is over
            PlayAnimation(Animation.Walk); // reset back to walk animation after exhaustion is over

            canAttack = true; // allow enemy to attack again after cooldown
            attackCoroutine = null;
            yield break; // exit coroutine

        }

        // fire

        // dir to player
        Vector3 playerDir = GetPlayerDirection();

        for (int b = 0; b < burst; b++) {

            if (canAdjustAimDuringBurst)
                playerDir = GetPlayerDirection();

            int projectilesToShoot = Random.Range(projectilesPerShot.x, projectilesPerShot.y + 1);

            for (int p = 0; p < projectilesToShoot; p++) {

                // calculate angle to rotate the direction vector by
                // bloom
                float t = bloom * Random.Range(-1f, 1f);
                // multi projectile scatter
                if (projectilesToShoot > 1)
                    t += ((projectileArc / 2) - (p * (projectileArc / (projectilesToShoot - 1))));
                // conversion from degrees
                t *= Mathf.Deg2Rad;

                // vector that points to player
                Vector3 projectileDir = playerDir;

                // rotate dir by t rads
                projectileDir = new Vector3(playerDir.x * Mathf.Cos(t) - playerDir.y * Mathf.Sin(t),
                              playerDir.x * Mathf.Sin(t) + playerDir.y * Mathf.Cos(t)).normalized;

                projectileManager.FireProjectile(projectilePrefab, projectileOrigin.transform.position, projectileDir);

            }

            currentAmmo--;
            soundManager.PlaySound(EnemySoundType.Attack); // play attack sound

            if (currentAmmo == 0 && !infiniteAmmo)
                break;

            yield return new WaitForSeconds(burstInterval);

        }


        // immediately start reloading if ammo is empty and not infinite
        if (currentAmmo == 0 && !infiniteAmmo)
            reloadCoroutine = StartCoroutine(Reload());

        if (!alwaysShowAttackingPopup && currentAttackingPopup != null) {

            currentAttackingPopup.Stop();
            currentAttackingPopup = null;

        }

        //transform.right = new Vector3(1, 0, 0);
        animLength = PlayAnimation(Animation.WindDown); // play wind down animation
        yield return new WaitForSeconds(animLength); // wait for wind down animation to finish
        PlayAnimation(Animation.Walk); // play walk animation after attack
        attackCooldownCoroutine = StartCoroutine(HandleAttackCooldown(successfulAttackCooldown)); // handle attack cooldown

        aiPath.canMove = reloadCoroutine == null; // allow enemy to move again if not reloading, no longer shooting

        attackCoroutine = null;

    }

    private IEnumerator Reload() {

        float elapsed = 0;

        if (currentAttackingPopup != null) {

            currentAttackingPopup.Stop();
            currentAttackingPopup = null;

        }

        aiPath.canMove = canMoveWhileReloading;

        while (elapsed < exhaustionTime) {

            yield return new WaitForEndOfFrame();

            // while enemy is stunned, reload is paused
            if (enemyState != EnemyState.Stunned) {

                enemyState = EnemyState.Exhausted;

                elapsed += Time.deltaTime;

                aiPath.canMove = canMoveWhileReloading;

                // stunning the enemy removes the exhaustion popup, but we need the exhaustion popup
                // to be visible throughout the entire reload
                if (currentExhaustedPopup == null)
                    currentExhaustedPopup = popups.Play(exhaustedPopup, exhaustedPopupLocation, true);

            } else if (stunCompletesReload) {

                elapsed = exhaustionTime; // end early

            }
        }

        // done
        if (enemyState != EnemyState.Stunned) { // aka, if not ended early 

            aiPath.canMove = true;
            enemyState = EnemyState.Walking;

        }

        currentAmmo = maxAmmo;
        reloadCoroutine = null;
        hasDoneFirstAttack = false;

        if (currentExhaustedPopup != null) {

            currentExhaustedPopup.Stop();
            currentExhaustedPopup = null;

        }
    }

    new void OnDrawGizmosSelected() {

        base.OnDrawGizmosSelected();

        // main firing arc
        // show gizmo to the right if in prefab preview or no player, points to player otherwise

        Gizmos.color = Color.blue;
        float t = (projectileArc / 2) * (Mathf.PI / 180);
        float x = 0;
        float y = 0;
        Vector3 upDir = Vector3.zero;
        Vector3 downDir = Vector3.zero;
        Vector2 playerDir = GetPlayerDirection();
        float vectorMagnitude = projectileSpeed;

#if UNITY_EDITOR
        if (EditorSceneManager.IsPreviewScene(gameObject.scene) || playerController == null) {

            x = Mathf.Cos(t);
            y = Mathf.Sin(t);
            upDir = new Vector3(x, y) * vectorMagnitude;
            downDir = new Vector3(x, -y) * vectorMagnitude;

            Gizmos.DrawRay(transform.position, upDir);
            Gizmos.DrawRay(transform.position, downDir);

        } else if (playerController != null) {

            upDir = new Vector3(playerDir.x * Mathf.Cos(t) - playerDir.y * Mathf.Sin(t),
              playerDir.x * Mathf.Sin(t) + playerDir.y * Mathf.Cos(t)).normalized * vectorMagnitude;
            downDir = new Vector3(playerDir.x * Mathf.Cos(-t) - playerDir.y * Mathf.Sin(-t),
              playerDir.x * Mathf.Sin(-t) + playerDir.y * Mathf.Cos(-t)).normalized * vectorMagnitude;

            Gizmos.DrawRay(transform.position, upDir);
            Gizmos.DrawRay(transform.position, downDir);

        }
#endif

        // with bloom firing arc
        t += (bloom * Mathf.PI / 180);
        Gizmos.color = Color.red;

#if UNITY_EDITOR
        if (bloom != 0 && (EditorSceneManager.IsPreviewScene(gameObject.scene) || playerController == null)) {

            x = Mathf.Cos(t);
            y = Mathf.Sin(t);
            upDir = new Vector3(x, y) * vectorMagnitude;
            downDir = new Vector3(x, -y) * vectorMagnitude;

            Gizmos.DrawRay(transform.position, upDir);
            Gizmos.DrawRay(transform.position, downDir);

        } else if (bloom != 0 && playerController != null) {

            upDir = new Vector3(playerDir.x * Mathf.Cos(t) - playerDir.y * Mathf.Sin(t),
              playerDir.x * Mathf.Sin(t) + playerDir.y * Mathf.Cos(t)).normalized * vectorMagnitude;
            downDir = new Vector3(playerDir.x * Mathf.Cos(-t) - playerDir.y * Mathf.Sin(-t),
              playerDir.x * Mathf.Sin(-t) + playerDir.y * Mathf.Cos(-t)).normalized * vectorMagnitude;

            Gizmos.DrawRay(transform.position, upDir);
            Gizmos.DrawRay(transform.position, downDir);

        }
#endif
    }

    private new void OnValidate() {

        base.OnValidate();

        float maxBloom = 90 - (projectileArc / 2);
        if (bloom > maxBloom)
            bloom = maxBloom;

        if (burst > maxAmmo && !infiniteAmmo)
            burst = maxAmmo;

        if (projectilesPerShot.x < 1)
            projectilesPerShot.x = 1;
        if (projectilesPerShot.y < 1)
            projectilesPerShot.y = 1;
        if (projectilesPerShot.y < projectilesPerShot.x)
            projectilesPerShot.y = projectilesPerShot.x;

    }
}
