using ICities;
using UnityEngine;

namespace QuayTools
{
    /// <summary>Stores the fence settings inside the savegame.</summary>
    public class SerializableData : SerializableDataExtensionBase
    {
        private const string Key = "QuayTools.Fences";

        public override void OnLoadData()
        {
            byte[] data = serializableDataManager.LoadData(Key);
            FenceStore.Load(data);
            Debug.Log("[QuayTools] Loaded fence settings (" + (data == null ? 0 : data.Length) + " bytes)");
        }

        public override void OnSaveData()
        {
            byte[] data = FenceStore.Save();
            serializableDataManager.SaveData(Key, data);
        }
    }
}
