using ICities;
using UnityEngine;

namespace QuayTools
{
    /// <summary>Stores the fence settings and decal paths inside the savegame.</summary>
    public class SerializableData : SerializableDataExtensionBase
    {
        private const string FenceKey = "QuayTools.Fences";
        private const string DecalKey = "QuayTools.Decals";
        private const string PropKey = "QuayTools.PropLines";
        private const string LockKey = "QuayTools.Locks";
        private const string PedKey = "QuayTools.NoPeds";

        public override void OnLoadData()
        {
            History.Clear();

            byte[] data = serializableDataManager.LoadData(FenceKey);
            FenceStore.Load(data);
            Debug.Log("[QuayTools] Loaded fence settings (" + (data == null ? 0 : data.Length) + " bytes)");

            byte[] decals = serializableDataManager.LoadData(DecalKey);
            DecalStore.Load(decals);
            Debug.Log("[QuayTools] Loaded decal paths (" + (decals == null ? 0 : decals.Length) + " bytes)");

            byte[] props = serializableDataManager.LoadData(PropKey);
            PropLineStore.Load(props);
            Debug.Log("[QuayTools] Loaded prop lines (" + (props == null ? 0 : props.Length) + " bytes)");

            byte[] locks = serializableDataManager.LoadData(LockKey);
            LockStore.Load(locks);
            PedStore.Load(serializableDataManager.LoadData(PedKey));
            Debug.Log("[QuayTools] Loaded segment locks (" + (locks == null ? 0 : locks.Length) + " bytes)");
        }

        public override void OnSaveData()
        {
            serializableDataManager.SaveData(FenceKey, FenceStore.Save());
            serializableDataManager.SaveData(DecalKey, DecalStore.Save());
            serializableDataManager.SaveData(PropKey, PropLineStore.Save());
            serializableDataManager.SaveData(LockKey, LockStore.Save());
            serializableDataManager.SaveData(PedKey, PedStore.Save());
        }
    }
}
