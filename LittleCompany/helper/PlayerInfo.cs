using GameNetcodeStuff;
using LittleCompany.components;
using LittleCompany.Config;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace LittleCompany.helper
{
    internal class PlayerInfo
    {
        public static readonly float VanillaPlayerSize = 1f;
        public static float DefaultPlayerSize => ModConfig.Instance.values.defaultPlayerSize;
        public static void Cleanup()
        {
            _cameraVisor = null;
        }

        public static bool IsHost => NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer;

        public static bool IsCurrentPlayerGrabbed()
        {
            if(!CurrentPlayerID.HasValue) return false;

            if (GrabbablePlayerList.TryFindGrabbableObjectForPlayer(CurrentPlayerID.Value, out GrabbablePlayerObject gpo))
                return gpo.playerHeldBy != null;

            return false;
        }

        public static bool IsCurrentPlayer(PlayerControllerB player) => player?.playerClientId == CurrentPlayer?.playerClientId;

        public static float LargestPlayerSize
        {
            get
            {
                var scale = 0f;
                foreach (var player in AllPlayers)
                    scale = Mathf.Max(scale, SizeOf(player));
                return scale;
            }
        }

        public static List<PlayerControllerB> AllPlayers => StartOfRound.Instance.allPlayerScripts.Where(pcb => pcb.isPlayerControlled && !pcb.isPlayerDead).ToList();

        public static List<GameObject> AllPlayerObjects => AllPlayers.Select(pcb => pcb.gameObject).ToList();

        public static PlayerControllerB ControllerFromID(ulong playerID)
        {
            foreach(var player in AllPlayers)
            {
                if (player.playerClientId == playerID)
                    return player;
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

        public static float Rounded(float unroundedValue) => Mathf.Round(unroundedValue * 100f) / 100f; // round to 2 digits

        public static float SizeOf(PlayerControllerB player) => Rounded(player.transform.localScale.y);

        public static float CurrentPlayerScale => SizeOf(CurrentPlayer);

        public static bool LargerThan(PlayerControllerB player, float size) => (SizeOf(player) - (ModConfig.SmallestSizeChange / 2)) > size;

        public static bool SmallerThan(PlayerControllerB player, float size) => (SizeOf(player) + (ModConfig.SmallestSizeChange / 2)) < size;

        public static bool IsShrunk(PlayerControllerB player) => SmallerThan(player, VanillaPlayerSize);

        public static bool IsDefaultVanillaSize(PlayerControllerB player) => Mathf.Approximately(SizeOf(player), VanillaPlayerSize);

        public static bool IsDefaultSize(PlayerControllerB player) => Mathf.Approximately(SizeOf(player), DefaultPlayerSize);

        public static bool IsEnlarged(PlayerControllerB player) => LargerThan(player, VanillaPlayerSize);

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
            AdjustLocalVisor();
        }

        public static void RebuildRig(PlayerControllerB pcb)
        {
            if (pcb != null && pcb.playerBodyAnimator != null)
            {
                pcb.playerBodyAnimator.WriteDefaultValues();
                pcb.playerBodyAnimator.GetComponent<RigBuilder>()?.Build();
            }
        }

        private static Vector3 defaultMaskPos = new Vector3(0.01f, -0.05f, -0.05f);
        private static Transform _cameraVisor = null;
        public static Transform CameraVisor
        {
            get
            {
                if (_cameraVisor == null)
                {
                    var helmet = CurrentPlayer.localVisor;
                    if (helmet == null) return null;

                    _cameraVisor = helmet.transform;
                    if (_cameraVisor == null) return null;
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

        public static void AdjustLocalVisor()
        {
            CurrentPlayer.localVisorTargetPoint.localPosition = defaultMaskPos + VisorOffset(SizeOf(CurrentPlayer));
        }

        public static Vector3 VisorOffset(float size)
        {
            return new Vector3()
            {
                x = (size < 1 ? (Mathf.Pow(size, -2f) - 1) * 0.002f : (Mathf.Pow(size, -2f) - 1) * 0.006f),
                y = (size < 1 ? (Mathf.Pow(size, -2f) - 1) * -0.008f : (Mathf.Pow(size, -2f) - 1) * -0.038f),
                z = (size < 1 ? (Mathf.Pow(size, -2f) - 1) * -0.008f : (Mathf.Pow(size, -2f) - 1) * -0.037f)
            };
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
