using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootPlantState : FootState
{
    private float plantTime = 1;
    private float currentTime;
    private Vector2 torsoTargetPosition;
    private float speed = 3;

    public FootPlantState(Boss1.FootStates footStates) => this.footStates = footStates;

    public override void EnterState(Boss1 stateMachine)
    {
        Debug.Log(footStates.foot.name + " plant start");
        currentTime = 0;
        torsoTargetPosition = (Vector2.up * stateMachine.torsoHeightOffset) + (Vector2)footStates.footKinematic.CurrentPosition;
    }

    public override void UpdateState(Boss1 stateMachine)
    {
        currentTime += Time.deltaTime;

        // if (Vector2.Distance(stateMachine.torso.position, torsoTargetPosition) > 0.001f)
        // {
        //     stateMachine.torso.position = Vector2.MoveTowards(stateMachine.torso.position, torsoTargetPosition, Time.deltaTime * speed);
        // }
        if (currentTime < plantTime)
        {
            currentTime += Time.deltaTime;
        }
        else
        {
            var oppositeFootStates = stateMachine.GetOppositeFootStates(footStates);
            var shoulderPosition = oppositeFootStates.shoulder.position;
            var targetPos = stateMachine.attackTarget.transform.position;
            var dist = Vector3.Distance(shoulderPosition, targetPos);
            Debug.Log(dist);
            if (dist > 6 && dist < 11)
                stateMachine.SetState(oppositeFootStates.attack);
            else
                stateMachine.SetState(oppositeFootStates.walk);
        }
    }

    public override void ExitState(Boss1 stateMachine)
    {
        Debug.Log(footStates.foot.name + " plant end");
    }
}