using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridWorldState
{
    // CONSTRUCTORS
    public GridWorldState(IntVector2 playerPos, UInt16 app, bool key, UInt16 chest, UInt16 monster)
    {
        m_PlayerY = playerPos.y;
        m_PlayerX = playerPos.x;
        m_AppleStatus = app;
        m_KeyStatus = key;
        m_ChestStatus = chest;
        m_MonsterStatus = monster;
        m_Visits = 0;
    }

    public int Visits
    {
        get { return m_Visits; }
    }

    public void IncrementVisits()
    {
        m_Visits += 1;
    }

    // OVERRIDES
    static public bool operator==(GridWorldState left, GridWorldState right)
    {
        return (left.m_AppleStatus == right.m_AppleStatus)
            && (left.m_PlayerX == right.m_PlayerX) && (left.m_PlayerY == right.m_PlayerY)
            && (left.m_KeyStatus == right.m_KeyStatus) && (left.m_ChestStatus == right.m_ChestStatus)
            && (left.m_MonsterStatus == right.m_MonsterStatus);
    }

    static public bool operator!=(GridWorldState left, GridWorldState right)
    {
        return !(left == right);
    }

    public override bool Equals(object other)
    {
        if (other == null)
        {
            return false;
        }
        if (other is not GridWorldState)
        {
            return false;
        }
        return this == (GridWorldState)other;
    }

    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 23 + m_PlayerX.GetHashCode();
        hash = hash * 23 + m_PlayerX.GetHashCode();
        hash = hash * 23 + m_AppleStatus.GetHashCode();
        hash = hash * 23 + m_KeyStatus.GetHashCode();
        hash = hash * 23 + m_ChestStatus.GetHashCode();
        hash = hash * 23 + m_MonsterStatus.GetHashCode();
        return hash;
    }

    // MEMBERS
    private int m_PlayerX;
    private int m_PlayerY;
    private UInt16 m_AppleStatus;
    private bool m_KeyStatus;
    private UInt16 m_ChestStatus;
    private UInt16 m_MonsterStatus;
    private int m_Visits;
}
