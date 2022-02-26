using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SmoothDampLineRenderer : SmoothDampJoints
{
    public int segmentCount = 10;
    private LineRenderer _lineRenderer;

    protected override void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        
        Transform currentJoint = transform;
        for (int i = 0; i < segmentCount - 1; i++)
        {
            Transform joint = new GameObject(i.ToString()).transform;
            joint.parent = currentJoint;
            currentJoint = joint;
        }
        
        base.Start();

        _lineRenderer.positionCount = joints.Count;
    }

    protected override void Update()
    {
        base.Update();

        for (int i = 0; i < joints.Count; i++)
        {
            _lineRenderer.SetPosition(i, joints[i].position);
        }
    }
}
