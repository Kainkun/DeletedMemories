using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.Networking;

public class AirWorm : MonoBehaviour
{
    public Transform target;
    public Transform pivot;
    public float passiveSpeed = 5;
    public float aggressiveSpeed = 20;
    public float rotateSmoothTime = 0.5f;
    private float rotateVelocity = 0;
    public float collisionRayDistance = 10;
    public float collisionRayRadius = 1;
    public float collisionFov = 90;
    public int collisionRayCount = 10;
    private float collisionAngleSpacing;
    private float angleOffset;
    private Rigidbody2D rb;
    private Collider2D collider;

    private void OnValidate()
    {
        passiveSpeed = Mathf.Max(passiveSpeed, 0);
        aggressiveSpeed = Mathf.Max(aggressiveSpeed, 0);
        collisionRayDistance = Mathf.Max(collisionRayDistance, 0);
        collisionRayRadius = Mathf.Max(collisionRayRadius, 0);
        collisionFov = Mathf.Max(collisionFov, 0);
        collisionRayCount = Mathf.Max(collisionRayCount, 0);

        Setup();
    }

    void Start()
    {
        //target = GameObject.FindObjectOfType<PlatformerController>().transform;
        Setup();
    }

    void Setup()
    {
        rb = GetComponent<Rigidbody2D>();
        collider = GetComponentInChildren<Collider2D>();
        collisionAngleSpacing = collisionFov / (collisionRayCount - 1);
        if (collisionRayCount % 2 == 0)
            angleOffset = collisionAngleSpacing / 2;
        else
            angleOffset = 0;
    }

    private void FixedUpdate()
    {
        Vector2 directionToTarget = (target.position - transform.position).normalized;
        Vector2 directionToLook = directionToTarget;
        collider.enabled = false;
        for (int i = 0; i < collisionRayCount; i++)
        {
            //maybe it it should do left then right or vice versa to prevent left right stutter
            float sign = ((i % 2) - 0.5f) * 2;
            Vector2 direction = MoreMath.Rotate(directionToTarget, (Mathf.Ceil(i / 2f) * sign * collisionAngleSpacing) - angleOffset);
            RaycastHit2D hit = Physics2D.CircleCast((Vector2)transform.position + (direction * collisionRayRadius), collisionRayRadius, direction, collisionRayDistance, GameData.airWormAvoidance);
            if (!hit)
            {
                directionToLook = direction;
                Debug.DrawRay((Vector2)transform.position + (direction * collisionRayRadius), direction * collisionRayDistance, Color.yellow);
                break;
            }

            directionToLook = -directionToTarget;
            Debug.DrawRay((Vector2)transform.position + (direction * collisionRayRadius), direction * collisionRayDistance, Color.blue);
        }

        collider.enabled = true;

        float angle = Mathf.SmoothDampAngle(pivot.eulerAngles.z, Vector2.SignedAngle(Vector2.right, directionToLook), ref rotateVelocity, rotateSmoothTime);
        pivot.eulerAngles = new Vector3(0, 0, angle);

        //transform.position += pivot.right * (passiveSpeed * Time.deltaTime);
        rb.MovePosition((Vector2)rb.position + (Vector2)pivot.right * (passiveSpeed * Time.fixedDeltaTime));
    }
}