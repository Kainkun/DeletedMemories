using System;
using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    private Vector2 direction;
    public float attackRange = 3;
    public Collider2D attackCollider;
    public SpriteRenderer attackSpriteRenderer;
    
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
            direction = Vector2.up;
        else if (value.y < -0.1f)
            direction = Vector2.down;
        else if (value.x > 0.1f)
            direction = Vector2.right;
        else if (value.x < -0.1f)
            direction = Vector2.left;
    }

    void Attack()
    {
        Debug.DrawRay(transform.position, direction * attackRange, Color.red, 0.2f);
        attackCollider.transform.right = direction;
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(~GameData.playerMask);
        contactFilter.useTriggers = true;
        attackCollider.enabled = true;
        attackSpriteRenderer.enabled = true;
        List<Collider2D> results = new List<Collider2D>();
        attackCollider.OverlapCollider(contactFilter, results);
        foreach (Collider2D result in results)
        {
            Entity entity = result.GetComponent<Entity>();
            if (entity)
            {
                entity.TakeDamage(1);
            }
        }

        StartCoroutine(CR_Attack());
    }

    IEnumerator CR_Attack()
    {
        yield return new WaitForSeconds(0.1f);
        attackSpriteRenderer.enabled = false;
        attackCollider.enabled = false;
    }
}
