using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace LCShrinkRay.helper
{
    internal class DashHandler
    {
        public float lastDashedAt = 0f;
        public const float dashStamina = 0.15f;

        public enum Direction
        {
            Forward,
            Backward,
            Left,
            Right
        }

        internal Dictionary<KeyControl, Direction> keyDirectionMap = new Dictionary<KeyControl, Direction>
        {
            { Keyboard.current.wKey, Direction.Forward },
            { Keyboard.current.sKey, Direction.Backward },
            { Keyboard.current.aKey, Direction.Left },
            { Keyboard.current.dKey, Direction.Right }
        };

        internal Dictionary<Direction, int> dashProgressMap = new Dictionary<Direction, int>
        {
            { Direction.Forward, 0 },
            { Direction.Backward, 0 },
            { Direction.Left, 0 },
            { Direction.Right, 0 }
        };

        internal Dictionary<Direction, float> lastKeyChangeMap = new Dictionary<Direction, float>
        {
            { Direction.Forward, 0f },
            { Direction.Backward, 0f },
            { Direction.Left, 0f },
            { Direction.Right, 0f }
        };

        internal void OnUpdate()
        {
            if(Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.wKey.wasReleasedThisFrame)
                HandleDashInDirection(Direction.Forward);

            else if(Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.sKey.wasReleasedThisFrame)
                HandleDashInDirection(Direction.Backward);

            else if(Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.aKey.wasReleasedThisFrame)
                HandleDashInDirection(Direction.Left);

            else if(Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.dKey.wasReleasedThisFrame)
                HandleDashInDirection(Direction.Right);
        }

        internal void HandleDashInDirection(Direction direction)
        {
            var currentTime = Time.time;
            if (currentTime - lastDashedAt < 0.75f)
                return;

            var diff = currentTime - lastKeyChangeMap[direction];
            lastKeyChangeMap[direction] = currentTime;
            if (diff < 0.2f)
                dashProgressMap[direction]++;
            else
            {
                dashProgressMap[direction] = 0;
                return;
            }

            Plugin.Log("dashIntervalHitInRow: " + dashProgressMap[direction] + " [" + (Keyboard.current.wKey.wasReleasedThisFrame ? "Release" : "Press") + "]");

            if (dashProgressMap[direction] >= 3)
                PerformDash(direction);
        }

        internal void PerformDash(Direction direction)
        {
            if (PlayerInfo.CurrentPlayer.sprintMeter >= dashStamina)
            {
                Vector3 directionalVector = PlayerInfo.CurrentPlayer.gameplayCamera.transform.forward;
                directionalVector.y = 0f; // Don't throw me up

                switch(direction)
                {
                    case Direction.Backward:
                        directionalVector *= -1;
                        break;
                    case Direction.Left:
                        directionalVector = Quaternion.Euler(0, -90, 0) * directionalVector;
                        break;
                    case Direction.Right:
                        directionalVector = Quaternion.Euler(0, 90, 0) * directionalVector;
                        break;
                    default: break; // Forward already correct
                }

                Plugin.Log("Performing dash in direction: " + directionalVector);
                coroutines.PlayerThrowAnimation.StartRoutine(PlayerInfo.CurrentPlayer, directionalVector, 15f, 0.4f); // Perform dash
                PlayerInfo.CurrentPlayer.sprintMeter = Mathf.Clamp(PlayerInfo.CurrentPlayer.sprintMeter - dashStamina, 0f, 1f);
                lastDashedAt = Time.time;
            }
            else
                Plugin.Log("Too exhausted for dash.");

            dashProgressMap[direction] = 0;
        }
    }
}
