using System;
using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using UnityEditor.UI;
using UnityEngine;

public class Boss1 : StateMachine<Boss1>
{
    public Entity attackTarget;

    public Transform torso;
    public float torsoHeightOffset = 5;
    public FootStates rightFootStates;
    public FootStates leftFootStates;

    [System.Serializable]
    public class FootStates
    {
        public Transform foot;
        public Transform hip;

        public void CreateStates(int footDirection)
        {
            this.footDirection = footDirection;
            this.footKinematic = foot.GetComponent<MovingKinematic>();
            this.plant = new FootPlantState(this);
            this.walk = new FootWalkState(this);
            this.attack = new FootAttackState(this);
        }

        public int footDirection;
        [HideInInspector]
        public MovingKinematic footKinematic;
        public FootPlantState plant;
        public FootWalkState walk;
        public FootAttackState attack;
    }

    void Start()
    {
        rightFootStates.CreateStates(1);
        leftFootStates.CreateStates(-1);

        SetState(rightFootStates.plant);
    }

    void FixedUpdate()
    {
        currentState.UpdateState(this);
    }

    public FootStates GetOppositeFootStates(FootStates footStates)
    {
        if (footStates == rightFootStates)
            return leftFootStates;
        else
            return rightFootStates;
    }

    public Vector2 GetTorsoTargetPositionMoving()
    {
        Vector2 footAverage = (leftFootStates.foot.position + rightFootStates.foot.position) / 2;
        return (Vector2.up * torsoHeightOffset) + footAverage;
    }
}