using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Panda;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.U2D.IK;

public class Boss1 : Entity
{
    private Transform _target;
    public MovingKinematic body;
    public float sightDistance = 20;
    public float torsoHeightOffset = 5;
    public float stepHeight = 5;
    public float maxStepDistance = 12;
    public float feetSpeed = 10;
    public FootData rightFoot;
    public FootData leftFoot;
    [HideInInspector]
    public FootData currentFoot;
    private Eye eye;
    private PandaBehaviour eyeBT;

    [Task]
    private void TickEye()
    {
        eyeBT.Tick();
        ThisTask.Succeed();
    }
    
    [Task] private bool IsDead => base.isDead;
    [Task] private bool SeesTarget => eye.seesTarget;
    
    [Task] public bool targetInStepRange;



    [Serializable]
    public class FootData
    {
        public void Setup(float torsoHeightOffset)
        {
            effectorTransform = solver.GetChain(0).effector;
            targetMovingKinematic = solver.GetChain(0).target.GetComponent<MovingKinematic>();
            collider = targetMovingKinematic.GetComponent<BoxCollider2D>();
            size = collider.size;
            
            collider.enabled = false;
            RaycastHit2D hit = Physics2D.BoxCast(targetMovingKinematic.transform.position, FootData.size, 0, Vector2.down, torsoHeightOffset + FootData.size.y, GameData.defaultMask);
            collider.enabled = true;
            if (hit)
                targetMovingKinematic.transform.position += Vector3.down * hit.distance;

            destiniation = targetMovingKinematic.transform.position;
            atDestination = true;
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
        [HideInInspector]
        public bool atDestination;
    }

    protected override void Awake()
    {
        base.Awake();
        eye = GetComponentInChildren<Eye>();
        eyeBT = eye.GetComponent<PandaBehaviour>();
        _target = GameObject.FindObjectOfType<PlatformerController>().transform;
        
        rightFoot.Setup(torsoHeightOffset);
        leftFoot.Setup(torsoHeightOffset);
        currentFoot = leftFoot;
        body.transform.position = GetTorsoCenter();
    }

    [Task]
    void CheckTargetInStepRange()
    {
        targetInStepRange = Vector2.Distance(body.transform.position, _target.position) <= maxStepDistance;
        ThisTask.Succeed();
    }

    [Task]
    void SetFootDestinationOnTarget()
    {
        currentFoot.collider.enabled = false;
        var targetGroundHit = Physics2D.Raycast(_target.position, Vector2.down, Mathf.Infinity, GameData.defaultMask);
        currentFoot.collider.enabled = true;

        var localFootTargetPos = body.transform.InverseTransformPoint(targetGroundHit.point);
        if (currentFoot == rightFoot)
            localFootTargetPos.x = Mathf.Clamp(localFootTargetPos.x, 0, maxStepDistance);
        else //currentFoot == leftFoot
            localFootTargetPos.x = Mathf.Clamp(localFootTargetPos.x, -maxStepDistance, 0);
        currentFoot.destiniation = body.transform.TransformPoint(localFootTargetPos);
        currentFoot.atDestination = false;

        ThisTask.Succeed();
    }

    [Task]
    void SetFootDestinationTowardsTarget()
    {
        var targetDirection = Mathf.Sign(_target.position.x - currentFoot.targetMovingKinematic.NextFramePosition.x);
    
        float tryStepDistance;
        //right foot going right or left foot going left
        if ((targetDirection > 0 && currentFoot == rightFoot) || (targetDirection < 0 && currentFoot == leftFoot))
            tryStepDistance = maxStepDistance - Mathf.Abs(body.NextFramePosition.x - currentFoot.targetMovingKinematic.NextFramePosition.x);
        //left foot going right or right foot going left
        else
            tryStepDistance = Mathf.Abs(body.NextFramePosition.x - currentFoot.targetMovingKinematic.NextFramePosition.x) - ((FootData.size.x / 2) + 0.1f);
        
        currentFoot.collider.enabled = false;
        var FootAtStepHeight = currentFoot.targetMovingKinematic.NextFramePosition + new Vector2(0, stepHeight);
        //float dist = stepDistance - Mathf.dis (body.NextFramePosition.x - currentFoot.targetMovingKinematic.NextFramePosition.x);
        var horizontalHit = Physics2D.BoxCast(FootAtStepHeight, FootData.size, 0, new Vector2(targetDirection, 0), tryStepDistance, GameData.defaultMask);
        
        float finalStepDistance;
        if (horizontalHit)
            finalStepDistance = horizontalHit.distance - 0.1f;
        else
            finalStepDistance = tryStepDistance;
        
        if(currentFoot == leftFoot)
            Debug.DrawRay(FootAtStepHeight, new Vector2(targetDirection * finalStepDistance, 0), Color.green, 1);
        else
            Debug.DrawRay(FootAtStepHeight, new Vector2(targetDirection * finalStepDistance, 0), Color.red, 1);
        
        Vector2 groundCastTop = FootAtStepHeight + new Vector2(targetDirection * finalStepDistance, 0);
        var groundCastHit = Physics2D.Raycast(groundCastTop, Vector2.down, Mathf.Infinity, GameData.defaultMask);
        currentFoot.collider.enabled = true;
        
        if(groundCastHit.transform == null)
        {
            currentFoot.destiniation = currentFoot.targetMovingKinematic.NextFramePosition;
            ThisTask.Fail();
            return;
        }
        
        if(currentFoot == leftFoot)
            Debug.DrawRay(groundCastTop, Vector3.down * groundCastHit.distance, Color.green, 1);
        else
            Debug.DrawRay(groundCastTop, Vector3.down * groundCastHit.distance, Color.red, 1);
        
        currentFoot.destiniation = groundCastHit.point;
        currentFoot.atDestination = false;

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
    
    [Task]
    void SelectFootClosestToTarget()
    {
        if (_target.position.x < body.NextFramePosition.x)
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
    
    class ArcTween
    {
        public Vector2 startPosition;
        public Vector2 endPosition;
        private float stepHeight;
        //public float startTime;
        public float time;
        public float arcLength;
        public float duration;

        public ArcTween(Vector2 startPosition, Vector2 endPosition, float stepHeight, float speed)
        {
            this.startPosition = startPosition;
            this.endPosition = endPosition;
            this.stepHeight = stepHeight;
            time = 0;
            arcLength = EstimateArcLength(10);
            duration = arcLength / speed;
        }

        public Vector2 GetPosition()
        {
            //this 'Time.fixedTime - startTime' viable for long play time?
            float t = Mathf.Clamp01(time / duration);
            return Lerp(t);
        }
        
        public Vector2 GetDelta()
        {
            //this 'Time.fixedTime - startTime' viable for long play time?
            Vector2 newPos = Lerp((time + Time.fixedDeltaTime) / duration);
            Vector2 prevPos = Lerp(time / duration);
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

    private ArcTween currentArcTween;
    [Task] private bool IsMovingFoot => currentArcTween != null;
    //[Task] private bool IsMovingFoot => !leftFoot.atDestination || !rightFoot.atDestination;

    [Task] private bool CurrentFootAtDestination => currentFoot.atDestination; 

    [Task]
    void MoveFootTowardsDestination()
    {
        if (currentArcTween == null)
        {
            currentArcTween = new ArcTween(currentFoot.targetMovingKinematic.NextFramePosition, currentFoot.destiniation, stepHeight, feetSpeed);
        }

        //if (Time.fixedTime <= (currentArcTween.startTime + currentArcTween.duration))
        if(currentArcTween.time <= currentArcTween.duration)
        {
            currentArcTween.time += Time.deltaTime;
            Vector2 delta = currentArcTween.GetDelta();

            currentFoot.collider.enabled = false;
            if (Physics2D.OverlapBox(currentFoot.targetMovingKinematic.NextFramePosition + delta, FootData.size, 0, GameData.defaultMask))
            {
                Vector2 newDelta = delta;
                newDelta.x = 0;

                if (delta.y < 0)
                {
                    RaycastHit2D hit = Physics2D.BoxCast(currentFoot.targetMovingKinematic.NextFramePosition, FootData.size, 0, Vector2.down, delta.magnitude, GameData.defaultMask);
                    if (hit)
                    {
                        newDelta.y = -hit.distance;
                    }
                }
                else
                {
                    if (Physics2D.OverlapBox(currentFoot.targetMovingKinematic.NextFramePosition + newDelta, FootData.size, 0, GameData.defaultMask))
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
            body.MovementUpdate();
            GetComponent<CinemachineImpulseSource>().GenerateImpulse();
            currentArcTween = null;
            currentFoot.atDestination = true;
            SwapCurrentFoot();
        }
        ThisTask.Succeed();
    }

    // [Task]
    // void MoveFootToGround()
    // {
    //     Vector2 p = currentFoot.targetMovingKinematic.NextFramePosition + (Vector2.down * feetSpeed * Time.fixedDeltaTime);
    //     currentFoot.targetMovingKinematic.MovementUpdate(p);
    //     body.MovementUpdate(GetTorsoCenter());
    //     if(Physics2D.OverlapBox(p, FootData.size, 0, GameData.defaultGroundMask))
    //     {
    //         ThisTask.Succeed();
    //     }
    // }

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
