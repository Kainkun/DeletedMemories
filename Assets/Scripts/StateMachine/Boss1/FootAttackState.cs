using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootAttackState : FootState
{
    private float time;
    private float speed = 4;
    private Vector2 footStartPosition;
    private Vector2 footTargetPosition;
    private float timeToComplete;

    public FootAttackState(Boss1.FootStates footStates) => this.footStates = footStates;

    public override void EnterState(Boss1 stateMachine)
    {
        Debug.Log(footStates.foot.name + " attack start");
        footStartPosition = stateMachine.attackTarget.transform.position;
    }

    public override void UpdateState(Boss1 stateMachine)
    {
        time += Time.deltaTime;

        var dist = Vector3.Distance(footStates.foot.position, footStartPosition);
        if (dist > 0.001f)
        {
            footStates.foot.position = Vector3.MoveTowards(footStates.foot.position, footStartPosition, Time.deltaTime * speed);
            stateMachine.torso.position = Vector2.MoveTowards(stateMachine.torso.position, stateMachine.GetTorsoTargetPositionMoving(), Time.deltaTime * speed);
        }
        else
        {
            stateMachine.SetState(footStates.plant);
        }
    }

    public override void ExitState(Boss1 stateMachine)
    {
        Debug.Log(footStates.foot.name + " attack end");
    }
}