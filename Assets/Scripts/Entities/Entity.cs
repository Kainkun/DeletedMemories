using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class Entity : MonoBehaviour
{
    public int maxHealth = 3;
    public bool ignoreDamage;
    public int currentHealth;
    public Action<Entity> onDeath;
    public List<Entity> childrenEntities;
    public bool dieIfChildrenKilled;
    public bool destroyOnDeath;
    public bool isDead;

    [EasyButtons.Button]
    public void GetEntityChildren()
    {
        childrenEntities.Clear();
        foreach (Entity entity in GetComponentsInChildren<Entity>())
            if(entity != this)
                childrenEntities.Add(entity);
    }

    protected virtual void Awake()
    {
        currentHealth = maxHealth;

        foreach (Entity childrenEntity in childrenEntities)
            childrenEntity.onDeath += ChildDeath;
    }

    public virtual void TakeDamage(int damage)
    {
        if(ignoreDamage)
            return;
        
        currentHealth -= damage;
        if (currentHealth <= 0)
            Die();
    }

    protected virtual void ChildDeath(Entity child)
    {
        childrenEntities.Remove(child);
        
        if(dieIfChildrenKilled && childrenEntities.Count <= 0)
            Die();
    }
    
    public virtual void Die()
    {
        if(isDead)
            return;;
        
        isDead = true;
        onDeath?.Invoke(this);
        if(destroyOnDeath)
            Destroy(gameObject);
    }
}