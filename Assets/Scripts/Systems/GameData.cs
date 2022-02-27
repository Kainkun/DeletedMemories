using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

public class GameData
{
    public static LayerMask playerMask;
    public static LayerMask defaultMask;
    public static LayerMask platformMask;
    public static LayerMask traversableMask;
    public static LayerMask opaqueMask;
    public static LayerMask airWormAvoidance;
    
    public void SetData()
    {
        playerMask = LayerMask.GetMask("Player") | LayerMask.GetMask("PlayerPlatformFall");
        defaultMask = LayerMask.GetMask("Default") | LayerMask.GetMask("TransparentDefault");
        platformMask = LayerMask.GetMask("Platform");
        traversableMask =  defaultMask | platformMask;
        opaqueMask = playerMask | LayerMask.GetMask("Default");
        airWormAvoidance = defaultMask | LayerMask.GetMask("PlayerCollisionIgnore");
        
    }
}