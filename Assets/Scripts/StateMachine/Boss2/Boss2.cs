using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss2 : StateMachine<Boss2>
{
    public Entity attackTarget;

    public Transform torso;
    [HideInInspector]
    public MovingKinematic torsoKinematic;
    public float torsoHeightOffset = 5;
    public HandStates rightHandStates;
    public HandStates leftHandStates;
    public HoverToTargetState hoverToTargetState;

    [System.Serializable]
    public class HandStates
    {
        public Transform hand;
        public Transform shoulder;

        public void CreateStates(int handDirection)
        {
            this.handDirection = handDirection;
            this.handKinematic = hand.GetComponent<MovingKinematic>();
            this.attack = new HandAttackState(this);
        }

        public int handDirection;
        [HideInInspector]
        public MovingKinematic handKinematic;
        public HandAttackState attack;
    }
    
    void Start()
    {
        torsoKinematic = torso.GetComponent<MovingKinematic>();
        
        rightHandStates.CreateStates(1);
        leftHandStates.CreateStates(-1);
        hoverToTargetState = new HoverToTargetState();

        SetState(hoverToTargetState);
    }

    void FixedUpdate()
    {
        currentState.UpdateState(this);
    }
}
