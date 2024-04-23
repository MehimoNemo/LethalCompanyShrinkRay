using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace LittleCompany.helper
{
    internal class PrefabCloner
    {
        public static GameObject ClonePrefab(GameObject prefabToClone, string newName = null)
        {
            GameObject gameObject = UnityEngine.Object.Instantiate(prefabToClone, ClonedPrefabHolder.Holder.transform);
            if (newName != null)
            {
                gameObject.name = newName;
            }
            else
            {
                gameObject.name = prefabToClone.name;
            }
            NetworkObject networkObject = gameObject.GetComponent<NetworkObject>();
            if(networkObject != null)
            {
                byte[] value = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(Assembly.GetCallingAssembly().GetName().Name + gameObject.name));
                networkObject.GlobalObjectIdHash = BitConverter.ToUInt32(value, 0);
            }
            return gameObject;
        }

        internal static class ClonedPrefabHolder
        {
            private static GameObject _Holder = null;

            internal static GameObject Holder
            {
                get
                {
                    if (_Holder == null)
                    {
                        _Holder = new GameObject("LittleCompanyClonedPrefabHolder");
                        _Holder.hideFlags = HideFlags.HideAndDontSave;
                        _Holder.SetActive(value: false);
                    }
                    return _Holder;
                }
            }
        }
    }
}
