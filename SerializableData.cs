using ICities;
using UnityEngine;

namespace QuayTools
{
    /// <summary>Stores the fence settings and decal paths inside the savegame.</summary>
    public class SerializableData : SerializableDataExtensionBase
    {
        private const string FenceKey = "QuayTools.Fences";
        private const string DecalKey = "QuayTools.Decals";

        public override void OnLoadData()
        {
            History.Clear();

            byte[] data = serializableDataManager.LoadData(FenceKey);
            FenceStore.Load(data);
            Debug.Log("[QuayTools] Loaded fence settings (" + (data == null ? 0 : data.Length) + " bytes)");

            byte[] decals = serializableDataManager.LoadData(DecalKey);
            DecalStore.Load(decals);
            Debug.Log("[QuayTools] Loaded decal paths (" + (decals == null ? 0 : decals.Length) + " bytes)");
        }

        public override void OnSaveData()
        {
            serializableDataManager.SaveData(FenceKey, FenceStore.Save());
            serializableDataManager.SaveData(DecalKey, DecalStore.Save());
        }
    }
}
