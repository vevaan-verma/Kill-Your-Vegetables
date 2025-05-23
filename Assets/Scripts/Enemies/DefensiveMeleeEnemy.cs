using System.Collections;
using UnityEngine;

public class DefensiveMeleeEnemy : Enemy {

    private new void FixedUpdate() {

        base.FixedUpdate();

        // if player is in range, attack  
        if (isPlayerInRange)
            Attack();

    }

    protected override IEnumerator HandleAttack() {

        canAttack = false;
        enemyState = EnemyState.Attacking; // set enemy state to attacking  
        bool attackPerformed = false;
        float animLength;

        if (currentAttackingPopup == null && enemyState != EnemyState.Exhausted)
            currentAttackingPopup = popups.Play(attackingPopup, attackingPopupLocation, true);

        if (!hasDoneFirstAttack) {

            hasDoneFirstAttack = true;

            float elapsed = 0f;

            while (elapsed < firstAttackDelay) {

                yield return new WaitForSeconds(Time.deltaTime);
                elapsed += Time.deltaTime;

                if (!isPlayerInRange)
                    break;

            }
        }

        // if player leaves range before the firstAttackDelay ends then dont do this   
        if (isPlayerInRange) {

            attackPerformed = true;
            animLength = PlayAnimation(Animation.WindUp); // play wind up animation  

            yield return new WaitForSeconds(animLength); // wait for wind up animation to finish  

        }

        // if player is not in range then attack fails  
        // or, if player has ninja dashes (dash invulnerability), and they dash here at just the right time  
        if (!isPlayerInRange || (GameData.IsAbilityUnlocked(AbilityType.NinjaDashes) && playerController.IsDashing())) {

            // if player left range before firstAttackDelay ended then exhaust the enemy  
            if (attackPerformed) {

                enemyState = EnemyState.Exhausted; // set enemy state to exhausted  
                aiPath.canMove = false; // prevent enemy from moving while exhausted  
                PlayAnimation(Animation.Exhausted); // play exhausted animation  
                currentExhaustedPopup = popups.Play(exhaustedPopup, exhaustedPopupLocation, exhaustionTime);

                yield return new WaitForSeconds(exhaustionTime); // wait for exhaustion time before allowing another attack  

            }

            enemyState = EnemyState.Walking; // set enemy state back to walking  
            aiPath.canMove = true; // allow enemy to move again since exhaustion is over  
            PlayAnimation(Animation.Walk); // reset back to walk animation after exhaustion is over  

            canAttack = true; // allow enemy to attack again after cooldown  
            attackCoroutine = null;
            yield break; // exit coroutine  

        }

        playerController.TakeDamage(damage); // damage the player  
                                             // ability: thorns  
        if (GameData.IsAbilityUnlocked(AbilityType.Herbicide)) {

            TakeDamage(GameData.GetStat(StatType.Damage) * playerController.GetThornsDamageRatio(), false, false);
            ApplySlow(playerController.GetThornsSlowAmount(), playerController.GetThornsSlowDuration());

        }

        soundManager.PlaySound(EnemySoundType.Attack);

        animLength = PlayAnimation(Animation.WindDown); // play wind down animation  

        yield return new WaitForSeconds(animLength); // wait for wind down animation to finish  
        PlayAnimation(Animation.Walk); // play walk animation after attack  
                                       // soundManager.PlaySound(EnemySoundType.Attack); // TODO: play attack sound  
        attackCooldownCoroutine = StartCoroutine(HandleAttackCooldown(successfulAttackCooldown)); // handle attack cooldown  

        attackCoroutine = null;

    }

    // half of the logic is handled in WeaponManager, where the weapon checks if the enemy
    // is this typeof enemy and then calls this method instead of the base one 
    public new void TakeDamage(float damage, bool playSound, bool doInvulnerability) {

        if (attackCoroutine == null)
            base.TakeDamage(damage, playSound, doInvulnerability);
        else
            playerController.TakeDamage(this.damage);


    }

    public new void TakeDamage(float damage) => this.TakeDamage(damage, true, true);

}
