using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Net.Http.Headers;

public class SPWAgent : Agent
{
    private Player _playerScript;
    private GameManager _gameManager;

    private LayerMask _bossLayer;

    enum ControlStyle
    {
        Version1,
        Version2
    }

    [SerializeField] private ControlStyle controlStyle = ControlStyle.Version1;

    private float spawnerDamagedReward = 0.2f;
    private float bossDamagedReward = 0.35f;
    private float phaseCompleteReward = 0.6f;
    private float enemyKilledReward = 0.02f;
    private float healthPickedUpReward = 0.15f;
    private float spreadshotPickupReward = 0.15f;

    // Distance from boss pen
    private float minDistanceToBoss = 20.0f;
    private float zeroBossDistancePenalty = -0.4f;

    // Distance from middle reward
    private float middleRewardRadius = 40.0f;
    private float middleReward = 0.2f;

    // Penalties
    private float damageTakenPenalty = -0.5f;

    // Start is called before the first frame update
    void Start()
    {
        Application.runInBackground = true;

        _playerScript = GameObject.Find("Player").GetComponent<Player>();
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        _bossLayer = LayerMask.GetMask("Boss");

        if (controlStyle == ControlStyle.Version2)
        {
            spawnerDamagedReward = 0.15f;
            bossDamagedReward = 0.20f;
            phaseCompleteReward = 1f;
            enemyKilledReward = 0.0f;
            healthPickedUpReward = 0.15f;
            spreadshotPickupReward = 0.15f;

            minDistanceToBoss = 8.0f;
            zeroBossDistancePenalty = -0.2f;

            middleRewardRadius = 8.0f;
            middleReward = 0.05f;

            damageTakenPenalty = -1.0f;
        }

    }

    private void FixedUpdate()
    {
        AddReward(CalculateBossDistanceReward());
        AddReward(CalculateMiddleDistanceReward());
    }

    public override void OnEpisodeBegin()
    {
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position.x);
        sensor.AddObservation(transform.position.y);
        sensor.AddObservation(_playerScript.Lives);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (controlStyle == ControlStyle.Version1)
        {
            int rotate = actions.DiscreteActions[0];
            int strafe = actions.DiscreteActions[1];
            _playerScript.Movement_V1(rotate, strafe);
        }
        else
        {
            int action = actions.DiscreteActions[0];
            int strafe = actions.DiscreteActions[1];
            _playerScript.Movement_V2(action, strafe);
        }
    }

    public void HandleEpisodeEnd(int phaseReached, int score)
    {
        Academy.Instance.StatsRecorder.Add("Phase Reached", phaseReached);
        Academy.Instance.StatsRecorder.Add("Score", score);
        EndEpisode();
    }

    public void HandleEnemyKilled()
    {
        AddReward(enemyKilledReward);
    }

    public void HandleSpawnerDamaged()
    {
        AddReward(spawnerDamagedReward);
    }

    public void HandleBossDamaged()
    {
        AddReward(bossDamagedReward);
    }

    public void HandleDamageTaken(int damage)
    {
        AddReward(damageTakenPenalty * damage);
    }

    public void HandlePhaseCompletion()
    {
        AddReward(phaseCompleteReward);
    }

    public void HandleHealthPickup()
    {
        AddReward(healthPickedUpReward);
    }

    public void HandleSpreadshotPickup()
    {
        AddReward(spreadshotPickupReward);
    }

    private float GetDistanceToClosestBoss()
    {
        float closestDistance = Mathf.Infinity;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in enemies)
        {
            if (enemy.layer == _bossLayer)
            {
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                }
            }
        }

        return closestDistance;
    }

    private float CalculateBossDistanceReward()
    {
        if (GetDistanceToClosestBoss() < minDistanceToBoss)
        {
            return ((zeroBossDistancePenalty * -1.0f) / minDistanceToBoss) * GetDistanceToClosestBoss() + zeroBossDistancePenalty;
        } 
        else
        {
            return 0.0f;
        }
    }

    private float DistanceFromMiddle()
    {
        return Vector2.Distance(transform.position, _gameManager.Middle());
    }

    private float CalculateMiddleDistanceReward()
    {
        if (DistanceFromMiddle() < middleRewardRadius)
        {
            return (1 / DistanceFromMiddle()) * middleReward;
        }
        else
        {
            return 0.0f;
        }
    }
}
