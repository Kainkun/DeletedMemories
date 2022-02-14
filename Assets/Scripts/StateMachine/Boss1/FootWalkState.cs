using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

public class FootWalkState : FootState
{
    private float time;
    private float speed = 4;
    private Vector2 footStartPosition;
    private Vector2 footTargetPosition;
    private float timeToComplete;
    
    public FootWalkState(Boss1.FootStates footStates) => this.footStates = footStates;

    public override void EnterState(Boss1 stateMachine)
    {
        Debug.Log(footStates.foot.name + " walk start");

        var localFootTargetPos = stateMachine.torso.transform.InverseTransformPoint(stateMachine.attackTarget.transform.position);
        if (footStates.footDirection == 1)
            localFootTargetPos.x = Mathf.Clamp(localFootTargetPos.x, 0, 12f);
        else
            localFootTargetPos.x = Mathf.Clamp(localFootTargetPos.x, -12f, 0);
        footTargetPosition = stateMachine.torso.transform.TransformPoint(localFootTargetPos);
        footTargetPosition.y = 0;

        time = 0;
        footStartPosition = footStates.footKinematic.CurrentPosition;
        float totalDistance = Vector2.Distance(footStartPosition, footTargetPosition);
        timeToComplete = totalDistance / speed;
    }

    public override void UpdateState(Boss1 stateMachine)
    {
        time += Time.deltaTime;
        float t = Mathf.Clamp01(time / timeToComplete);

        var dist = Vector3.Distance(footStates.footKinematic.CurrentPosition, footTargetPosition);
        Debug.Log(dist);
        if (dist > 0.001f)
        {
            var p = Vector2.Lerp(footStartPosition, footTargetPosition, t);
            p.y += Mathf.Sin(t * Mathf.PI) * 5;
            footStates.footKinematic.MovementUpdate(p);
            //footStates.foot.position = Vector3.MoveTowards(footStates.foot.position, footTargetPos, Time.deltaTime * speed);
            stateMachine.torso.position = Vector2.MoveTowards(stateMachine.torso.position, stateMachine.GetTorsoTargetPositionMoving(), Time.deltaTime * speed);
        }
        else
        {
            stateMachine.SetState(footStates.plant);
        }
    }

    public override void ExitState(Boss1 stateMachine)
    {
        footStates.footKinematic.MovementUpdate(footStates.footKinematic.CurrentPosition);
        Debug.Log(footStates.foot.name + " walk end");
    }
}