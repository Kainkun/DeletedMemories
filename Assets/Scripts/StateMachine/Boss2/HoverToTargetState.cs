using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

public class HoverToTargetState : State<Boss2>
{
    private float speed = 2;

    public override void UpdateState(Boss2 stateMachine)
    {
        Vector2 targetPos = stateMachine.attackTarget.transform.position;
        targetPos.y = stateMachine.torsoHeightOffset;
        Vector2 torsoPos = stateMachine.torsoKinematic.NextPosition;
        if (Mathf.Abs(targetPos.x - torsoPos.x) > 0.001f)
        {
            Vector2 delta = (targetPos - torsoPos).normalized * (Time.fixedDeltaTime * speed);

            var torso = stateMachine.torsoKinematic;
            torso.MovementUpdate(torso.NextPosition + delta);

            var right = stateMachine.rightHandStates.handKinematic;
            right.MovementUpdate(right.NextPosition + delta);

            var left = stateMachine.leftHandStates.handKinematic;
            left.MovementUpdate(left.NextPosition + delta);
        }
        else
        {
            stateMachine.SetState(stateMachine.hoverToTargetState);
        }
    }
}