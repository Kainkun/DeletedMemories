using System;
using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    private Vector2 inputDirection;
    public float attackRange = 3;
    public float attackBuffer = 0.2f;
    private bool attackWaiting;
    public float attackDuration = 0.35f;
    public float attackSpriteDuration = 0.15f;
    public float attackCooldown = 0.35f;
    public Collider2D attackCollider;
    private Vector2 attackDirection;
    public SpriteRenderer attackSpriteRenderer;
    
    private Coroutine currentAttackCoroutine;
    
    private void Start()
    {
        InputManager.Get().Move += HandleMove;
        InputManager.Get().Primary += Attack;
        
        attackCollider.enabled = false;
        attackSpriteRenderer.enabled = false;
    }

    void HandleMove(Vector2 value)
    {
        // if (value.magnitude > 0.1f)
        //     direction = value;
        if(value.y > 0.1f)
            inputDirection = Vector2.up;
        else if (value.y < -0.1f)
            inputDirection = Vector2.down;
        else if (value.x > 0.1f)
            inputDirection = Vector2.right;
        else if (value.x < -0.1f)
            inputDirection = Vector2.left;
    }

    void Attack()
    {
        attackWaiting = true;
        attackDirection = inputDirection;
    }

    private void Update()
    {
        if (attackWaiting && currentAttackCoroutine == null)
        {
            attackWaiting = false;
            attackCollider.transform.right = attackDirection;
            currentAttackCoroutine = StartCoroutine(CR_Attack());
        }
    }

    IEnumerator CR_Attack()
    {
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(~GameData.playerMask);
        contactFilter.useTriggers = true;
        attackCollider.enabled = true;
        attackSpriteRenderer.enabled = true;
        List<Collider2D> results = new List<Collider2D>();
        List<Entity> damaged = new List<Entity>();
        
        float time = 0;
        while (time <= attackDuration)
        {
            attackCollider.OverlapCollider(contactFilter, results);
            foreach (Collider2D result in results)
            {
                Entity entity = result.GetComponent<Entity>();
                if (entity && !damaged.Contains(entity))
                {
                    entity.TakeDamage(1);
                    damaged.Add(entity);
                }
            }
            
            if(time >= attackSpriteDuration)
                attackSpriteRenderer.enabled = false;

            time += Time.deltaTime;
            yield return null;
        }
        
        attackCollider.enabled = false;

        yield return new WaitForSeconds(attackCooldown);
        currentAttackCoroutine = null;
    }
}
