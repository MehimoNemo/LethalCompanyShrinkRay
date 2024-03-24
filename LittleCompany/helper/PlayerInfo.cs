using GameNetcodeStuff;
using LittleCompany.components;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LittleCompany.helper
{
    internal class PlayerInfo
    {
        public static void Cleanup()
        {
            _cameraVisor = null;
            _localArms = null;
        }

        public static bool IsHost => NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer;

        public static bool IsCurrentPlayerGrabbed()
        {
            if(!CurrentPlayerID.HasValue) return false;

            if (GrabbablePlayerList.TryFindGrabbableObjectForPlayer(CurrentPlayerID.Value, out GrabbablePlayerObject gpo))
                return gpo.playerHeldBy != null;

            return false;
        }

        public static List<GameObject> AllPlayers => StartOfRound.Instance.allPlayerScripts.Where(pcb => pcb.isPlayerControlled).Select(pcb => pcb.gameObject).ToList();

        public static PlayerControllerB ControllerFromID(ulong playerID)
        {
            foreach(var pcb in StartOfRound.Instance.allPlayerScripts)
            {
                if (pcb.playerClientId == playerID)
                    return pcb;
            }
            return null;
        }

        public static ulong? IDFromObject(GameObject gameObject)
        {
            if (!gameObject.name.Contains('('))
                return null;

            int startIndex = gameObject.name.IndexOf("(");
            int endIndex = gameObject.name.IndexOf(")");
            return ulong.Parse(gameObject.name.Substring(startIndex + 1, endIndex - startIndex - 1));
        }

        public static PlayerControllerB CurrentPlayer => GameNetworkManager.Instance.localPlayerController;

        public static ulong? CurrentPlayerID => CurrentPlayer?.playerClientId;

        public static float CurrentPlayerScale => SizeOf(CurrentPlayer);

        public static float SizeOf(PlayerControllerB player) => SizeOf(player?.gameObject);

        public static float SizeOf(GameObject playerObject) => playerObject == null ? 1f : Rounded(playerObject.transform.localScale.y);

        public static float Rounded(float unroundedValue) => Mathf.Round(unroundedValue * 100f) / 100f; // round to 2 digits

        public static bool IsShrunk(PlayerControllerB player) => IsShrunk(player.gameObject);

        public static bool IsShrunk(GameObject playerObject)
        {
            if (playerObject == null)
                return false;

            return IsShrunk(playerObject.transform.localScale.x);
        }

        public static bool IsShrunk(float size) => Rounded(size) < 1f;

        public static bool IsNormalSize(PlayerControllerB player) => SizeOf(player) == 1f;

        public static bool IsCurrentPlayerShrunk => IsShrunk(CurrentPlayer);

        public static GrabbableObject HeldItem(PlayerControllerB pcb)
        {
            if (pcb != null && pcb.isHoldingObject && pcb.ItemSlots[pcb.currentItemSlot] != null)
                return pcb.ItemSlots[pcb.currentItemSlot];

            return null;
        }

        public static GrabbableObject CurrentPlayerHeldItem => HeldItem(CurrentPlayer);

        public static Transform BodyTransformOf(PlayerControllerB pcb) => pcb?.gameObject?.transform.Find("ScavengerModel/metarig");

        public static Transform SpineOf(PlayerControllerB pcb) => BodyTransformOf(pcb)?.Find("spine");

        public static void ScaleLocalPlayerBodyParts()
        {
            AdjustLocalArms();
            AdjustLocalMask();
        }

        private static Vector3 defaultArmScale = Vector3.one;
        private static Vector3 defaultArmPosition = Vector3.one;
        private static Transform _localArms = null;
        public static Transform LocalArms
        {
            get
            {
                if (_localArms == null)
                {
                    _localArms = BodyTransformOf(CurrentPlayer)?.Find("ScavengerModelArmsOnly"); // our locally visible pair of hands
                    if (_localArms != null)
                    {
                        Plugin.Log("Arms found!");
                        defaultArmScale = _localArms.localScale;
                        defaultArmPosition = _localArms.localPosition;
                    }
                }
                return _localArms;
            }
        }

        public static void AdjustLocalArms()
        {
            if (LocalArms == null) return;

            LocalArms.localScale = CalcLocalArmScale();
        }

        public static Vector3 CalcLocalArmScale()
        {
            var scale = SizeOf(CurrentPlayer);
            return new Vector3()
            {
                x = 0.35f * scale + 0.58f,
                y = -0.0625f * scale + 1.0625f,
                z = -0.125f * scale + 1.15f
            };
        }

        private static Vector3 defaultMaskScale = Vector3.one;
        private static Vector3 defaultMaskPos = Vector3.zero;
        private static Transform _cameraVisor = null;
        public static Transform CameraVisor
        {
            get
            {
                if (_cameraVisor == null)
                {
                    var helmet = GameObject.Find("ScavengerHelmet");
                    if (helmet == null) return null;

                    _cameraVisor = helmet.GetComponent<Transform>();
                    if (_cameraVisor == null) return null;

                    defaultMaskScale = _cameraVisor.localScale;
                    defaultMaskPos = _cameraVisor.localPosition;
                }
                return _cameraVisor;
            }
        }

        public static void EnableCameraVisor(bool enable = true)
        {
            if (CameraVisor == null || CameraVisor.gameObject == null) return;
            var renderer = CameraVisor.gameObject.GetComponent<MeshRenderer>();
            if(renderer != null)
                renderer.enabled = enable;
        }

        public static void AdjustLocalMask()
        {
            if (CameraVisor == null) return;

            // todo: disappears when size < 0.3f
            CameraVisor.localScale = defaultMaskScale * SizeOf(CurrentPlayer);
            CameraVisor.localPosition = defaultMaskPos * SizeOf(CurrentPlayer);
        }

        public static float CalculateWeightFor(PlayerControllerB player)
        {
            if(player?.ItemSlots == null) return 1f;

            float weight = 1f;

            foreach (var item in player.ItemSlots)
                if (item != null)
                    weight += Mathf.Clamp(item.itemProperties.weight - 1f, 0f, 10f);

            return weight;
        }

        public static void UpdateWeatherForPlayer(PlayerControllerB targetPlayer)
        {
            var audioPresetIndex = targetPlayer.isInsideFactory ? 2 : 3;
            var audioReverbPresets = Object.FindObjectOfType<AudioReverbPresets>();
            if (audioReverbPresets != null && audioReverbPresets.audioPresets.Length > audioPresetIndex)
            {
                Plugin.Log("Change audio reverb preset (to affect weather)");
                audioReverbPresets.audioPresets[audioPresetIndex].ChangeAudioReverbForPlayer(targetPlayer);
            }
        }
    }
}
