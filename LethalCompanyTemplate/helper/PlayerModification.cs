using GameNetcodeStuff;
using LCShrinkRay.comp;
using LCShrinkRay.Config;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LCShrinkRay.helper
{
    public class PlayerModification
    {
        #region Properties
        internal static float DeathShrinkMargin => 0.1f;
        public enum ModificationType
        {
            Normalizing,
            Shrinking,
            Enlarging
        }

        internal static AudioClip deathPoofSFX;
        #endregion

        #region Methods

        public static float NextShrunkenSizeOf(PlayerControllerB targetPlayer)
        {

            var playerSize = PlayerInfo.SizeOf(targetPlayer);
            if (!ModConfig.Instance.values.deathShrinking)
            {
                if (playerSize - ModConfig.Instance.values.sizeChangeStep < DeathShrinkMargin) // deathShrink
                    return playerSize;
            }

            return playerSize - ModConfig.Instance.values.sizeChangeStep;
        }

        public static float NextIncreasedSizeOf(PlayerControllerB targetPlayer)
        {
            var playerSize = PlayerInfo.SizeOf(targetPlayer);
            //if (playerSize + ModConfig.Instance.values.sizeChangeStep > 1f) return;
            return playerSize + ModConfig.Instance.values.sizeChangeStep;
        }


        public static bool CanApplyModificationTo(PlayerControllerB targetPlayer, ModificationType type)
        {
            if (targetPlayer == null || targetPlayer.isPlayerDead || targetPlayer.isClimbingLadder)
                return false;

            switch (type)
            {
                case ModificationType.Normalizing:
                    if (PlayerInfo.IsNormalSize(targetPlayer))
                        return false;
                    break;

                case ModificationType.Shrinking:
                    var nextShrunkenSize = NextShrunkenSizeOf(targetPlayer);
                    if (nextShrunkenSize == PlayerInfo.SizeOf(targetPlayer) || (nextShrunkenSize == 0f && !targetPlayer.AllowPlayerDeath()))
                        return false;
                    break;

                case ModificationType.Enlarging:
                    var nextIncreasedSize = NextIncreasedSizeOf(targetPlayer);
                    if (nextIncreasedSize == PlayerInfo.SizeOf(targetPlayer))
                        return false;
                    break;

                default:
                    return false; // Not supported yet
            }

            if (GrabbablePlayerList.TryFindGrabbableObjectForPlayer(PlayerInfo.CurrentPlayerID, out GrabbablePlayerObject gpo))
            {
                if (gpo.playerHeldBy != null && gpo.playerHeldBy.playerClientId == targetPlayer.playerClientId)
                {
                    Plugin.Log("Attempting to shrink the player who holds us. Bad idea!");
                    return false;
                }
                if (gpo.IsOnSellCounter.Value)
                {
                    Plugin.Log("Attempting to shrink a player who is on the sell counter. Poor soul is already doomed, let's not do this..");
                    return false;
                }
            }

            return true;
        }

        public static void ApplyModificationTo(PlayerControllerB targetPlayer, ModificationType type, Action onComplete = null)
        {
            if (targetPlayer == null) return;

            bool targetingUs = targetPlayer.playerClientId == PlayerInfo.CurrentPlayerID;

            switch (type)
            {
                case ModificationType.Normalizing:
                    {
                        var normalizedSize = 1f;
                        Plugin.Log("Normalizing player [" + targetPlayer.playerClientId + "]");
                        coroutines.PlayerShrinkAnimation.StartRoutine(targetPlayer, normalizedSize, () =>
                        {
                            Plugin.Log("Finished ray shoot with type: " + type.ToString());
                            if (PlayerInfo.IsHost)
                                GrabbablePlayerList.RemovePlayerGrabbable(targetPlayer.playerClientId);

                            if (targetingUs)
                                Vents.DisableVents();

                            GrabbablePlayerList.UpdateWhoIsGrabbableFromPerspectiveOf(targetPlayer);

                            GrabbablePlayerList.ResetAnyPlayerModificationsFor(targetPlayer);

                            if (onComplete != null)
                                onComplete();
                        });
                        break;
                    }

                case ModificationType.Shrinking:
                    {
                        var nextShrunkenSize = NextShrunkenSizeOf(targetPlayer);
                        Plugin.Log("Shrinking player [" + targetPlayer.playerClientId + "] to size: " + nextShrunkenSize);
                        coroutines.PlayerShrinkAnimation.StartRoutine(targetPlayer, nextShrunkenSize, () =>
                        {
                            if (nextShrunkenSize < DeathShrinkMargin)
                            {
                                // Poof Target to death because they are too small to exist
                                if (ShrinkRayFX.TryCreateDeathPoofAt(out GameObject deathPoof, targetPlayer.transform.position, Quaternion.identity))
                                    UnityEngine.Object.Destroy(deathPoof, 4f);

                                targetPlayer.movementAudio.PlayOneShot(deathPoofSFX);

                                if (targetingUs)
                                    targetPlayer.KillPlayer(Vector3.down, false, CauseOfDeath.Crushing);
                            }

                            if (targetingUs && PlayerInfo.IsShrunk(nextShrunkenSize))
                                    Vents.EnableVents();

                            if (nextShrunkenSize < 1f && nextShrunkenSize > 0f && PlayerInfo.IsHost) // todo: create a mechanism that only allows larger players to grab small ones
                                GrabbablePlayerList.SetPlayerGrabbable(targetPlayer.playerClientId);

                            GrabbablePlayerList.UpdateWhoIsGrabbableFromPerspectiveOf(targetPlayer);

                            if (onComplete != null)
                                onComplete();
                        });

                        break;
                    }

                case ModificationType.Enlarging:
                    {
                        var nextIncreasedSize = NextIncreasedSizeOf(targetPlayer);
                        Plugin.Log("Enlarging player [" + targetPlayer.playerClientId + "] to size: " + nextIncreasedSize);
                        coroutines.PlayerShrinkAnimation.StartRoutine(targetPlayer, nextIncreasedSize, () =>
                        {
                            if (nextIncreasedSize >= 1f)
                            {
                                if (PlayerInfo.IsHost)
                                    GrabbablePlayerList.RemovePlayerGrabbable(targetPlayer.playerClientId);

                                if (targetingUs)
                                    Vents.DisableVents();
                            }

                            GrabbablePlayerList.UpdateWhoIsGrabbableFromPerspectiveOf(targetPlayer);

                            if (onComplete != null)
                                onComplete();
                        });

                        break;
                    }
                default:
                    return;
            }

            return;
        }
        #endregion
    }
}
