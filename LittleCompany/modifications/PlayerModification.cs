using GameNetcodeStuff;
using LittleCompany.components;
using LittleCompany.Config;
using LittleCompany.coroutines;
using LittleCompany.helper;
using LittleCompany.patches;
using System;
using UnityEngine;

namespace LittleCompany.modifications
{
    public class PlayerModification : Modification
    {
        #region Methods
        internal static PlayerScaling ScalingOf(PlayerControllerB target)
        {
            if (!target.TryGetComponent(out PlayerScaling scaling))
                scaling = target.gameObject.AddComponent<PlayerScaling>();
            return scaling;
        }

        internal static bool IsGettingScaled(PlayerControllerB target)
        {
            if (!target.TryGetComponent(out PlayerScaling scaling))
                return false;

            return scaling.GettingScaled;
        }

        public static float NextShrunkenSizeOf(PlayerControllerB targetPlayer)
        {
            var playerSize = PlayerInfo.SizeOf(targetPlayer);
            var nextShrunkenSize = Mathf.Max(Rounded(playerSize - Plugin.Config.PLAYER_SIZE_STEP_CHANGE), 0f);
            if ((nextShrunkenSize + (ModConfig.SmallestSizeChange / 2)) <= DeathShrinkMargin)
                return 0f;
            else
                return nextShrunkenSize;
        }

        public static float NextEnlargedSizeOf(PlayerControllerB targetPlayer)
        {
            var playerSize = PlayerInfo.SizeOf(targetPlayer);
            Plugin.Log("NextEnlargedSizeOf -> " + playerSize);
            return Mathf.Min(Rounded(playerSize + Plugin.Config.PLAYER_SIZE_STEP_CHANGE), Plugin.Config.MAXIMUM_PLAYER_SIZE);
        }

        public static void TransitionedToShrunk(PlayerControllerB targetPlayer)
        {
            if (PlayerInfo.IsCurrentPlayer(targetPlayer))
            {
                PlayerMultiplierPatch.Modify();
                Vents.EnableVents();
            }
        }

        public static void TransitionedFromShrunk(PlayerControllerB targetPlayer)
        {
            if (PlayerInfo.IsCurrentPlayer(targetPlayer))
            {
                PlayerMultiplierPatch.Reset();
                Vents.DisableVents();
            }
        }

        public static bool CanApplyModificationTo(PlayerControllerB targetPlayer, ModificationType type, PlayerControllerB playerModifiedBy)
        {
            if (targetPlayer == null || targetPlayer.isPlayerDead || targetPlayer.isClimbingLadder || targetPlayer.inTerminalMenu)
                return false;

            switch (type)
            {
                case ModificationType.Normalizing:
                    if (PlayerInfo.IsDefaultSize(targetPlayer))
                        return false;
                    break;

                case ModificationType.Shrinking:
                    if (GoombaStomp.IsGettingGoombad(targetPlayer)) return false;
                    if (ScalingOf(targetPlayer).GettingScaled) return false;

                    var nextShrunkenSize = NextShrunkenSizeOf(targetPlayer);
                    if ((!Plugin.Config.DEATH_SHRINKING || !targetPlayer.AllowPlayerDeath()) && Mathf.Approximately(nextShrunkenSize, 0f) )
                        return false;

                    break;

                case ModificationType.Enlarging:
                    if (GoombaStomp.IsGettingGoombad(targetPlayer)) return false;
                    if (ScalingOf(targetPlayer).GettingScaled) return false;

                    var nextIncreasedSize = NextEnlargedSizeOf(targetPlayer);
                    if (Mathf.Approximately(nextIncreasedSize, Plugin.Config.MAXIMUM_PLAYER_SIZE))
                        return false;
                    break;

                default:
                    return false; // Not supported yet
            }

            var grabbables = GrabbablePlayerList.FindGrabbableObjectsFor(targetPlayer.playerClientId);
            if (grabbables.holderGPO != null)
                return false;

            if(grabbables.grabbedGPO != null && (grabbables.grabbedGPO.playerHeldBy != null || grabbables.grabbedGPO.IsOnSellCounter.Value))
                return false;

            return true;
        }

        public static void ApplyModificationTo(PlayerControllerB targetPlayer, ModificationType type, PlayerControllerB playerModifiedBy, Action onComplete = null)
        {
            if (targetPlayer == null) return;

            bool targetingUs = PlayerInfo.IsCurrentPlayer(targetPlayer);

            switch (type)
            {
                case ModificationType.Normalizing:
                    {
                        GoombaStomp.StopGoombaOn(targetPlayer);

                        Plugin.Log("Normalizing player [" + targetPlayer.playerClientId + "]");
                        ScalingOf(targetPlayer).ScaleTo(Plugin.Config.DEFAULT_PLAYER_SIZE, playerModifiedBy);

                        if (onComplete != null)
                            onComplete();
                        break;
                    }

                case ModificationType.Shrinking:
                    {
                        var nextShrunkenSize = NextShrunkenSizeOf(targetPlayer);
                        Plugin.Log("Shrinking player [" + targetPlayer.playerClientId + "] to size: " + nextShrunkenSize);
                        ScalingOf(targetPlayer).ScaleOverTimeTo(nextShrunkenSize, playerModifiedBy, () =>
                        {
                            if (nextShrunkenSize < DeathShrinkMargin)
                            {
                                // Poof Target to death because they are too small to exist
                                if(Effects.TryCreateDeathPoofAt(out GameObject deathPoof, targetPlayer.transform.position) && targetPlayer.movementAudio != null)
                                    targetPlayer.movementAudio.PlayOneShot(deathPoofSFX);

                                if (targetingUs)
                                    targetPlayer.KillPlayer(Vector3.down, false, CauseOfDeath.Crushing);
                            }

                            if (onComplete != null)
                                onComplete();
                        });

                        break;
                    }

                case ModificationType.Enlarging:
                    {
                        var nextIncreasedSize = NextEnlargedSizeOf(targetPlayer);
                        Plugin.Log("Enlarging player [" + targetPlayer.playerClientId + "] to size: " + nextIncreasedSize);

                        ScalingOf(targetPlayer).ScaleOverTimeTo(nextIncreasedSize, playerModifiedBy, () =>
                        {
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
