using GameNetcodeStuff;
using LittleCompany.components;
using LittleCompany.Config;
using LittleCompany.helper;
using LittleCompany.patches;
using System;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

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

        public static float NextShrunkenSizeOf(PlayerControllerB targetPlayer)
        {
            var playerSize = PlayerInfo.SizeOf(targetPlayer);
            var nextShrunkenSize = Mathf.Max(PlayerInfo.Rounded(playerSize - ModConfig.Instance.values.sizeChangeStep), 0f);
            if ((nextShrunkenSize + (ModConfig.SmallestSizeChange / 2)) <= DeathShrinkMargin)
                return 0f;
            else
                return nextShrunkenSize;
        }

        public static float NextEnlargedSizeOf(PlayerControllerB targetPlayer)
        {
            var playerSize = PlayerInfo.SizeOf(targetPlayer);
            Plugin.Log("NextEnlargedSizeOf -> " + playerSize);
            return Mathf.Min(PlayerInfo.Rounded(playerSize + ModConfig.Instance.values.sizeChangeStep), ModConfig.Instance.values.maximumPlayerSize);
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

        public static bool CanApplyModificationTo(PlayerControllerB targetPlayer, ModificationType type)
        {
            if (targetPlayer == null || targetPlayer.isPlayerDead || targetPlayer.isClimbingLadder || targetPlayer.inTerminalMenu)
                return false;

            switch (type)
            {
                case ModificationType.Normalizing:
                    if (PlayerInfo.IsNormalSize(targetPlayer))
                        return false;
                    break;

                case ModificationType.Shrinking:
                    if (ScalingOf(targetPlayer).GettingScaled) return false;

                    var nextShrunkenSize = NextShrunkenSizeOf(targetPlayer);
                    if ((!ModConfig.Instance.values.deathShrinking || !targetPlayer.AllowPlayerDeath()) && Mathf.Approximately(nextShrunkenSize, 0f) )
                        return false;

                    break;

                case ModificationType.Enlarging:
                    if (ScalingOf(targetPlayer).GettingScaled) return false;

                    var nextIncreasedSize = NextEnlargedSizeOf(targetPlayer);
                    if (Mathf.Approximately(nextIncreasedSize, ModConfig.Instance.values.maximumPlayerSize))
                        return false;
                    break;

                default:
                    return false; // Not supported yet
            }

            if (GrabbablePlayerList.TryFindGrabbableObjectForPlayer(targetPlayer.playerClientId, out GrabbablePlayerObject gpo))
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

            bool targetingUs = PlayerInfo.IsCurrentPlayer(targetPlayer);
            bool wasShrunkBefore = PlayerInfo.IsShrunk(targetPlayer);

            switch (type)
            {
                case ModificationType.Normalizing:
                    {
                        Plugin.Log("Normalizing player [" + targetPlayer.playerClientId + "]");
                        ScalingOf(targetPlayer).ScaleTo(ModConfig.Instance.values.defaultPlayerSize);

                        if (onComplete != null)
                            onComplete();
                        break;
                    }

                case ModificationType.Shrinking:
                    {
                        var nextShrunkenSize = NextShrunkenSizeOf(targetPlayer);
                        Plugin.Log("Shrinking player [" + targetPlayer.playerClientId + "] to size: " + nextShrunkenSize);
                        ScalingOf(targetPlayer).ScaleOverTimeTo(nextShrunkenSize, () =>
                        {
                            if (nextShrunkenSize < DeathShrinkMargin)
                            {
                                // Poof Target to death because they are too small to exist
                                if(ShrinkRayFX.TryCreateDeathPoofAt(out GameObject deathPoof, targetPlayer.transform.position) && targetPlayer.movementAudio != null)
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

                        if (nextIncreasedSize >= 1f && GrabbablePlayerList.TryFindGrabbableObjectForPlayer(targetPlayer.playerClientId, out GrabbablePlayerObject gpo))
                            gpo.EnableInteractTrigger(false);

                        ScalingOf(targetPlayer).ScaleOverTimeTo(nextIncreasedSize, () =>
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
