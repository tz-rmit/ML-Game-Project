using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngineInternal;

public class GridWorldAgent : MonoBehaviour
{
	// Don't modify these constants.
	private const int MAX_EPISODE_STEPS = 1000;
	private const int EPISODES_PER_YIELD = 25;
	private const int NUM_ACTIONS = 4;

	public enum ControlType
    {
		Keyboard,
		QLearning,
		SARSA
    }

	public ControlType controlType;

	// Number of training episodes so far.
	private int episodeCount;

	// The exploration rate.
	private float epsilon;

	// Frames per second once the initial training phases finishes.
	[Range(1, 10)]
	public int endOfTrainFPS;

	private GridWorld env;
	private GridWorldUI ui;

	public GridWorldTrainingParams[] trainingParams;
	private GridWorldTrainingParams tp;

	private QTable Q;

	private void Start()
    {
		episodeCount = 0;
		ui = GameObject.Find("Canvas").GetComponent<GridWorldUI>();

		env = GameObject.Find("MapLoader").GetComponent<GridWorld>();
		env.Initialise();

		// Load training parameters for the current map.
		tp = trainingParams[env.mapNum];

		// TODO: Per the spec, make epsilon decay from tp.epsilonStart to tp.epsilonEnd over the course of training.
		epsilon = tp.epsilonStart;

        // Initialise QTable
        Q = new QTable();

        if (controlType == ControlType.QLearning)
		{
			StartCoroutine(RunQLearning());
		}
		else if(controlType == ControlType.SARSA)
		{	
			StartCoroutine(RunSARSA());
		}
	}

    IEnumerator RunQLearning()
    {
		while (true)
		{
			env.Reset();

			// Initialise S
			GridWorldState state = new GridWorldState(env.GetPlayerPosition(), env.AppleStatus(), env.KeyStatus(), env.ChestStatus(), env.MonsterStatus());
			Q.Add(state);
			Q.IncrementVisitsOfState(state);

			// Calculate epsilon based on tp.epsilonStart, tp.epsilonEnd, episodeCount and tp.numTrainEpisodes.
			epsilon = (epsilon > tp.epsilonEnd ? ((tp.epsilonEnd - tp.epsilonStart) / tp.numTrainEpisodes) * episodeCount + tp.epsilonStart : tp.epsilonEnd);

			#region ControlFrameRate
			// Don't remove this code, it controls the frame rate.
			if (episodeCount >= tp.numTrainEpisodes)
			{
				yield return new WaitForSeconds(1.0f / endOfTrainFPS);
			}
            #endregion

            int steps = 0;

			while (!env.IsGameOver() && steps < MAX_EPISODE_STEPS)
			{
				// Choose A from S using policy derived from Q (epsilon-greedy)
				int action;
				if (UnityEngine.Random.value < epsilon)
				{
                    action = UnityEngine.Random.Range(0, 4);
                }
				else
				{
					action = Q.GreedyAction(state);
				}
				// Take action A, observe R, S'
				(float reward, bool gameOver) = env.Act(action);
                GridWorldState newState = new GridWorldState(env.GetPlayerPosition(), env.AppleStatus(), env.KeyStatus(), env.ChestStatus(), env.MonsterStatus());
				// Intrinsic reward
				reward += tp.intrinsicRewardStrength * (1 / (Mathf.Sqrt(Q.GetVisitsOfState(state) + 1)));
				Q.IncrementVisitsOfState(newState);

                // If S' is a terminal state (game over)
                if (gameOver)
				{
					// Q(S, A) <-- (1 - alpha) * Q(S, A) + alpha * R
					Q[state, action] = (1 - tp.alpha) * Q[state, action] + tp.alpha * reward;
                }
				// Else
				else
				{
					// Q(S, A) <-- (1 - alpha) * Q(S, A) + alpha * [R + gamma * max_a(Q(S', a))]
                    Q[state, action] = (1 - tp.alpha) * Q[state, action] + tp.alpha * (reward + tp.gamma * Q.ArgMax(newState));
                }
				// S <-- S'
				state = newState;

                steps++;

				#region ControlFrameRate
				// Don't remove this code, it controls the frame rate.
				if (episodeCount >= tp.numTrainEpisodes)
				{
					yield return new WaitForSeconds(1.0f / endOfTrainFPS);
				}
                #endregion
            }

            #region ControlFrameRate
            // Don't remove this code, it controls the frame rate.
            if (episodeCount % EPISODES_PER_YIELD == 0)
            {
				yield return null;
			}
            #endregion

            episodeCount++;
		}
    }

	class SARSAState
	{
		private int PlayerX;
		private int PlayerY;
		private int mapWidth;
		private int mapHeight;
		private UInt16 AppleStatus;
		private bool KeyStatus;
		private UInt16 MonsterStatus;
		public SARSAState(int x, int y, int w, int h, UInt16 a, bool k, UInt16 m)
		{
			PlayerX = x;
			PlayerY = y;
			mapWidth = w;
			mapHeight = h;
			AppleStatus = a;
			KeyStatus = k;
			MonsterStatus = m;
		}

		public int MaxStates {
			get
			{
				return (mapWidth * mapHeight) << 16;
			}
		}

		public int ID {
			get
			{
				int id = (PlayerX + (PlayerY * mapWidth)) << 16;
				id |= AppleStatus << 13;
				id |= (KeyStatus ? 1 : 0) << 12;
				id |= MonsterStatus;
				return id;
			}
		}

	}
	public class SARSATable
	{
		private float[,] _rewardTable;
		private int[] _visitsTable;

		public float this[int SID, int A]
		{
			get
			{
				return _rewardTable[SID,A];
			}
			set
			{
				_rewardTable[SID,A] = value;
			}
		}

		public SARSATable(int MaxStates)
		{
			_rewardTable = new float[MaxStates,4];
			_visitsTable = new int[MaxStates];
		}

		public void IncrementVisits(int SID)
		{
			_visitsTable[SID]++;
		}
		public int GetVisits(int SID)
		{
			return _visitsTable[SID];
		}
	}
	int SARSAChooseAction(SARSATable Q, SARSAState state, float epsilon)
	{		
		if (UnityEngine.Random.Range(0.0f, 1.0f) < epsilon)
		{
			return UnityEngine.Random.Range(0,4);
		}
		
		int highestAction = -1;
		float highestActionVal = float.NegativeInfinity;
		
		for (int i=0; i<4; i++)
		{

			if (Q[state.ID, i] > highestActionVal)
			{
				highestActionVal = Q[state.ID, i];
				highestAction = i;
			}
			else if (Q[state.ID, i] == highestActionVal)
			{
				//random tiebreaking
				if (UnityEngine.Random.Range(0f,1f) > 0.5f)
				{
					highestActionVal = Q[state.ID, i];
					highestAction = i;
				}
			}
		}
		return highestAction;

	}

	IEnumerator RunSARSA()
	{
		SARSAState initState = new SARSAState(env.GetPlayerPosition().x, env.GetPlayerPosition().y, env.GetMapSize().x, env.GetMapSize().y, 0, false, 0);
		SARSATable Q = new SARSATable(initState.MaxStates);
		while (true)
		{
			env.Reset();

			// Pseudocode to implement:
			// Initialise S
			SARSAState S = new SARSAState(env.GetPlayerPosition().x, env.GetPlayerPosition().y, env.GetMapSize().x, env.GetMapSize().y, env.AppleStatus(), env.KeyStatus(), env.MonsterStatus2());
			Q.IncrementVisits(S.ID);
			// Calculate epsilon based on tp.epsilonStart, tp.epsilonEnd, episodeCount and tp.numTrainEpisodes.
			epsilon = tp.epsilonStart - (((float)episodeCount / tp.numTrainEpisodes) * (tp.epsilonStart - tp.epsilonEnd));
			// Choose A from S using policy derived from Q (epsilon-greedy)
			int A = SARSAChooseAction(Q, S, epsilon);

			#region ControlFrameRate
			// Don't remove this code, it controls the frame rate.
			if (episodeCount >= tp.numTrainEpisodes)
			{
				yield return new WaitForSeconds(1.0f / endOfTrainFPS);
			}
            #endregion

            int steps = 0;

			while (!env.IsGameOver() && steps < MAX_EPISODE_STEPS)
			{
				// Placeholder code that selects a random action.
				// Delete once pseudocode below is implemented.
				//int randomAction = Random.Range(0, NUM_ACTIONS);
				//(float reward, bool gameOver) = env.Act(randomAction);
				// ====================================================

				// Pseudocode to implement:
				// Take action A, observe R, S'
				(float R, bool terminal) = env.Act(A);
				SARSAState Snext = new SARSAState(env.GetPlayerPosition().x, env.GetPlayerPosition().y, env.GetMapSize().x, env.GetMapSize().y, env.AppleStatus(), env.KeyStatus(), env.MonsterStatus2());
				// Choose A' from S' using policy derived from Q (epsilon-greedy)
				int Anext = SARSAChooseAction(Q, Snext, epsilon);
				// If S' is a terminal state (game over)
				//     Q(S, A) <-- (1 - alpha) * Q(S, A) + alpha * R
				// Else
				//     Q(S, A) <-- (1 - alpha) * Q(S, A) + alpha * [R + gamma * Q(S', A')]

				if (terminal)
				{
					Q[S.ID,A] = ((1.0f - tp.alpha) * Q[S.ID,A] + tp.alpha * R) + (tp.intrinsicRewardStrength * (1f / Mathf.Sqrt(Q.GetVisits(S.ID) + 1)));
				}
				else
				{
					Q[S.ID,A] = ((1.0f - tp.alpha) * Q[S.ID,A] + tp.alpha * (R + tp.gamma * Q[Snext.ID,Anext])) + (tp.intrinsicRewardStrength * (1f / Mathf.Sqrt(Q.GetVisits(S.ID) + 1)));
				}
				// S <-- S'
				S = Snext;
				Q.IncrementVisits(S.ID);
				// A <-- A'
				A = Anext;

				steps++;

				#region ControlFrameRate
				// Don't remove this code, it controls the frame rate.
				if (episodeCount >= tp.numTrainEpisodes)
				{
					yield return new WaitForSeconds(1.0f / endOfTrainFPS);
				}
				#endregion
			}

			#region ControlFrameRate
			// Don't remove this code, it controls the frame rate.
			if (episodeCount % EPISODES_PER_YIELD == 0)
			{
				yield return null;
			}
            #endregion

            episodeCount++;
		}
	}

	private void Update()
	{
		ui.UpdateTopRightText("Total reward: " + env.GetTotalEpisodeReward());
		ui.UpdateBottomRightText("Epsilon: " + epsilon.ToString("0.00"));

		if (controlType == ControlType.Keyboard)
		{
			ui.UpdateTopLeftText("Keyboard control mode");

			if (env.IsGameOver())
			{
				ui.UpdateBottomLeftText("Game over, press 'R' to reset");

				if (Input.GetKeyDown(KeyCode.R))
				{
					env.Reset();
				}
			}
			else
			{
				ui.UpdateBottomLeftText("Use arrow keys to move");

				if (Input.GetKeyDown(KeyCode.UpArrow))
				{
					env.Act(0);
				}
				else if (Input.GetKeyDown(KeyCode.DownArrow))
				{
					env.Act(1);
				}
				else if (Input.GetKeyDown(KeyCode.LeftArrow))
				{
					env.Act(2);
				}
				else if (Input.GetKeyDown(KeyCode.RightArrow))
				{
					env.Act(3);
				}
			}
		}
		else
		{
			ui.UpdateTopLeftText("Training Episode");

			if (episodeCount < tp.numTrainEpisodes)
			{
				ui.UpdateBottomLeftText(episodeCount.ToString() + " (" + (100.0f * episodeCount / tp.numTrainEpisodes).ToString("0.0") + "%)");
			}
			else
			{
				ui.UpdateBottomLeftText(episodeCount.ToString());
			}
		}
	}
}
