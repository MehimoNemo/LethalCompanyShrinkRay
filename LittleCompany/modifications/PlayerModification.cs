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
        public static float DefaultPlayerSize => ModConfig.Instance.values.defaultPlayerSize;

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

        public static float SizeChangeStep(float multiplier = 1f) => Mathf.Max(ModConfig.Instance.values.playerSizeChangeStep * multiplier, ModConfig.SmallestSizeChange);

        public static float NextShrunkenSizeOf(PlayerControllerB targetPlayer, float multiplier = 1f)
        {
            var playerSize = PlayerInfo.SizeOf(targetPlayer);
            var nextShrunkenSize = Mathf.Max(Rounded(playerSize - SizeChangeStep(multiplier)), 0f);
            if ((nextShrunkenSize + (ModConfig.SmallestSizeChange / 2)) <= DeathShrinkMargin)
                return 0f;
            else if (ModConfig.Instance.values.playerSizeStopAtDefault && !PlayerInfo.IsDefaultSize(targetPlayer) && playerSize > DefaultPlayerSize && nextShrunkenSize < DefaultPlayerSize)
                return DefaultPlayerSize;
            else
                return nextShrunkenSize;
        }

        public static float NextEnlargedSizeOf(PlayerControllerB targetPlayer, float multiplier = 1f)
        {
            var playerSize = PlayerInfo.SizeOf(targetPlayer);
            var nextEnlargedSize = Mathf.Min(Rounded(playerSize + SizeChangeStep(multiplier)), ModConfig.Instance.values.maximumPlayerSize);

            if (ModConfig.Instance.values.playerSizeStopAtDefault && !PlayerInfo.IsDefaultSize(targetPlayer) && playerSize < DefaultPlayerSize && nextEnlargedSize > DefaultPlayerSize)
                return DefaultPlayerSize;
            else
                return nextEnlargedSize;
        }

        public static void TransitionedToShrunk(PlayerControllerB targetPlayer)
        {
            if (PlayerInfo.IsCurrentPlayer(targetPlayer))
            {
                if(!ModConfig.Instance.values.logicalMultiplier)
                    PlayerMultiplierPatch.Modify();
                Vents.EnableVents();
            }
        }

        public static void TransitionedFromShrunk(PlayerControllerB targetPlayer)
        {
            if (PlayerInfo.IsCurrentPlayer(targetPlayer))
            {
                if (!ModConfig.Instance.values.logicalMultiplier)
                    PlayerMultiplierPatch.Reset();
                Vents.DisableVents();
            }
        }

        public static bool CanApplyModificationTo(PlayerControllerB targetPlayer, ModificationType type, PlayerControllerB playerModifiedBy, float multiplier = 1f)
        {
            if (targetPlayer == null || targetPlayer.isPlayerDead || targetPlayer.isClimbingLadder)
                return false;

            var playerSize = PlayerInfo.SizeOf(targetPlayer);

            switch (type)
            {
                case ModificationType.Normalizing:
                    if (PlayerInfo.IsDefaultSize(targetPlayer))
                        return false;
                    break;

                case ModificationType.Shrinking:
                    if (GoombaStomp.IsGettingGoombad(targetPlayer)) return false;
                    if (ScalingOf(targetPlayer).GettingScaled) return false;

                    var nextShrunkenSize = NextShrunkenSizeOf(targetPlayer, multiplier);
                    if (Mathf.Approximately(playerSize, nextShrunkenSize)) return false; // No change

                    if (Mathf.Approximately(nextShrunkenSize, 0f) && (!ModConfig.Instance.values.deathShrinking || !targetPlayer.AllowPlayerDeath()) )
                        return false;

                    break;

                case ModificationType.Enlarging:
                    if (GoombaStomp.IsGettingGoombad(targetPlayer)) return false;
                    if (ScalingOf(targetPlayer).GettingScaled) return false;

                    var nextIncreasedSize = NextEnlargedSizeOf(targetPlayer, multiplier);
                    if (Mathf.Approximately(playerSize, nextIncreasedSize)) return false; // No change
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

        public static void ApplyModificationTo(PlayerControllerB targetPlayer, ModificationType type, PlayerControllerB playerModifiedBy, float multiplier = 1f, Action onComplete = null)
        {
            if (targetPlayer == null) return;

            bool targetingUs = PlayerInfo.IsCurrentPlayer(targetPlayer);

            if (targetingUs && targetPlayer.inTerminalMenu)
                UnityEngine.Object.FindObjectOfType<Terminal>()?.QuitTerminal();

            switch (type)
            {
                case ModificationType.Normalizing:
                    {
                        GoombaStomp.StopGoombaOn(targetPlayer);

                        Plugin.Log("Normalizing player [" + targetPlayer.playerClientId + "]");
                        ScalingOf(targetPlayer).ScaleTo(ModConfig.Instance.values.defaultPlayerSize, playerModifiedBy);

                        if (onComplete != null)
                            onComplete();
                        break;
                    }

                case ModificationType.Shrinking:
                    {
                        var nextShrunkenSize = NextShrunkenSizeOf(targetPlayer, multiplier);
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
                        var nextIncreasedSize = NextEnlargedSizeOf(targetPlayer, multiplier);
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
