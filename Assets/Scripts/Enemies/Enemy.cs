using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Seeker))] // enemy must have a seeker component to pathfind
[RequireComponent(typeof(EnemySoundManager))] // enemy must have an enemysoundmanager component to enemy sound manage
[ExecuteAlways]
public abstract class Enemy : MonoBehaviour {

    [Header("References")]
    [SerializeField] private GameObject sprite;
    [SerializeField] private EnemyType enemyType;
    protected PlayerController playerController;
    protected EnemySoundManager soundManager;
    private Rigidbody2D rb;
    protected RoundManager roundManager;
    private Animator animator;
    protected PopupPlayer popups;
    protected Coroutine attackCooldownCoroutine;
    protected Coroutine attackCoroutine;
    private Coroutine hitInvulnerabilityCoroutine;
    private Coroutine kbCoroutine;
    private Coroutine stunCoroutine;
    private Coroutine rangeIndicatorCoroutine;

    [Header("Settings")]
    [SerializeField] private float baseDamage;
    [SerializeField] private float roundDamageIncrement; // this can be a float because the damage can be a float
    [SerializeField] private float baseMoveSpeed;
    [SerializeField] private float roundMoveSpeedIncrement; // this can be a float because the movement speed can be a float
    [SerializeField] private float maxMoveSpeed;
    [SerializeField][Tooltip("Time before next attack after enemy misses attack. Enemy movement also stops during this time")] protected float exhaustionTime;
    [SerializeField][Tooltip("Time between successful attacks")] protected float successfulAttackCooldown;
    [SerializeField][Tooltip("Time between getting in range and performing an attack")] protected float firstAttackDelay;
    [SerializeField][Tooltip("Range which player must be within enemy of in order for enemy to attack")] private float engageRange;
    [SerializeField][Tooltip("Range which target must leave for enemy to stop attacking. Must be greater than engage range")] private float disengageRange;
    [SerializeField] private bool stunResetsAttackCooldown;
    [SerializeField] private bool startFacingLeft;
    [SerializeField][Tooltip("Enemy will stop moving once player is in range of them")] private bool freezeInRange;
    private float moveSpeed;
    protected float damage;
    protected bool isPlayerInRange;
    protected bool canAttack;
    private float remainingAttackCooldown;
    bool isFlipped;

    [Header("Invulnerability")]
    [SerializeField][Min(0f)] private float hitInvulDuration;
    [SerializeField][Range(0f, 1f)] private float invulAnimMaxAlpha;
    [SerializeField][Range(0f, 1f)] private float invulAnimMinAlpha;
    [SerializeField][Min(0f)] private int invulAnimNumFlashes;
    protected bool invulnerable;

    [Header("Drops")]
    [SerializeField] private DroppedFood droppedFoodPrefab;
    [SerializeField] private FoodData droppedFoodData;
    [SerializeField] private float dropChance;

    [Header("State")]
    protected EnemyState enemyState;
    protected bool hasDoneFirstAttack;
    private Queue<Coroutine> slowQueue;

    [Header("Health")]
    [SerializeField] private float baseMaxHealth;
    [SerializeField] private float roundMaxHealthIncrement;
    protected float maxHealth;
    private float health;

    [Header("Pathfinding")]
    protected AIPath aiPath;
    private AIDestinationSetter destinationSetter;

    [Header("Popups")]
    [SerializeField] private GameObject stunPopupLocation;
    [SerializeField] private Popup stunPopup;
    [SerializeField] protected GameObject attackingPopupLocation;
    [SerializeField] protected Popup attackingPopup;
    [SerializeField] protected GameObject exhaustedPopupLocation;
    [SerializeField] private Popup slowPopup;
    [SerializeField] private GameObject slowPopupLocation;
    [SerializeField] protected Popup exhaustedPopup;
    [SerializeField] private Popup deathPopup;
    protected Popup currentAttackingPopup;
    protected Popup currentExhaustedPopup;
    private Popup currentSlowPopup;
    // TODO: currentStunPopup

    // FOR THIS ARRAY:
    // the order of the Animation enums should match the order of the elements in here, so the indices match
    // the order of the parameters in the animation should also match this array
    // ex: Animation.Walk = 0
    //     "Walk" index = 0,
    //     animator param walkSpeed index = 0
    // just make sure the animator param order matches, other two are coded into here
    private readonly string[] animStates = new string[] { "Walk", "WindUp", "WindDown", "Stunned", "Exhausted", "Dying" };

    public enum EnemyState {

        Walking,
        Attacking,
        Stunned,
        Exhausted,
        Dying

    }

    protected void Start() {

        playerController = FindFirstObjectByType<PlayerController>();
        soundManager = GetComponentInChildren<EnemySoundManager>();
        roundManager = FindFirstObjectByType<RoundManager>();
        destinationSetter = GetComponent<AIDestinationSetter>();
        aiPath = GetComponent<AIPath>();
        rb = GetComponent<Rigidbody2D>();
        animator = sprite.GetComponent<Animator>();
        popups = FindFirstObjectByType<PopupPlayer>();
        slowQueue = new Queue<Coroutine>();

        // force all enemies to face right at the start
        if (startFacingLeft)
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);

        // make sure the disengage range is greater than the engage range
        if (disengageRange < engageRange)
            Debug.LogError(gameObject.name + ": Enemy disengage range must be greater than engage range.");

        destinationSetter.target = playerController.transform;

        moveSpeed = Mathf.Min(baseMoveSpeed + roundMoveSpeedIncrement * (GameData.GetRoundNumber() - 1), maxMoveSpeed); // enemy move speed is capped at maxMoveSpeed

        damage = baseDamage + roundDamageIncrement * (GameData.GetRoundNumber() - 1); // set damage based on round number

        maxHealth = (int) (baseMaxHealth + roundMaxHealthIncrement * (GameData.GetRoundNumber() - 1)); // set max health based on round number; truncate to integer
        SetHealth(maxHealth); // set health to max health

        //hitInvulnerabilityCoroutine = StartCoroutine(HandleHitInvulnerability());

        canAttack = true; // allow the enemy to attack by default

        aiPath.maxSpeed = moveSpeed;

        //ResetAnimationFps();

    }

    protected void Update() {

        // make sure the enemy faces the player
        if (playerController != null && enemyState != EnemyState.Exhausted && enemyState != EnemyState.Stunned) { // don't flip when stunned or exhausted

            float x = sprite.transform.localScale.x;

            if (!isFlipped && playerController.transform.position.x < transform.position.x) { // player is to the left of enemy, so flip to right

                sprite.transform.localScale = new Vector3(-x, sprite.transform.localScale.y, sprite.transform.localScale.z);
                isFlipped = true;

            } else if (isFlipped && playerController.transform.position.x > transform.position.x) { // player is to the right of enemy, flip back to normal (facing right)

                sprite.transform.localScale = new Vector3(-x, sprite.transform.localScale.y, sprite.transform.localScale.z);
                isFlipped = false;

            }
        }
    }

    // this method determines if the enemy is in range of the player
    // if yes, stop. enemies will probably want to attack when isPlayerInRange == true
    protected void FixedUpdate() {

        float dist = GetPlayerDistance();

        // player enters range, enemy is now attacking them
        if (dist < engageRange && !isPlayerInRange) {

            isPlayerInRange = true;
            hasDoneFirstAttack = false;

            if (freezeInRange)
                aiPath.canMove = false;

        } else if (isPlayerInRange && dist > disengageRange) { // player was in enemy's range but has left the disengage range. start chasing again

            isPlayerInRange = false;

            if (freezeInRange)
                aiPath.canMove = true;

            // hide (pool) attacking popup
            if (currentAttackingPopup != null) {

                currentAttackingPopup.Stop();
                currentAttackingPopup = null;

            }
        }
    }

    protected void OnCollisionEnter2D(Collision2D collision) {

        if (kbCoroutine != null) {

            // stop knockback coroutine
            StopCoroutine(kbCoroutine);
            kbCoroutine = null;
            rb.linearVelocity = Vector2.zero;
            // do not re enable pathfinding here in case the enemy is stunned

        }
    }

    #region Behavior
    protected void Attack() {

        if (!canAttack) return;

        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
        attackCoroutine = StartCoroutine(HandleAttack());

    }

    protected abstract IEnumerator HandleAttack();

    protected IEnumerator HandleAttackCooldown(float duration) {

        remainingAttackCooldown = duration;

        while (remainingAttackCooldown > 0f) {

            remainingAttackCooldown -= Time.deltaTime;
            yield return null;

        }

        remainingAttackCooldown = 0f;
        enemyState = EnemyState.Walking; // set enemy state to walking
        canAttack = true;
        attackCooldownCoroutine = null;

    }

    // applies a force in a given direction using fun kinematics!
    // provide it with initial velocity (magnitude) of the push and distance to knock back. calculates for force required
    // provide a Vector3 direction or it will default to moving directly away from the player
    // enemy cannot do anything while in knockback. attack cooldown resets at the end of kb
    // please pass in positive values for initialVel and dist
    // thank you, AP Physics C: Mechanics!!!!!!!!!!! although i hate you sometimes, you are useful af </3
    public void ApplyKnockback(float initialVel, float dist, Vector2 dir) {

        dir = dir.normalized;
        // you can adjust the enemy's mass a bit to make it feel a bit different to hit 
        dist /= rb.mass;
        initialVel /= Mathf.Sqrt(rb.mass); // don't ask
        // (in case you asked)
        // the effect of mass on initialVel is increased when m<0 and decreased when m>0 

        float deceleration = -Mathf.Pow(initialVel, 2f) / (2f * dist); // a = (-Vo^2)/2d

        //float travelTime = 2 * dist / initialVel; // t = mVo/F = 2d/Vo 

        if (kbCoroutine != null) StopCoroutine(kbCoroutine);
        kbCoroutine = StartCoroutine(KnockbackTravel(initialVel, dist, deceleration, dir));

    }

    public void ApplyKnockback(float initialVel, float dist) => ApplyKnockback(initialVel, dist, -GetPlayerDirection());

    private IEnumerator KnockbackTravel(float initialVel, float dist, float deceleration, Vector2 dir) {

        float travelDist = 0f;
        float currVel = initialVel;

        deceleration = Mathf.Abs(deceleration); // needs to be positive for the calculations

        float kickOutDuration = initialVel / deceleration;
        float elapsed = 0;
        Stun(kickOutDuration);

        while (travelDist < dist && elapsed < kickOutDuration) {

            float updVel = deceleration * Time.fixedDeltaTime;

            currVel = Mathf.Max(currVel - updVel, 0f);

            Vector2 displacement = Time.fixedDeltaTime * currVel * dir;
            rb.MovePosition(rb.position + displacement);

            travelDist += displacement.magnitude;

            yield return new WaitForFixedUpdate();

            elapsed += Time.fixedDeltaTime;

        }

        rb.linearVelocity = Vector2.zero;
        kbCoroutine = null;

    }

    // stun this enemy, cancelling the current attack and preventing movement/attacking
    public void Stun(float duration) {

        // validate the duration
        if (duration < 0f || duration == float.NaN) return;

        enemyState = EnemyState.Stunned; // set enemy state to stunned

        if (currentAttackingPopup != null) {

            currentAttackingPopup.Stop();
            currentAttackingPopup = null;

        }

        if (currentExhaustedPopup != null) {

            currentExhaustedPopup.Stop();
            currentExhaustedPopup = null;

        }

        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        stunCoroutine = StartCoroutine(ApplyStun(duration));

        // handle existing attack cooldowns
        if (attackCooldownCoroutine != null) {

            if (stunResetsAttackCooldown) {

                // clear existing cooldown then start it over entirely
                StopCoroutine(attackCooldownCoroutine);
                attackCooldownCoroutine = StartCoroutine(HandleAttackCooldown(duration));

            } else {

                // clear existing cooldown then start it over from where it left off prior to the stun
                StopCoroutine(attackCooldownCoroutine);
                attackCooldownCoroutine = StartCoroutine(HandleAttackCooldown(remainingAttackCooldown));

            }
        }

        if (attackCoroutine != null) {

            StopCoroutine(attackCoroutine);
            attackCoroutine = null;

        }
    }

    // stun and wait for time then unstun the enemy.
    // pauses animation during stun and resumes them after
    private IEnumerator ApplyStun(float duration) {

        aiPath.canMove = false; // stop pathfinding
        canAttack = false;
        PlayAnimation(Animation.Stunned); // play stunned animation
        popups.Play(stunPopup, stunPopupLocation, duration);

        float currentTime = 0f;

        while (currentTime < duration) {

            enemyState = EnemyState.Stunned; // set enemy state to stunned each frame to ensure it remains in the stunned state
            currentTime += Time.deltaTime;
            yield return null;

        }

        PlayAnimation(Animation.Walk); // reset back to walk animation after stun ends
        enemyState = EnemyState.Walking; // set enemy state to walking
        aiPath.canMove = true; // resume pathfinding
        stunCoroutine = null;
        canAttack = true;

    }

    // slow the enemy. amount is a percentage from 0 to 1 (0% to 100%)
    // if slow = 0.2 then enemy speed will be 0.8x for the duration
    // can't input 1 or there will be a divide by zero in HandleSlow
    public void ApplySlow(float reduction, float duration) {

        if (reduction > 0.999f || reduction < 0f) {
            Debug.LogWarning("Invalid slow amount! Please insert a value from 0 to 0.999.");
            return;
        }

        // this is being played as a persistent popup instead of with a fixed duration to
        // accomodate for when multiple slows are applied
        if (currentSlowPopup == null)
            currentSlowPopup = popups.Play(slowPopup, slowPopupLocation, true);

        // track all active slows to know when to hide the popup (since you can stack slows)
        slowQueue.Enqueue(StartCoroutine(HandleSlow(reduction, duration)));

    }

    private IEnumerator HandleSlow(float reduction, float duration) {

        moveSpeed *= (1f - reduction);
        aiPath.maxSpeed = moveSpeed;

        float elapsed = 0f;

        while (elapsed < duration) {

            elapsed += Time.deltaTime;
            yield return new WaitForEndOfFrame();

        }

        moveSpeed /= (1 - reduction);
        aiPath.maxSpeed = moveSpeed;

        // only hide the slow thing if there is no active slow
        if (slowQueue.Count != 0)
            slowQueue.Dequeue();

        if (currentSlowPopup != null && slowQueue.Count == 0) {

            currentSlowPopup.Stop();
            currentSlowPopup = null;

        }
    }

    public IEnumerator HandleHitInvulnerability() {

        SpriteRenderer spriteRenderer = sprite.GetComponent<SpriteRenderer>();

        invulnerable = true;

        float flashDuration = hitInvulDuration / invulAnimNumFlashes;
        Color start = spriteRenderer.color;
        // max alpha
        Color peak = new Color(start.r, start.g, start.b, invulAnimMaxAlpha);
        // min alpha
        Color trough = new Color(start.r, start.g, start.b, invulAnimMinAlpha);

        // flash the alpha value
        for (int i = 0; i < invulAnimNumFlashes; i++) {

            float elapsed = 0f;
            float time; // smoothening

            // lerp alpha from max to min
            while (elapsed < flashDuration / 2f) {

                time = elapsed / (flashDuration / 2f);
                time = 1f - Mathf.Cos(time * Mathf.PI * 0.5f); // ease in
                spriteRenderer.color = Color.Lerp(peak, trough, time);
                elapsed += Time.deltaTime;
                yield return new WaitForEndOfFrame();

            }

            spriteRenderer.color = peak;

            // lerp alpha from min to max
            elapsed = 0;

            while (elapsed < flashDuration / 2f) {

                time = elapsed / (flashDuration / 2f);
                time = Mathf.Sin(time * Mathf.PI * 0.5f); // ease out
                spriteRenderer.color = Color.Lerp(trough, peak, time);
                elapsed += Time.deltaTime;
                yield return new WaitForEndOfFrame();

            }

            spriteRenderer.color = trough;

        }

        spriteRenderer.color = start;
        invulnerable = false;
        hitInvulnerabilityCoroutine = null;

    }

    private void SpawnDrop() => Instantiate(droppedFoodPrefab, transform.position, Quaternion.identity).Initialize(droppedFoodData); // spawn scrap drops and initialize them
    #endregion

    #region Health

    // can be private because health is only modified by the enemy itself (for example taking damage; this could be changed later)
    private void SetHealth(float health) {

        this.health = health;
        //uiController.UpdateHealth();

    }

    // can be private because health is only modified by the enemy itself (for example taking damage; this could be changed later)
    private void AddHealth(float health) {

        this.health += health;
        //uiController.UpdateHealth();

    }

    // can be private because health is only modified by the enemy itself (for example taking damage; this could be changed later)
    private void RemoveHealth(float health) {

        this.health -= health;
        //uiController.UpdateHealth();

        if (this.health <= 0) {

            if (this is VengefulEnemy)
                ((VengefulEnemy) this).Die();
            else
                Die();

        }

    }

    public void TakeDamage(float damage, bool playSound, bool doInvulnerability) {

        // invulnerability check is performed in WeaponManager
        RemoveHealth(damage);

        if (health > 0) {

            if (playSound)
                soundManager.PlaySound(EnemySoundType.Damaged);

            // reset hit invulnerability
            if (doInvulnerability) {

                if (hitInvulnerabilityCoroutine != null) StopCoroutine(hitInvulnerabilityCoroutine);
                hitInvulnerabilityCoroutine = StartCoroutine(HandleHitInvulnerability());

            }
        }


    }

    public void TakeDamage(float damage) => TakeDamage(damage, true, true);

    public float CurrentHealth() => health;

    // called once this enemy is out of health or dies in some other way
    public void Die() {

        canAttack = false;

        // stop any active coroutines
        if (hitInvulnerabilityCoroutine != null) {

            StopCoroutine(hitInvulnerabilityCoroutine);
            hitInvulnerabilityCoroutine = null;

        }

        if (stunCoroutine != null) {

            StopCoroutine(stunCoroutine);
            stunCoroutine = null;

        }

        if (kbCoroutine != null) {

            StopCoroutine(kbCoroutine);
            kbCoroutine = null;

        }

        if (rangeIndicatorCoroutine != null) {

            StopCoroutine(rangeIndicatorCoroutine);
            rangeIndicatorCoroutine = null;

        }

        if (attackCooldownCoroutine != null) {

            StopCoroutine(attackCooldownCoroutine);
            attackCooldownCoroutine = null;

        }

        if (currentAttackingPopup != null) {

            currentAttackingPopup.Stop();
            currentAttackingPopup = null;

        }

        if (currentExhaustedPopup != null) {

            currentExhaustedPopup.Stop();
            currentExhaustedPopup = null;

        }

        soundManager.PlaySound(EnemySoundType.Death);
        StartCoroutine(HandleDeath());

    }
    #endregion

    #region Animation
    // the different animations all Enemies can play
    // order should match those in the animStates array (indices correspond). just dont touch the order
    // ^^^^ if above is not true then PlayAnimation will also not work
    protected enum Animation {

        Walk,
        WindUp,
        WindDown,
        Stunned,
        Exhausted

    }

    // begins playing an animation clip. returns the length of the clip
    protected float PlayAnimation(Animation clip) {

        int clipIdx = (int) clip; // used to access clip and other info
        string clipToPlay = animStates[clipIdx]; // name of the clip to be played
        AnimationClip[] anims = animator.runtimeAnimatorController.animationClips; // all anims on this animator

        bool animatorHasAnim = false;

        // make sure the animator has the animation we're trying to play
        foreach (AnimationClip anim in anims) {

            // store if the animator contains an animation clip
            // checked by name. ex: anything with Walk in it triggers this to be true if searching for Walk (anim 0)
            animatorHasAnim = anim.name.Contains(clipToPlay);

            if (animatorHasAnim)
                break;

        }

        // modulate it if needed then play it
        if (animatorHasAnim) {

            // get clip that is about to play
            // nowPlaying is just used to get info about the clip
            AnimationClip nowPlaying = anims[clipIdx];

            // get the speed parameter for the clip
            int paramHash = animator.GetParameter(clipIdx).nameHash;
            float nowPlayingSpeed = animator.GetFloat(paramHash);

            //ResetAnimationFps(clipIdx); // allows for the speed of the animation to change while the game is running

            animator.Play(clipToPlay); // play after adjusting time

            return nowPlaying.length / nowPlayingSpeed; // return animation duration with speed factored out

        } else {

            Debug.Log("Animation " + clipToPlay + " is not defined for " + name + "! Add the animation to its animator or make sure all names are correct.");

        }

        return 0f;

    }

    // set all animations to what their FPS should be (base FPS with speed factored in)
    // FPS is divided by clip speed to maintain consitency across all animations
    // so that they all look like 24fps
    private void ResetAnimationFps() {

        AnimationClip[] anims = animator.runtimeAnimatorController.animationClips;
        AnimationClip anim;

        float clipSpeed;

        for (int i = 0; i < anims.Length; i++) {

            anim = anims[i];
            clipSpeed = animator.GetParameter(i).defaultFloat;

            if (anim.frameRate == GameSettings.ANIMATION_FPS && clipSpeed != 1f)
                anim.frameRate = GameSettings.ANIMATION_FPS / clipSpeed;
            else if (anim.frameRate != GameSettings.ANIMATION_FPS && clipSpeed == 1f)
                anim.frameRate = GameSettings.ANIMATION_FPS;

        }
    }

    // reset the FPS of a specific animation to what it should be 
    // see method above for more info
    private void ResetAnimationFps(int idx) {

        AnimationClip anim = animator.runtimeAnimatorController.animationClips[idx];
        float clipSpeed = animator.GetParameter(idx).defaultFloat;

        if (anim.frameRate == GameSettings.ANIMATION_FPS && clipSpeed != 1f)
            anim.frameRate = GameSettings.ANIMATION_FPS / clipSpeed;
        else if (anim.frameRate != GameSettings.ANIMATION_FPS && clipSpeed == 1f)
            anim.frameRate = GameSettings.ANIMATION_FPS;

    }

    private IEnumerator HandleDeath() {

        aiPath.canMove = false;

        float elapsed = 0f;
        float duration = 0.4f;
        float t = 0f;
        SpriteRenderer spriteRenderer = sprite.GetComponent<SpriteRenderer>();

        // sad hardcoding
        Color startColor = spriteRenderer.color;
        //Color endColor = new Color(0.6f, 0, 0, 0.5f);
        Color endColor = new Color(0.31f, 0.17f, 0.58f, 0.5f);
        Quaternion startRot = sprite.transform.rotation;
        Quaternion endRot = Quaternion.Euler(0, 0, -80);

        while (elapsed <= duration) {

            // ease out
            t = elapsed / duration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f);

            spriteRenderer.color = Color.Lerp(startColor, endColor, t);
            sprite.transform.rotation = Quaternion.Lerp(startRot, endRot, t);

            yield return new WaitForEndOfFrame();
            elapsed += Time.deltaTime;

        }

        duration = 0.08f;
        elapsed = 0;
        startColor = endColor;
        endColor = new Color(0.376f, 0.271f, 0.352f, 0f);

        while (elapsed <= duration) {

            // linear
            t = elapsed / duration;

            spriteRenderer.color = Color.Lerp(startColor, endColor, t);

            yield return new WaitForEndOfFrame();
            elapsed += Time.deltaTime;

        }

        if (Random.Range(0f, 100f) <= dropChance)
            SpawnDrop();

        popups.Play(deathPopup, transform.position, false);

        if (currentAttackingPopup != null) {

            currentAttackingPopup.Stop();
            currentAttackingPopup = null;

        }

        if (currentExhaustedPopup != null) {

            currentExhaustedPopup.Stop();
            currentExhaustedPopup = null;

        }

        if (currentSlowPopup != null) {

            currentSlowPopup.Stop();
            currentSlowPopup = null;

        }

        Destroy(gameObject);

        if (roundManager != null)
            roundManager.OnEnemyDeath(this);
        //soundManager.PlaySound(EnemySoundType.Death); // TODO: play death sound



    }
    #endregion

    #region Util
    // returns the distance from this enemy to the player
    protected float GetPlayerDistance() => Vector3.Distance(playerController.transform.position, transform.position);

    // returns a unit vector that points at the player
    protected Vector2 GetPlayerDirection() => (playerController.gameObject.transform.position - transform.position).normalized;

    public bool IsInvulnerable() => invulnerable;

    protected void OnDrawGizmosSelected() {

        // disengage radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, disengageRange);

        // attack radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, engageRange);

    }

    protected void OnValidate() {

        /*if (disengageRange <= engageRange)
            disengageRange = engageRange;*/

    }
    #endregion

    #region Accessors
    public EnemyType GetEnemyType() => enemyType;

    public EnemyState GetState() => enemyState;
    #endregion
}
