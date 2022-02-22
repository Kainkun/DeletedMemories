using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Panda;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.U2D.IK;

public class Boss1 : MonoBehaviour
{
    private Transform _target;
    public MovingKinematic body;
    public float sightDistance = 20;
    public float torsoHeightOffset = 5;
    public float stepHeight = 5;
    public float stepDistance = 12;
    public float feetSpeed = 10;
    public FootData rightFoot;
    public FootData leftFoot;
    [HideInInspector]
    public FootData currentFoot;

    [Serializable]
    public class FootData
    {
        public void Setup()
        {
            effectorTransform = solver.GetChain(0).effector;
            targetMovingKinematic = solver.GetChain(0).target.GetComponent<MovingKinematic>();
            collider = targetMovingKinematic.GetComponent<BoxCollider2D>();
            size = collider.size;
        }

        public static Vector2 size;
        public LimbSolver2D solver;
        [HideInInspector]
        public Transform effectorTransform;
        [HideInInspector]
        public MovingKinematic targetMovingKinematic;
        [HideInInspector]
        public BoxCollider2D collider;
        [HideInInspector]
        public Vector2 destiniation;
    }

    private void Awake()
    {
        Setup();
    }

    private void Start()
    {
        _target = GameObject.FindObjectOfType<PlatformerController>().transform;
    }


    void Setup()
    {
        rightFoot.Setup();
        leftFoot.Setup();
        
        currentFoot = leftFoot;
        
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
        if (!_target)
            return false;
        return Vector2.Distance(body.transform.position, _target.position) <= sightDistance;
    }

    [Task]
    bool TargetInStepRange()
    {
        return Vector2.Distance(body.transform.position, _target.position) <= stepDistance;
    }

    [Task]
    void SetFootDestinationOnTarget()
    {
        currentFoot.collider.enabled = false;
        var targetGroundHit = Physics2D.Raycast(_target.position, Vector2.down, Mathf.Infinity, GameData.defaultGroundMask);
        currentFoot.collider.enabled = true;

        var localFootTargetPos = body.transform.InverseTransformPoint(targetGroundHit.point);
        if (currentFoot == rightFoot)
            localFootTargetPos.x = Mathf.Clamp(localFootTargetPos.x, 0, stepDistance);
        else //currentFoot == leftFoot
            localFootTargetPos.x = Mathf.Clamp(localFootTargetPos.x, -stepDistance, 0);
        currentFoot.destiniation = body.transform.TransformPoint(localFootTargetPos);

        ThisTask.Succeed();
    }
    
    [Task]
    void SetFootDestinationTowardsTarget()
    {
        var targetDir = Mathf.Sign(_target.position.x - currentFoot.targetMovingKinematic.NextFramePosition.x);
        var pos = currentFoot.targetMovingKinematic.NextFramePosition + new Vector2(targetDir * stepDistance, stepHeight);
        currentFoot.collider.enabled = false;
        var targetGroundHit = Physics2D.Raycast(pos, Vector2.down, Mathf.Infinity, GameData.defaultGroundMask);
        currentFoot.collider.enabled = true;
        var localFootTargetPos = body.transform.InverseTransformPoint(targetGroundHit.point);
        if (currentFoot == rightFoot)
            localFootTargetPos.x = Mathf.Clamp(localFootTargetPos.x, 0, stepDistance);
        else //currentFoot == leftFoot
            localFootTargetPos.x = Mathf.Clamp(localFootTargetPos.x, -stepDistance, 0);
        currentFoot.destiniation = body.transform.TransformPoint(localFootTargetPos);

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
        Vector2 footAverage = (leftFoot.targetMovingKinematic.transform.position + rightFoot.targetMovingKinematic.transform.position) / 2;
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
            ThisTask.data = new ArcTween(currentFoot.targetMovingKinematic.NextFramePosition, currentFoot.destiniation, stepHeight, feetSpeed);
        }

        ArcTween arcTween = ThisTask.GetData<ArcTween>();

        if (Time.fixedTime <= (arcTween.startTime + arcTween.duration))
        {
            Vector2 delta = arcTween.GetDelta();

            currentFoot.collider.enabled = false;
            if (Physics2D.OverlapBox(currentFoot.targetMovingKinematic.NextFramePosition + delta, FootData.size, 0, GameData.defaultGroundMask))
            {
                Vector2 newDelta = delta;
                newDelta.x = 0;

                if (delta.y < 0)
                {
                    RaycastHit2D hit = Physics2D.BoxCast(currentFoot.targetMovingKinematic.NextFramePosition, FootData.size, 0, Vector2.down, delta.magnitude, GameData.defaultGroundMask);
                    if (hit)
                    {
                        newDelta.y = -hit.distance;
                    }
                }
                else
                {
                    if (Physics2D.OverlapBox(currentFoot.targetMovingKinematic.NextFramePosition + newDelta, FootData.size, 0, GameData.defaultGroundMask))
                    {
                        newDelta = Vector2.zero;
                    }
                }

                delta = newDelta;
            }
            currentFoot.collider.enabled = true;
            
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
            
            Vector2 leftFootDisconnect = leftFoot.effectorTransform.position - leftFoot.targetMovingKinematic.transform.position;
            Vector2 rightFootDisconnect = rightFoot.effectorTransform.position - rightFoot.targetMovingKinematic.transform.position;

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
            
            Debug.DrawRay(currentFoot.targetMovingKinematic.NextFramePosition + delta, Vector3.up, Color.cyan);
            currentFoot.targetMovingKinematic.MovementUpdate(currentFoot.targetMovingKinematic.NextFramePosition + delta);
            body.MovementUpdate(GetTorsoCenter());
        }
        else
        {
            currentFoot.targetMovingKinematic.MovementUpdate();
            body.MovementUpdate(GetTorsoCenter());
            
            GetComponent<CinemachineImpulseSource>().GenerateImpulse();
            ThisTask.Succeed();
        }
    }

    [Task]
    void MoveFootToGround()
    {
        Vector2 p = currentFoot.targetMovingKinematic.NextFramePosition + (Vector2.down * feetSpeed * Time.fixedDeltaTime);
        currentFoot.targetMovingKinematic.MovementUpdate(p);
        body.MovementUpdate(GetTorsoCenter());
        if(Physics2D.OverlapBox(p, FootData.size, 0, GameData.defaultGroundMask))
        {
            ThisTask.Succeed();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        if(currentFoot.destiniation != Vector2.zero)
            Gizmos.DrawSphere(currentFoot.destiniation, 1);

        // Vector2 l = leftFoot.transform.position;
        // l.y += (footHitboxThickness/2);
        // Gizmos.DrawWireCube( l , footHitboxSize);
        //
        // Vector2 r = rightFoot.transform.position;
        // r.y += (footHitboxThickness/2);
        // Gizmos.DrawWireCube( r , footHitboxSize);
    }
}
