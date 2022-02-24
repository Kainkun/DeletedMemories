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
        if(value.magnitude > 0.1f)
            direction = value;
    }

    void Attack()
    {
        Debug.DrawRay(transform.position, direction * attackRange, Color.red, 0.2f);
        attackCollider.transform.right = direction;
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(~GameData.playerMask);
        contactFilter.useTriggers = true;
        List<Collider2D> results = new List<Collider2D>();
        attackCollider.enabled = true;
        attackCollider.OverlapCollider(contactFilter, results);
        attackCollider.enabled = false;
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
        attackSpriteRenderer.enabled = true;
        yield return new WaitForSeconds(0.1f);
        attackSpriteRenderer.enabled = false;
    }
}
