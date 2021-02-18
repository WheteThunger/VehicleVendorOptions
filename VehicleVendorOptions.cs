using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Linq;
using static ConversationData;

namespace Oxide.Plugins
{
    [Info("Vehicle Vendor Options", "WhiteThunder", "1.3.0")]
    [Description("Allows vehicles spawned at vendors to have configurable fuel, properly assigned ownership, and to be free with permissions.")]
    internal class VehicleVendorOptions : CovalencePlugin
    {
        #region Fields

        private const string Permission_Ownership_All = "vehiclevendoroptions.ownership.allvehicles";
        private const string Permission_Ownership_MiniCopter = "vehiclevendoroptions.ownership.minicopter";
        private const string Permission_Ownership_ScrapHeli = "vehiclevendoroptions.ownership.scraptransport";
        private const string Permission_Ownership_Rowboat = "vehiclevendoroptions.ownership.rowboat";
        private const string Permission_Ownership_RHIB = "vehiclevendoroptions.ownership.rhib";
        private const string Permission_Ownership_RidableHorse = "vehiclevendoroptions.ownership.ridablehorse";

        private const string Permission_Free_All = "vehiclevendoroptions.free.allvehicles";
        private const string Permission_Free_RidableHorse = "vehiclevendoroptions.free.ridablehorse";

        private const int MinHidddenSlot = 24;
        private const int ScrapItemId = -932201673;

        private Item _scrapItem;
        private static Configuration _pluginConfig;

        private readonly FreeVehicleConfig[] _freeVehicleConfigs = new FreeVehicleConfig[]
        {
            new FreeVehicleConfig()
            {
                freePermission = "vehiclevendoroptions.free.minicopter",
                matchSpeechNode = "minicopterbuy",
                responseAction = "buyminicopter",
                successSpeechNode = "success"
            },
            new FreeVehicleConfig()
            {
                freePermission = "vehiclevendoroptions.free.scraptransport",
                matchSpeechNode = "transportbuy",
                responseAction = "buytransport",
                successSpeechNode = "success"
            },
            new FreeVehicleConfig()
            {
                freePermission = "vehiclevendoroptions.free.rowboat",
                matchSpeechNode = "pay_rowboat",
                responseAction = "buyboat",
                successSpeechNode = "buysuccess"
            },
            new FreeVehicleConfig()
            {
                freePermission = "vehiclevendoroptions.free.rhib",
                matchSpeechNode = "pay_rhib",
                responseAction = "buyrhib",
                successSpeechNode = "buysuccess"
            },
        };

        internal class FreeVehicleConfig
        {
            public string freePermission;
            public string matchSpeechNode;
            public string responseAction;
            public string successSpeechNode;
        }

        private readonly VehiclePriceConfig[] _vehiclePriceConfigs = new VehiclePriceConfig[]
        {
            new VehiclePriceConfig()
            {
                responseAction = "buyminicopter",
                GetPrice = () => _pluginConfig.Vehicles.Minicopter.ScrapCost
            },
            new VehiclePriceConfig()
            {
                responseAction = "buytransport",
                GetPrice = () => _pluginConfig.Vehicles.ScrapTransport.ScrapCost
            },
            new VehiclePriceConfig()
            {
                responseAction = "buyboat",
                GetPrice = () => _pluginConfig.Vehicles.Rowboat.ScrapCost
            },
            new VehiclePriceConfig()
            {
                responseAction = "buyrhib",
                GetPrice = () => _pluginConfig.Vehicles.RHIB.ScrapCost
            }
        };

        internal class VehiclePriceConfig
        {
            public string responseAction;
            public Func<int> GetPrice;
        }

        #endregion

        #region Hooks

        private void Init()
        {
            _pluginConfig = Config.ReadObject<Configuration>();

            permission.RegisterPermission(Permission_Ownership_All, this);
            permission.RegisterPermission(Permission_Ownership_MiniCopter, this);
            permission.RegisterPermission(Permission_Ownership_ScrapHeli, this);
            permission.RegisterPermission(Permission_Ownership_Rowboat, this);
            permission.RegisterPermission(Permission_Ownership_RHIB, this);
            permission.RegisterPermission(Permission_Ownership_RidableHorse, this);

            permission.RegisterPermission(Permission_Free_All, this);
            permission.RegisterPermission(Permission_Free_RidableHorse, this);

            foreach (var responseConfig in _freeVehicleConfigs)
                permission.RegisterPermission(responseConfig.freePermission, this);
        }

        private void OnServerInitialized()
        {
            _scrapItem = ItemManager.CreateByItemID(ScrapItemId);
        }

        private void Unload()
        {
            _scrapItem?.Remove();
        }

        private void OnEntitySpawned(MiniCopter vehicle) => HandleSpawn(vehicle);

        private void OnEntitySpawned(MotorRowboat vehicle) => HandleSpawn(vehicle);

        private object OnRidableAnimalClaim(RidableHorse horse, BasePlayer player)
        {
            if (!horse.IsForSale() || !HasPermissionAny(player.UserIDString, Permission_Free_All, Permission_Free_RidableHorse))
                return null;

            horse.SetFlag(BaseEntity.Flags.Reserved2, false);
            horse.AttemptMount(player, doMountChecks: false);
            Interface.CallHook("OnRidableAnimalClaimed", horse, player);
            return false;
        }

        private void OnRidableAnimalClaimed(RidableHorse horse, BasePlayer player) =>
            SetOwnerIfPermission(horse, player);

        private object OnNpcConversationRespond(NPCTalking npcTalking, BasePlayer player, ConversationData conversationData, ResponseNode responseNode)
        {
            if (!(npcTalking is VehicleVendor))
                return null;

            if (TryPurchaseFree(npcTalking, player, conversationData, responseNode))
                return false;

            MaybeFakePlayerScrap(npcTalking, player, conversationData, responseNode);
            MaybeAddPlayerScrap(npcTalking, player, conversationData, responseNode);

            return null;
        }

        #endregion

        #region Helper Methods

        private static void AdjustFuel(BaseVehicle vehicle, int desiredFuelAmount)
        {
            var fuelSystem = vehicle.GetFuelSystem();
            if (fuelSystem == null)
                return;

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

        private static void RefreshInventory(BasePlayer player) =>
            player.inventory.SendUpdatedInventory(PlayerInventory.Type.Main, player.inventory.containerMain);

        private static int GetNextAvailableSlot(ProtoBuf.ItemContainer containerInfo)
        {
            var highestSlot = MinHidddenSlot;
            foreach (var item in containerInfo.contents)
            {
                if (item.slot > highestSlot)
                    highestSlot = item.slot;
            }
            return highestSlot;
        }

        private static ProtoBuf.Item FindItem(List<ProtoBuf.Item> itemList, int itemId)
        {
            foreach (var item in itemList)
                if (item.itemid == itemId)
                    return item;

            return null;
        }

        private static int GetRequiredScrapAmount(ResponseNode responseNode)
        {
            foreach (var condition in responseNode.conditions)
            {
                if (condition.conditionType == ConversationCondition.ConditionType.HASSCRAP)
                    return condition.conditionAmount;
            }
            return -1;
        }

        private static SpeechNode FindSpeechNodeByName(ConversationData conversationData, string speechNodeName)
        {
            foreach (var speechNode in conversationData.speeches)
            {
                if (speechNode.shortname == speechNodeName)
                    return speechNode;
            }
            return null;
        }

        private static bool TryConversationAction(NPCTalking npcTalking, BasePlayer player, string action)
        {
            var resultAction = npcTalking.conversationResultActions.FirstOrDefault(result => result.action == action);
            if (resultAction == null)
                return false;

            // This re-implements game logic to kick out other conversing players when a vehicle spawns
            npcTalking.CleanupConversingPlayers();
            foreach (BasePlayer conversingPlayer in npcTalking.conversingPlayers)
            {
                if (conversingPlayer != player && conversingPlayer != null)
                {
                    int speechNodeIndex = npcTalking.GetConversationFor(player).GetSpeechNodeIndex("startbusy");
                    npcTalking.ForceSpeechNode(conversingPlayer, speechNodeIndex);
                }
            }

            // Spawn the vehicle
            npcTalking.lastActionPlayer = player;
            npcTalking.BroadcastEntityMessage(resultAction.broadcastMessage, resultAction.broadcastRange);
            npcTalking.lastActionPlayer = null;

            return true;
        }

        private static void AdvanceOrEndConveration(NPCTalking npcTalking, BasePlayer player, ConversationData conversationData, string targetSpeechName)
        {
            var speechNodeIndex = conversationData.GetSpeechNodeIndex(targetSpeechName);
            if (speechNodeIndex == -1)
            {
                npcTalking.ForceEndConversation(player);
            }
            else
            {
                var speechNode = conversationData.speeches[speechNodeIndex];
                npcTalking.ForceSpeechNode(player, speechNodeIndex);
            }
        }

        private void HandleSpawn(BaseVehicle vehicle)
        {
            if (Rust.Application.isLoadingSave)
                return;

            NextTick(() =>
            {
                if (vehicle.creatorEntity == null)
                    return;

                var vehicleConfig = GetVehicleConfig(vehicle);
                if (vehicleConfig == null)
                    return;

                AdjustFuel(vehicle, vehicleConfig.FuelAmount);
                MaybeSetOwner(vehicle);
            });
        }

        private void MaybeSetOwner(BaseVehicle vehicle)
        {
            var basePlayer = vehicle.creatorEntity as BasePlayer;
            if (basePlayer == null)
                return;

            SetOwnerIfPermission(vehicle, basePlayer);
        }

        private void SetOwnerIfPermission(BaseVehicle vehicle, BasePlayer basePlayer)
        {
            if (HasPermissionAny(basePlayer.IPlayer, Permission_Ownership_All, GetOwnershipPermission(vehicle)))
                vehicle.OwnerID = basePlayer.userID;
        }

        private bool HasPermissionAny(IPlayer player, params string[] permissionNames) =>
            HasPermissionAny(player.Id, permissionNames);

        private bool HasPermissionAny(string userIdString, params string[] permissionNames)
        {
            foreach (var perm in permissionNames)
                if (perm != null && permission.UserHasPermission(userIdString, perm))
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

            if (vehicle is RidableHorse)
                return Permission_Ownership_RidableHorse;

            return null;
        }

        private void SendUpdatedInventoryWithFakeScrap(BasePlayer player, int amountDiff)
        {
            using (var containerUpdate = Facepunch.Pool.Get<ProtoBuf.UpdateItemContainer>())
            {
                containerUpdate.type = (int)PlayerInventory.Type.Main;
                containerUpdate.container = Facepunch.Pool.Get<List<ProtoBuf.ItemContainer>>();

                var containerInfo = player.inventory.containerMain.Save();
                var itemSlot = AddFakeScrapToContainerUpdate(containerInfo, amountDiff);

                containerUpdate.container.Capacity = itemSlot + 1;
                containerUpdate.container.Add(containerInfo);
                player.ClientRPCPlayer(null, player, "UpdatedItemContainer", containerUpdate);
            }
        }

        private int AddFakeScrapToContainerUpdate(ProtoBuf.ItemContainer containerInfo, int scrapAmount)
        {
            // Always use a separate item so it can be placed out of view.
            var itemInfo = _scrapItem.Save();
            itemInfo.amount = scrapAmount;
            itemInfo.slot = GetNextAvailableSlot(containerInfo);
            containerInfo.contents.Add(itemInfo);
            return itemInfo.slot;
        }

        private bool TryPurchaseFree(NPCTalking npcTalking, BasePlayer player, ConversationData conversationData, ResponseNode responseNode)
        {
            foreach (var responseConfig in _freeVehicleConfigs)
            {
                if (responseNode.resultingSpeechNode != responseConfig.matchSpeechNode)
                    continue;

                if (!HasPermissionAny(player.UserIDString, Permission_Free_All, responseConfig.freePermission))
                    return false;

                if (!TryConversationAction(npcTalking, player, responseConfig.responseAction))
                    return false;

                AdvanceOrEndConveration(npcTalking, player, conversationData, responseConfig.successSpeechNode);
                return true;
            }
            return false;
        }

        private void MaybeAddPlayerScrap(NPCTalking npcTalking, BasePlayer player, ConversationData conversationData, ResponseNode responseNode)
        {
            foreach (var priceConfig in _vehiclePriceConfigs)
            {
                if (priceConfig.responseAction != responseNode.actionString)
                    continue;

                var vanillaPrice = GetRequiredScrapAmount(responseNode);
                if (vanillaPrice == -1)
                {
                    LogError($"Something went wrong. The '{responseNode.actionString}' reponse node does not require scrap. The price was unable to be adjusted. Please contact the plugin maintainer.");
                    return;
                }

                var customPrice = priceConfig.GetPrice();
                if (customPrice < 0 || customPrice == vanillaPrice)
                {
                    // Use vanilla price, so nothing to do.
                    return;
                }

                var playerAmount = player.inventory.GetAmount(ScrapItemId);
                if (playerAmount < customPrice)
                    return;

                var extraScrap = vanillaPrice - customPrice;

                player.inventory.containerMain.AddItem(ItemManager.itemDictionary[ScrapItemId], extraScrap);

                // Check conditions just in case, to make sure we don't give free scrap.
                if (!responseNode.PassesConditions(player, npcTalking))
                    player.inventory.containerMain.AddItem(ItemManager.itemDictionary[ScrapItemId], -extraScrap);

                return;
            }
        }

        private void MaybeFakePlayerScrap(NPCTalking npcTalking, BasePlayer player, ConversationData conversationData, ResponseNode responseNode)
        {
            var speechNode = FindSpeechNodeByName(conversationData, responseNode.resultingSpeechNode);
            if (speechNode == null)
                return;

            foreach (var response in speechNode.responses)
            {
                foreach (var priceConfig in _vehiclePriceConfigs)
                {
                    if (priceConfig.responseAction != response.actionString)
                        continue;

                    var vanillaPrice = GetRequiredScrapAmount(response);
                    if (vanillaPrice == -1)
                        continue;

                    var customPrice = priceConfig.GetPrice();
                    if (customPrice < 0 || customPrice == vanillaPrice)
                    {
                        // Use vanilla price, so nothing to do.
                        return;
                    }

                    var playerAmount = player.inventory.GetAmount(ScrapItemId);
                    var playerHasEnough = playerAmount >= customPrice;
                    var willDisplayOption = playerAmount >= vanillaPrice;

                    if (willDisplayOption == playerHasEnough)
                        return;

                    SendUpdatedInventoryWithFakeScrap(player, vanillaPrice - customPrice);

                    // This delay needs to be long enough for the text to print out, which could vary by language.
                    timer.Once(2f, () =>
                    {
                        if (player != null)
                            RefreshInventory(player);
                    });
                    return;
                }
            }
        }

        #endregion

        #region Configuration

        private VehicleConfig GetVehicleConfig(BaseVehicle vehicle)
        {
            // Must go before MiniCopter
            if (vehicle is ScrapTransportHelicopter)
                return _pluginConfig.Vehicles.ScrapTransport;

            if (vehicle is MiniCopter)
                return _pluginConfig.Vehicles.Minicopter;

            // Must go before MotorRowboat
            if (vehicle is RHIB)
                return _pluginConfig.Vehicles.RHIB;

            if (vehicle is MotorRowboat)
                return _pluginConfig.Vehicles.Rowboat;

            return null;
        }

        internal class Configuration : SerializableConfiguration
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

            [JsonProperty("ScrapTransport")]
            public VehicleConfig ScrapTransport = new VehicleConfig()
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
        }

        internal class VehicleConfig
        {
            [JsonProperty("FuelAmount")]
            public int FuelAmount = 100;

            [JsonProperty("ScrapCost")]
            public int ScrapCost = -1;
        }

        private Configuration GetDefaultConfig() => new Configuration();

        #endregion

        #region Configuration Boilerplate

        internal class SerializableConfiguration
        {
            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonHelper.Deserialize(ToJson()) as Dictionary<string, object>;
        }

        internal static class JsonHelper
        {
            public static object Deserialize(string json) => ToObject(JToken.Parse(json));

            private static object ToObject(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        return token.Children<JProperty>()
                                    .ToDictionary(prop => prop.Name,
                                                  prop => ToObject(prop.Value));

                    case JTokenType.Array:
                        return token.Select(ToObject).ToList();

                    default:
                        return ((JValue)token).Value;
                }
            }
        }

        private bool MaybeUpdateConfig(SerializableConfiguration config)
        {
            var currentWithDefaults = config.ToDictionary();
            var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);
            return MaybeUpdateConfigDict(currentWithDefaults, currentRaw);
        }

        private bool MaybeUpdateConfigDict(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            bool changed = false;

            foreach (var key in currentWithDefaults.Keys)
            {
                object currentRawValue;
                if (currentRaw.TryGetValue(key, out currentRawValue))
                {
                    var defaultDictValue = currentWithDefaults[key] as Dictionary<string, object>;
                    var currentDictValue = currentRawValue as Dictionary<string, object>;

                    if (defaultDictValue != null)
                    {
                        if (currentDictValue == null)
                        {
                            currentRaw[key] = currentWithDefaults[key];
                            changed = true;
                        }
                        else if (MaybeUpdateConfigDict(defaultDictValue, currentDictValue))
                            changed = true;
                    }
                }
                else
                {
                    currentRaw[key] = currentWithDefaults[key];
                    changed = true;
                }
            }

            return changed;
        }

        protected override void LoadDefaultConfig() => _pluginConfig = GetDefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _pluginConfig = Config.ReadObject<Configuration>();
                if (_pluginConfig == null)
                {
                    throw new JsonException();
                }

                if (MaybeUpdateConfig(_pluginConfig))
                {
                    LogWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                LogWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Log($"Configuration changes saved to {Name}.json");
            Config.WriteObject(_pluginConfig, true);
        }

        #endregion
    }
}
