using UnityEngine;

namespace DishevelledBadger.FlashFrostVale.SharedData
{
    /// <summary>
    /// Represents the synchronized data for a single hexagonal tile on the map.
    /// This struct is used in a SyncList to replicate map state to all clients,
    /// and also for sending map data in chunks via RPC.
    /// </summary>
    public struct HexTileData
    {
        public Vector2Int coordinates;
        public int typeId; // Maps to visualHexTypePrefabs index on client
        public float height;
        public int occupantBuildingTypeId; // -1 if no building
        public uint occupantOwnerNetId;    // NetworkId of the NetworkGamePlayerFFV owner

        public HexTileData(Vector2Int coordinates, int typeId, float height, int occupantBuildingTypeId = -1, uint occupantOwnerNetId = 0)
        {
            this.coordinates = coordinates;
            this.typeId = typeId;
            this.height = height;
            this.occupantBuildingTypeId = occupantBuildingTypeId;
            this.occupantOwnerNetId = occupantOwnerNetId;
        }

        /// <summary>
        /// Helper property to quickly check if a tile is considered occupied by a building.
        /// </summary>
        public bool IsOccupied()
        {
            return occupantBuildingTypeId != -1;
        }
    }
}
