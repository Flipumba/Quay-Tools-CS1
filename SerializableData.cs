using ICities;
using UnityEngine;

namespace QuayTools
{
    /// <summary>Stores the network lines, texture paths, prop lines and locks inside the savegame.</summary>
    public class SerializableData : SerializableDataExtensionBase
    {
        private const string FenceKey = "QuayTools.Fences";      // read only: saves of v0.4.x
        private const string NetLineKey = "QuayTools.NetLines";
        private const string DecalKey = "QuayTools.Decals";
        private const string PropKey = "QuayTools.PropLines";
        private const string LockKey = "QuayTools.Locks";
        private const string PedKey = "QuayTools.NoPeds";
        private const string HideKey = "QuayTools.HideProps";

        public override void OnLoadData()
        {
            History.Clear();

            byte[] lines = serializableDataManager.LoadData(NetLineKey);
            NetLineStore.Load(lines);
            Debug.Log("[QuayTools] Loaded network lines (" + (lines == null ? 0 : lines.Length) + " bytes)");

            // saves of v0.4.x: the "land" and "water" fences become line 1 and line 2
            byte[] legacy = serializableDataManager.LoadData(FenceKey);
            if (legacy != null && legacy.Length > 0 && (lines == null || lines.Length == 0))
            {
                int n = NetLineStore.MigrateLegacy(legacy);
                Debug.Log("[QuayTools] Converted old fence settings of " + n + " segment(s) into network lines");
            }

            byte[] decals = serializableDataManager.LoadData(DecalKey);
            DecalStore.Load(decals);
            Debug.Log("[QuayTools] Loaded decal paths (" + (decals == null ? 0 : decals.Length) + " bytes)");

            byte[] props = serializableDataManager.LoadData(PropKey);
            PropLineStore.Load(props);
            Debug.Log("[QuayTools] Loaded prop lines (" + (props == null ? 0 : props.Length) + " bytes)");

            byte[] locks = serializableDataManager.LoadData(LockKey);
            LockStore.Load(locks);
            PedStore.Load(serializableDataManager.LoadData(PedKey));
            HideStore.Load(serializableDataManager.LoadData(HideKey));
            Debug.Log("[QuayTools] Loaded segment locks (" + (locks == null ? 0 : locks.Length) + " bytes)");
        }

        public override void OnSaveData()
        {
            serializableDataManager.SaveData(NetLineKey, NetLineStore.Save());
            serializableDataManager.SaveData(DecalKey, DecalStore.Save());
            serializableDataManager.SaveData(PropKey, PropLineStore.Save());
            serializableDataManager.SaveData(LockKey, LockStore.Save());
            serializableDataManager.SaveData(PedKey, PedStore.Save());
            serializableDataManager.SaveData(HideKey, HideStore.Save());
        }
    }
}
