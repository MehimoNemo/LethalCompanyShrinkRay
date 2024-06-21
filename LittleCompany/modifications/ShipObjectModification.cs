using GameNetcodeStuff;
using LittleCompany.components;
using LittleCompany.Config;
using LittleCompany.helper;
using System;
using Unity.Netcode;
using UnityEngine;

using static LittleCompany.components.TargetScaling<PlaceableShipObject>;

namespace LittleCompany.modifications
{
    public class ShipObjectModification : Modification
    {
        #region Methods
        internal static ShipObjectScaling ScalingOf(PlaceableShipObject target)
        {
            if (!target.TryGetComponent(out ShipObjectScaling scaling))
                scaling = target.gameObject.AddComponent<ShipObjectScaling>();
            return scaling;
        }

        public static float SizeChangeStep(float multiplier = 1f) => Mathf.Max(ModConfig.Instance.values.shipObjectSizeChangeStep * multiplier, ModConfig.SmallestSizeChange);

        public static float NextShrunkenSizeOf(PlaceableShipObject targetObject, float multiplier = 1f) => Mathf.Max(Rounded(ScalingOf(targetObject).RelativeScale - SizeChangeStep(multiplier)), 0f);

        public static float NextIncreasedSizeOf(PlaceableShipObject targetObject, float multiplier = 1f) => Rounded(ScalingOf(targetObject).RelativeScale + SizeChangeStep(multiplier));

        public static bool CanApplyModificationTo(PlaceableShipObject targetObject, ModificationType type, PlayerControllerB playerModifiedBy, float multiplier = 1f)
        {
            if (targetObject == null)
                return false;

            var scaling = ScalingOf(targetObject);
            if (scaling == null)
                return false;

            switch (type)
            {
                case ModificationType.Normalizing:
                    if (scaling.RelativeScale == 1f)
                        return false;
                    break;

                case ModificationType.Shrinking:
                    var nextShrunkenSize = NextShrunkenSizeOf(targetObject, multiplier);

                    if (targetObject.parentObject.name == "Terminal" && nextShrunkenSize < ModConfig.SmallestSizeChange)
                        return false;

                    if (nextShrunkenSize == scaling.RelativeScale)
                        return false;

                    break;

                case ModificationType.Enlarging:
                    var nextIncreasedSize = NextIncreasedSizeOf(targetObject, multiplier);
                    if (nextIncreasedSize == scaling.RelativeScale)
                        return false;

                    break;

                default:
                    return false; // Not supported yet
            }

            return true;
        }

        public static void ApplyModificationTo(PlaceableShipObject targetObject, ModificationType type, PlayerControllerB playerModifiedBy, float multiplier = 1f, Action onComplete = null)
        {
            if (targetObject?.gameObject == null) return;

            var scaling = ScalingOf(targetObject);
            if (scaling == null) return;

            switch (type)
            {
                case ModificationType.Normalizing:
                    {
                        var normalizedSize = 1f;
                        Plugin.Log("Normalizing ship object [" + targetObject.name + "]");
                        scaling.ScaleOverTimeTo(normalizedSize, playerModifiedBy, () =>
                        {
                            if (onComplete != null)
                                onComplete();
                        });
                        break;
                    }

                case ModificationType.Shrinking:
                    {
                        var previousSize = ScalingOf(targetObject).RelativeScale;
                        var nextShrunkenSize = NextShrunkenSizeOf(targetObject, multiplier);
                        Plugin.Log("Shrinking ship object [" + targetObject.name + "] to size: " + nextShrunkenSize);

                        scaling.ScaleOverTimeTo(nextShrunkenSize, playerModifiedBy, () =>
                        {
                            if (Mathf.Approximately(nextShrunkenSize, 0f))
                            {
                                // Logic from ShipBuildModeManager.StoreObjectLocalClient
                                if (!StartOfRound.Instance.unlockablesList.unlockables[targetObject.unlockableID].spawnPrefab)
                                    targetObject.parentObject.disableObject = true;

                                if (!PlayerInfo.IsHost)
                                    StartOfRound.Instance.unlockablesList.unlockables[targetObject.unlockableID].inStorage = true;

                                if (PlayerInfo.CurrentPlayer == playerModifiedBy)
                                {
                                    HUDManager.Instance.UIAudio.PlayOneShot(ShipBuildModeManager.Instance.storeItemSFX);
                                    HUDManager.Instance.DisplayTip("Item stored!", "You can see stored items in the terminal by using command 'STORAGE'", isWarning: false, useSave: false, "LC_StorageTip");

                                    ShipBuildModeManager.Instance.StoreObjectServerRpc(targetObject.parentObject.GetComponent<NetworkObject>(), (int)playerModifiedBy.playerClientId);
                                }

                                ScalingOf(targetObject).ScaleTo(1f, playerModifiedBy);
                            }

                            if (onComplete != null)
                                onComplete();
                        }, default, Mode.Linear);

                        break;
                    }

                case ModificationType.Enlarging:
                    {
                        var nextIncreasedSize = NextIncreasedSizeOf(targetObject, multiplier);
                        Plugin.Log("Enlarging ship object [" + targetObject.name + "] to size: " + nextIncreasedSize);
                        scaling.ScaleOverTimeTo(nextIncreasedSize, playerModifiedBy, () =>
                        {
                            if (onComplete != null)
                                onComplete();
                        }, default, Mode.Linear);

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
