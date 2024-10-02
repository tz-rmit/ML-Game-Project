using Newtonsoft.Json.Bson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class QTable
{
    // CONSTRUCTORS
    public QTable()
    {
        m_QTable = new Dictionary<GridWorldState, float[]>();
        m_Lookup = new Dictionary<GridWorldState, GridWorldState>();
    }

    public int GreedyAction(GridWorldState state)
    {
        if (!m_QTable.ContainsKey(state))
        {
            this.Add(state);
        }

        float max = m_QTable[state][0];
        int maxIndex = 0;
        for (int i = 1; i < m_QTable[state].Length; i++)
        {
            if (m_QTable[state][i] > max)
            {
                max = m_QTable[state][i];
                maxIndex = i;
            }
            else if (m_QTable[state][i] == max)
            {
                if (UnityEngine.Random.value >= 0.5)
                {
                    maxIndex = i;
                }
            }
        }
        return maxIndex;
    }

    public float ArgMax(GridWorldState state)
    {
        if (!m_QTable.ContainsKey(state))
        {
            this.Add(state);
        }
        float max = 0.0f;
        for (int i = 0; i < m_QTable[state].Length; i++)
        {
            if (m_QTable[state][i] >= max)
            {
                max = m_QTable[state][i];
            }
        }
        return max;
    }

    public float this[GridWorldState state, int action]
    {
        get
        {
            if (m_QTable.ContainsKey(state))
            {
                return m_QTable[state][action];
            }
            else
            {
                return float.NaN;
            }
        }
        set
        {
            if (!m_QTable.ContainsKey(state))
            {
                this.Add(state);
            }

            m_QTable[state][action] = value;
        }
    }

    public void IncrementVisitsOfState(GridWorldState state)
    {
        if (m_QTable.ContainsKey(state))
        {
            m_Lookup[state].IncrementVisits();
        }
    }

    public int GetVisitsOfState(GridWorldState state)
    {
        if (m_QTable.ContainsKey(state))
        {
            return m_Lookup[state].Visits;
        }
        // Will crash
        return -1;
    }

    public void Add(GridWorldState state)
    {
        if (!m_QTable.ContainsKey(state))
        {
            m_QTable.Add(state, new float[5]);
            m_Lookup.Add(state, state);
        }
    }
    

    private Dictionary<GridWorldState, float[]> m_QTable;
    private Dictionary<GridWorldState, GridWorldState> m_Lookup;
}
