using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Vehicle Vendor Options", "WhiteThunder", "1.0.0")]
    [Description("Allows adjusting fuel and ownership of vehicles spawned at vendors.")]
    internal class VehicleVendorOptions : CovalencePlugin
    {
        #region Fields

        private VendorOptionsConfig PluginConfig;

        private const string Permission_Ownership_All = "vehiclevendoroptions.ownership.allvehicles";
        private const string Permission_Ownership_MiniCopter = "vehiclevendoroptions.ownership.minicopter";
        private const string Permission_Ownership_ScrapHeli = "vehiclevendoroptions.ownership.scraptransport";
        private const string Permission_Ownership_Rowboat = "vehiclevendoroptions.ownership.rowboat";
        private const string Permission_Ownership_RHIB = "vehiclevendoroptions.ownership.rhib";

        #endregion

        #region Hooks

        private void Init()
        {
            PluginConfig = Config.ReadObject<VendorOptionsConfig>();

            permission.RegisterPermission(Permission_Ownership_All, this);
            permission.RegisterPermission(Permission_Ownership_MiniCopter, this);
            permission.RegisterPermission(Permission_Ownership_ScrapHeli, this);
            permission.RegisterPermission(Permission_Ownership_Rowboat, this);
            permission.RegisterPermission(Permission_Ownership_RHIB, this);
        }

        private void OnEntitySpawned(MiniCopter vehicle) => HandleSpawn(vehicle);

        private void OnEntitySpawned(MotorRowboat vehicle) => HandleSpawn(vehicle);

        #endregion

        #region Helper Methods

        private void HandleSpawn(BaseVehicle vehicle)
        {
            if (Rust.Application.isLoadingSave) return;

            NextTick(() =>
            {
                if (vehicle.creatorEntity == null) return;

                var vehicleConfig = GetVehicleConfig(vehicle);
                if (vehicleConfig == null) return;

                AdjustFuel(vehicle, vehicleConfig.FuelAmount);
                MaybeSetOwner(vehicle);
            });
        }

        private VehicleConfig GetVehicleConfig(BaseVehicle vehicle)
        {
            // Must go before MiniCopter
            if (vehicle is ScrapTransportHelicopter)
                return PluginConfig.Vehicles.ScrapTransport;

            if (vehicle is MiniCopter)
                return PluginConfig.Vehicles.Minicopter;

            // Must go before MotorRowboat
            if (vehicle is RHIB)
                return PluginConfig.Vehicles.RHIB;

            if (vehicle is MotorRowboat)
                return PluginConfig.Vehicles.Rowboat;

            return null;
        }

        private void AdjustFuel(BaseVehicle vehicle, int desiredFuelAmount)
        {
            var fuelSystem = vehicle.GetFuelSystem();
            if (fuelSystem == null) return;

            var fuelAmount = desiredFuelAmount < 0
                ? fuelSystem.GetFuelContainer().allowedItem.stackable
                : desiredFuelAmount;

            var fuelItem = fuelSystem.GetFuelItem();
            if (fuelItem != null && fuelItem.amount != fuelAmount)
            {
                fuelItem.amount = fuelAmount;
                fuelItem.MarkDirty();
            }
        }

        private void MaybeSetOwner(BaseVehicle vehicle)
        {
            var basePlayer = vehicle.creatorEntity as BasePlayer;
            if (basePlayer == null) return;

            if (HasPermissionAny(basePlayer.IPlayer, Permission_Ownership_All, GetOwnershipPermission(vehicle)))
                vehicle.OwnerID = basePlayer.userID;
        }

        private bool HasPermissionAny(IPlayer player, params string[] permissionNames)
        {
            foreach (var perm in permissionNames)
                if (perm != null && permission.UserHasPermission(player.Id, perm))
                    return true;

            return false;
        }

        private string GetOwnershipPermission(BaseVehicle vehicle)
        {
            // Must go before MiniCopter
            if (vehicle is ScrapTransportHelicopter)
                return Permission_Ownership_ScrapHeli;

            if (vehicle is MiniCopter)
                return Permission_Ownership_MiniCopter;

            // Must go before MotorRowboat
            if (vehicle is RHIB)
                return Permission_Ownership_RHIB;

            if (vehicle is MotorRowboat)
                return Permission_Ownership_Rowboat;

            return null;
        }

        #endregion

        #region Configuration

        protected override void LoadDefaultConfig() => Config.WriteObject(new VendorOptionsConfig(), true);

        internal class VendorOptionsConfig
        {
            [JsonProperty("Vehicles")]
            public VehicleConfigMap Vehicles = new VehicleConfigMap();
        }

        internal class VehicleConfigMap
        {
            [JsonProperty("Minicopter")]
            public VehicleConfig Minicopter = new VehicleConfig()
            {
                FuelAmount = 100
            };

            [JsonProperty("Rowboat")]
            public VehicleConfig Rowboat = new VehicleConfig()
            {
                FuelAmount = 50
            };

            [JsonProperty("RHIB")]
            public VehicleConfig RHIB = new VehicleConfig()
            {
                FuelAmount = 50
            };

            [JsonProperty("ScrapTransport")]
            public VehicleConfig ScrapTransport = new VehicleConfig()
            {
                FuelAmount = 100
            };
        }

        internal class VehicleConfig
        {
            [JsonProperty("FuelAmount")]
            public int FuelAmount = 100;
        }

        #endregion
    }
}
