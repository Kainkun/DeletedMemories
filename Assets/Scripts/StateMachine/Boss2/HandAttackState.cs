using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandAttackState : HandState
{
    private float time;
    
    public HandAttackState(Boss2.HandStates handStates) => this.handStates = handStates;

    public override void EnterState(Boss2 stateMachine)
    {
        time = 0;
    }

    public override void UpdateState(Boss2 stateMachine)
    {
        if (time < 3)
        {
            time += Time.fixedDeltaTime;
        }
        else
        {
            stateMachine.SetState(stateMachine.hoverToTargetState);
        }
    }
}
