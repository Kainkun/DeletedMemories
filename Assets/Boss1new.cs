using System;
using System.Collections;
using System.Collections.Generic;
using Panda;
using UnityEngine;

public class Boss1new : MonoBehaviour
{
    public Transform attackTarget;
    public Transform body;
    public float torsoHeightOffset = 5;
    public float stepRange = 12;
    public float stepHeight = 5;
    public float feetSpeed = 10;
    public MovingKinematic rightFoot;
    public MovingKinematic leftFoot;

    private MovingKinematic currentFoot;
    private Vector2 footDestiniation;

    private void Start()
    {
        currentFoot = leftFoot;
    }


    [Task]
    bool IsAlive()
    {
        return true;
    }

    [Task]
    void Die()
    {
        Destroy(gameObject);
        ThisTask.Succeed();
        
    }

    [Task]
    bool TargetInStepRange()
    {
        return Vector2.Distance(body.position, attackTarget.position) <= stepRange;
    }

    [Task]
    void SetFootDestinationTowardsTarget()
    {
        var localFootTargetPos = body.InverseTransformPoint(attackTarget.position);
        if (currentFoot == rightFoot)
            localFootTargetPos.x = Mathf.Clamp(localFootTargetPos.x, 0, stepRange);
        else //currentFoot == leftFoot
            localFootTargetPos.x = Mathf.Clamp(localFootTargetPos.x, -stepRange, 0);
        footDestiniation = body.TransformPoint(localFootTargetPos);

        ThisTask.Succeed();
    }
    
    [Task]
    void SwapCurrentFoot()
    {
        if (currentFoot == rightFoot)
            currentFoot = leftFoot;
        else
            currentFoot = rightFoot;
        ThisTask.Succeed();
    }
    
    public Vector2 GetTorsoCenter()
    {
        Vector2 footAverage = (leftFoot.transform.position + rightFoot.transform.position) / 2;
        return (Vector2.up * torsoHeightOffset) + footAverage;
    }
    
    struct ArcTween
    {
        public Vector2 startPosition;
        public Vector2 endPosition;
        private float stepHeight;
        public float startTime;
        public float arcLength;
        public float duration;

        public ArcTween(Vector2 startPosition, Vector2 endPosition, float stepHeight, float speed)
        {
            this.startPosition = startPosition;
            this.endPosition = endPosition;
            this.stepHeight = stepHeight;
            startTime = Time.time;
            arcLength = 0;
            duration = 0;
            arcLength = EstimateArcLength(10);
            duration = arcLength / speed;
        }

        public Vector2 GetPosition()
        {
            //this 'Time.time - startTime' viable for long play time?
            float timeElapsed = Time.time - startTime;
            float t = Mathf.Clamp01(timeElapsed / duration);
            return Lerp(t);
        }
        
        public Vector2 Lerp(float t)
        {
            Vector2 p = Vector2.Lerp(startPosition, endPosition, t);
            p.y += Mathf.Sin(t * Mathf.PI) * stepHeight;
            return p;
        }

        float EstimateArcLength(int steps)
        {
            float total = 0;

            for (int i = 0; i < steps; i++)
            {
                Vector2 a = Lerp(i / (float) steps);
                Vector2 b = Lerp((i + 1) / (float) steps);
                total += Vector2.Distance(a, b);
            }

            return total;
        }
    }

    [Task]
    void MoveFootToDestination()
    {
        if (ThisTask.isStarting)
        {
            ThisTask.data = new ArcTween(currentFoot.transform.position, footDestiniation, stepHeight, feetSpeed);
        }

        ArcTween arcTween = ThisTask.GetData<ArcTween>();

        if (Vector2.Distance(currentFoot.CurrentPosition, footDestiniation) > 0.01f)
        {
            currentFoot.MovementUpdate(arcTween.GetPosition());
            body.position = GetTorsoCenter();
        }
        else
        {
            ThisTask.Succeed();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(footDestiniation, 1);
    }
}
