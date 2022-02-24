using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

public class GameData
{
    public static LayerMask playerMask;
    public static LayerMask defaultGroundMask;
    public static LayerMask platformMask;
    public static LayerMask traversableMask;
    public static LayerMask opaqueMask;
    
    public void SetData()
    {
        playerMask = LayerMask.GetMask("Player") | LayerMask.GetMask("PlayerPlatformFall");
        defaultGroundMask = LayerMask.GetMask("Default");
        platformMask = LayerMask.GetMask("Platform");
        traversableMask =  defaultGroundMask | platformMask;
        opaqueMask = playerMask | defaultGroundMask;
    }
}