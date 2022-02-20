using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Panda;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.U2D.IK;

public class Boss1 : MonoBehaviour
{
    public Transform target;
    public MovingKinematic body;
    public float sightDistance = 20;
    public float torsoHeightOffset = 5;
    public float stepHeight = 5;
    public float stepDistance = 12;
    public float feetSpeed = 10;
    public LimbSolver2D rightFootSolver;
    public LimbSolver2D leftFootSolver;
    private MovingKinematic _rightFootTarget;
    private MovingKinematic _leftFootTarget;
    private Transform _rightFootTransform;
    private Transform _leftFootTransform;

    private MovingKinematic currentFootTarget;
    private Transform currentFootTransform;
    private Vector2 footDestiniation;
    private Vector2 footSize;
    //private Vector2 footHitboxSize;
    
    
    private void OnValidate()
    {
        GetComponents();
        SetStartingVariables();
    }

    private void Awake()
    {
        GetComponents();
        SetStartingVariables();
    }

    void GetComponents()
    {
        _rightFootTarget = rightFootSolver.GetChain(0).target.GetComponent<MovingKinematic>();
        _leftFootTarget =  leftFootSolver.GetChain(0).target.GetComponent<MovingKinematic>();
        _rightFootTransform = rightFootSolver.GetChain(0).effector;
        _leftFootTransform = leftFootSolver.GetChain(0).effector;
        
        footSize = _rightFootTarget.GetComponent<BoxCollider2D>().size;
    }

    void SetStartingVariables()
    {
        currentFootTarget = _leftFootTarget;
        currentFootTransform = _leftFootTransform;
        
        // footHitboxSize = footSize;
        // footHitboxSize.x += footHitboxThickness * 2;
        // footHitboxSize.y += footHitboxThickness;
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
    bool IsTargetSeen()
    {
        return Vector2.Distance(body.transform.position, target.position) <= sightDistance;
    }

    [Task]
    bool TargetInStepRange()
    {
        return Vector2.Distance(body.transform.position, target.position) <= stepDistance;
    }

    [Task]
    void SetFootDestinationOnTarget()
    {
        var targetGroundHit = Physics2D.Raycast(target.position, Vector2.down, Mathf.Infinity, GameData.defaultGroundMask);
        var localFootTargetPos = body.transform.InverseTransformPoint(targetGroundHit.point);
        if (currentFootTarget == _rightFootTarget)
            localFootTargetPos.x = Mathf.Clamp(localFootTargetPos.x, 0, stepDistance);
        else //currentFoot == leftFoot
            localFootTargetPos.x = Mathf.Clamp(localFootTargetPos.x, -stepDistance, 0);
        footDestiniation = body.transform.TransformPoint(localFootTargetPos);

        ThisTask.Succeed();
    }
    
    [Task]
    void SetFootDestinationTowardsTarget()
    {
        var targetDir = Mathf.Sign(target.position.x - currentFootTarget.NextPosition.x);
        var pos = currentFootTarget.NextPosition + new Vector2(targetDir * stepDistance, stepHeight);
        var targetGroundHit = Physics2D.Raycast(pos, Vector2.down, Mathf.Infinity, GameData.defaultGroundMask);
        var localFootTargetPos = body.transform.InverseTransformPoint(targetGroundHit.point);
        if (currentFootTarget == _rightFootTarget)
            localFootTargetPos.x = Mathf.Clamp(localFootTargetPos.x, 0, stepDistance);
        else //currentFoot == leftFoot
            localFootTargetPos.x = Mathf.Clamp(localFootTargetPos.x, -stepDistance, 0);
        footDestiniation = body.transform.TransformPoint(localFootTargetPos);

        ThisTask.Succeed();
    }
    
    [Task]
    void SwapCurrentFoot()
    {
        if (currentFootTarget == _rightFootTarget)
        {
            currentFootTarget = _leftFootTarget;
            currentFootTransform = _leftFootTransform;
        }
        else
        {
            currentFootTarget = _rightFootTarget;
            currentFootTransform = _rightFootTransform;
        }
        ThisTask.Succeed();
    }
    
    public Vector2 GetTorsoCenter()
    {
        Vector2 footAverage = (_leftFootTarget.transform.position + _rightFootTarget.transform.position) / 2;
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
            startTime = Time.fixedTime;
            arcLength = 0;
            duration = 0;
            arcLength = EstimateArcLength(10);
            duration = arcLength / speed;
        }

        public Vector2 GetPosition()
        {
            //this 'Time.fixedTime - startTime' viable for long play time?
            float timeElapsed = Time.fixedTime - startTime;
            float t = Mathf.Clamp01(timeElapsed / duration);
            return Lerp(t);
        }
        
        public Vector2 GetDelta()
        {
            //this 'Time.fixedTime - startTime' viable for long play time?
            float timeElapsed = Time.fixedTime - startTime;
            Vector2 newPos = Lerp((timeElapsed + Time.fixedDeltaTime) / duration);
            Vector2 prevPos = Lerp(timeElapsed / duration);
            return newPos - prevPos;
        }
        
        public Vector2 Lerp(float t)
        {
            t = Mathf.Clamp01(t);
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
            ThisTask.data = new ArcTween(currentFootTarget.NextPosition, footDestiniation, stepHeight, feetSpeed);
        }

        ArcTween arcTween = ThisTask.GetData<ArcTween>();

        if (Time.fixedTime <= (arcTween.startTime + arcTween.duration))
        {
            Vector2 delta = arcTween.GetDelta();

            if (Physics2D.OverlapBox(currentFootTarget.NextPosition + delta, footSize, 0, GameData.defaultGroundMask))
            {
                Vector2 newDelta = delta;
                newDelta.x = 0;

                if (delta.y < 0)
                {
                    RaycastHit2D hit = Physics2D.BoxCast(currentFootTarget.NextPosition, footSize, 0, Vector2.down, delta.magnitude, GameData.defaultGroundMask);
                    if (hit)
                    {
                        newDelta.y = -hit.distance;
                    }
                }
                else
                {
                    if (Physics2D.OverlapBox(currentFootTarget.NextPosition + newDelta, footSize, 0, GameData.defaultGroundMask))
                    {
                        newDelta = Vector2.zero;
                    }
                }

                delta = newDelta;
            }
            
            // if (Physics2D.OverlapBox(currentFootTarget.NextPosition + delta, footSize, 0, GameData.defaultGroundMask))
            // {
            //     if (Physics2D.OverlapBox(currentFootTarget.NextPosition + new Vector2(delta.x, 0), footSize, 0, GameData.defaultGroundMask))
            //     {
            //         delta.x = 0;
            //     }
            //
            //     if (Physics2D.OverlapBox(currentFootTarget.NextPosition + new Vector2(0, delta.y), footSize, 0, GameData.defaultGroundMask))
            //     {
            //         delta.y = 0;
            //     }
            // }
            
            Vector2 leftFootDisconnect = _leftFootTransform.position - _leftFootTarget.transform.position;
            Vector2 rightFootDisconnect = _rightFootTransform.position - _rightFootTarget.transform.position;

            float tolerance = 0.01f;
            bool disconnectRight = leftFootDisconnect.x > tolerance || rightFootDisconnect.x > tolerance;
            bool disconnectLeft = leftFootDisconnect.x < -tolerance || rightFootDisconnect.x < -tolerance;
            bool disconnectUp = leftFootDisconnect.y > tolerance || rightFootDisconnect.y > tolerance;
            bool disconnectDown = leftFootDisconnect.y < -tolerance || rightFootDisconnect.y < -tolerance;
            
            if (delta.y > 0 && disconnectUp)
                delta.y = Mathf.Min(delta.y, 0);
            if (delta.y < 0 && disconnectDown)
                delta.y = Mathf.Max(delta.y, 0);
            if (delta.x < 0 && disconnectLeft)
                delta.x = Mathf.Max(delta.x, 0);
            if (delta.x > 0 && disconnectRight)
                delta.x = Mathf.Min(delta.x, 0);
            
            currentFootTarget.MovementUpdate(currentFootTarget.NextPosition + delta);
            body.MovementUpdate(GetTorsoCenter());
        }
        else
        {
            currentFootTarget.MovementUpdate();
            body.MovementUpdate(GetTorsoCenter());
            
            GetComponent<CinemachineImpulseSource>().GenerateImpulse();
            ThisTask.Succeed();
        }
    }

    [Task]
    void MoveFootToGround()
    {
        Vector2 p = currentFootTarget.NextPosition + (Vector2.down * feetSpeed * Time.fixedDeltaTime);
        currentFootTarget.MovementUpdate(p);
        body.MovementUpdate(GetTorsoCenter());
        if(Physics2D.OverlapBox(p, footSize, 0, GameData.defaultGroundMask))
        {
            ThisTask.Succeed();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        if(footDestiniation != Vector2.zero)
            Gizmos.DrawSphere(footDestiniation, 1);

        // Vector2 l = leftFoot.transform.position;
        // l.y += (footHitboxThickness/2);
        // Gizmos.DrawWireCube( l , footHitboxSize);
        //
        // Vector2 r = rightFoot.transform.position;
        // r.y += (footHitboxThickness/2);
        // Gizmos.DrawWireCube( r , footHitboxSize);
    }
}
