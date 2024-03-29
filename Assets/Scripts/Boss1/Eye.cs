using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Panda;
using Random = UnityEngine.Random;

public class Eye : Entity
{
    private Transform pupil;
    private Transform target;
    public float fov = 45;
    public float saccadeCooldown = 1;
    public float pupilSpeed = 1;
    public float pupilSizeSpeed = 1;
    public float pupilClamp = 1;
    public float sightDistance = 50;
    private Vector2 directionToLook;
    private Vector2 directionLooking;
    private Vector2 directionToTarget;
    public float timeSinceSaccade;
    private float targetPupilSize;
    public float peripheryRadius = 5;
    public bool eyeMoving;
    [Task]
    public bool seesTarget;
    [Task]
    public bool saccadeInCooldown;

    void Start()
    {
        pupil = transform.GetChild(0);
        target = GameObject.FindObjectOfType<PlatformerController>().transform;
        timeSinceSaccade = Mathf.Infinity;
    }

    [Task]
    public void SaccadeCooldownUpdate()
    {
        if(!eyeMoving)
            timeSinceSaccade += Time.deltaTime;

        saccadeInCooldown = timeSinceSaccade <= saccadeCooldown;
        ThisTask.Succeed();
    }

    [Task]
    public void SetRandomLookDirection()
    {
        directionToLook = Random.insideUnitCircle.normalized;
        ThisTask.Succeed();
    }
    
    [Task]
    public void SetLookDirectionToTarget()
    {
        directionToLook = (target.position - transform.position).normalized;
        ThisTask.Succeed();
    }

    public void BringAttentionTo(Collision2D collision)
    {
        directionToLook = (collision.GetContact(0).point - (Vector2)transform.position).normalized;
    }
    
    public void BringAttentionTo(Collider2D col)
    {
        directionToLook = ((Vector2)col.transform.position - (Vector2)transform.position).normalized;
    }
    
    public void BringAttentionTo(Vector2 position)
    {
        directionToLook = (position - (Vector2)transform.position).normalized;
    }
    


    [Task]
    public void CheckSeesTarget()
    {
        Vector2 position = transform.position;
        directionToTarget = ((Vector2) target.position - position).normalized;

        if (Vector2.Distance(target.position, transform.position) <= peripheryRadius)
        {
            seesTarget = true;
        }
        else
        {
            seesTarget = false;
            if (Vector2.Angle(directionLooking, directionToTarget) <= fov / 2)
            {
                RaycastHit2D hit = Physics2D.Raycast(position, directionToTarget, sightDistance, GameData.opaqueMask);
                if (hit && hit.transform.GetComponent<PlatformerController>())
                    seesTarget = true;
            }
        }

        ThisTask.Succeed();
    }

    [Task]
    public void LerpEyePosition()
    {
        Vector2 targetPosition = (Vector2)transform.position + (directionToLook * pupilClamp);
        Vector2 currentPosition = pupil.transform.position;
        
        Debug.DrawRay(targetPosition, Vector3.up + Vector3.right, Color.magenta);
        Debug.DrawRay(currentPosition, Vector3.up + Vector3.right, Color.cyan);


        if(Vector2.Distance(currentPosition, targetPosition) <= 0.0001f)
        {
            ThisTask.Succeed();
            return;
        }
        

        
        eyeMoving = true;
        timeSinceSaccade = 0;
        currentPosition = Vector2.MoveTowards(currentPosition, targetPosition, pupilSpeed * Time.deltaTime);
        directionLooking = (currentPosition - (Vector2)transform.position).normalized;

        if(Vector2.Distance(currentPosition, targetPosition) <= 0.01f)
        {
            currentPosition = targetPosition;
            eyeMoving = false;
        }
        pupil.transform.position = currentPosition;
        ThisTask.Succeed();
    }

    [Task]
    public void SetPupilSize(float diameter)
    {
        targetPupilSize = diameter;
        ThisTask.Succeed();
    }
    
    [Task]
    public void LerpPupilSize()
    {
        float scale = pupil.localScale.x;
        scale = Mathf.MoveTowards(scale, targetPupilSize, pupilSizeSpeed * Time.deltaTime);
        pupil.localScale = new Vector3(scale, scale, 1);
        
        if(Mathf.Abs(pupil.localScale.x - targetPupilSize) <= 0.01f)
        {
            pupil.localScale = new Vector3(targetPupilSize, targetPupilSize, 1);
        }
        ThisTask.Succeed();
    }

    private void OnDrawGizmos()
    {
        Vector3 position = transform.position;
        Gizmos.color = Color.green;
        Gizmos.DrawRay(position, directionLooking * sightDistance);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(position, directionLooking.Rotate(fov / 2) * sightDistance);
        Gizmos.DrawRay(position, directionLooking.Rotate(-fov / 2) * sightDistance);
        
        Gizmos.DrawWireSphere(transform.position, peripheryRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawRay(position, directionToTarget * sightDistance);
    }
}
