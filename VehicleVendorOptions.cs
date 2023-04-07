using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using static ConversationData;

namespace Oxide.Plugins
{
    [Info("Vehicle Vendor Options", "WhiteThunder", "1.6.0")]
    [Description("Allows customizing vehicle fuel and prices at NPC vendors.")]
    internal class VehicleVendorOptions : CovalencePlugin
    {
        #region Fields

        [PluginReference]
        private readonly Plugin Economics, ServerRewards;

        private Configuration _config;

        private const string ShortName_ScrapHeli = "scraptransport";
        private const string ShortName_Minicopter = "minicopter";
        private const string ShortName_RHIB = "rhib";
        private const string ShortName_Rowboat = "rowboat";
        private const string ShortName_DuoSub = "duosub";
        private const string ShortName_SoloSub = "solosub";

        private const string Permission_Allow_All = "vehiclevendoroptions.allow.all";
        private const string Permission_Allow_ScrapHeli = "vehiclevendoroptions.allow.scraptransport";
        private const string Permission_Allow_MiniCopter = "vehiclevendoroptions.allow.minicopter";
        private const string Permission_Allow_RHIB = "vehiclevendoroptions.allow.rhib";
        private const string Permission_Allow_Rowboat = "vehiclevendoroptions.allow.rowboat";
        private const string Permission_Allow_SoloSub = "vehiclevendoroptions.allow.solosub";
        private const string Permission_Allow_DuoSub = "vehiclevendoroptions.allow.duosub";

        private const string Permission_Ownership_All = "vehiclevendoroptions.ownership.allvehicles";
        private const string Permission_Ownership_ScrapHeli = "vehiclevendoroptions.ownership.scraptransport";
        private const string Permission_Ownership_MiniCopter = "vehiclevendoroptions.ownership.minicopter";
        private const string Permission_Ownership_RHIB = "vehiclevendoroptions.ownership.rhib";
        private const string Permission_Ownership_Rowboat = "vehiclevendoroptions.ownership.rowboat";
        private const string Permission_Ownership_SoloSub = "vehiclevendoroptions.ownership.solosub";
        private const string Permission_Ownership_DuoSub = "vehiclevendoroptions.ownership.duosub";
        private const string Permission_Ownership_RidableHorse = "vehiclevendoroptions.ownership.ridablehorse";

        private const string Permission_Free_All = "vehiclevendoroptions.free.allvehicles";
        private const string Permission_Free_ScrapHeli = "vehiclevendoroptions.free.scraptransport";
        private const string Permission_Free_Minicopter = "vehiclevendoroptions.free.minicopter";
        private const string Permission_Free_RHIB = "vehiclevendoroptions.free.rhib";
        private const string Permission_Free_Rowboat = "vehiclevendoroptions.free.rowboat";
        private const string Permission_Free_SoloSub = "vehiclevendoroptions.free.solosub";
        private const string Permission_Free_DuoSub = "vehiclevendoroptions.free.duosub";
        private const string Permission_Free_RidableHorse = "vehiclevendoroptions.free.ridablehorse";

        private const string Permission_Price_Prefix = "vehiclevendoroptions.price";

        private const int MinHiddenSlot = 24;
        private const int ScrapItemId = -932201673;
        private const float VanillaDespawnProtectionTime = 300;

        private readonly object False = false;

        private Item _scrapItem;

        private List<BaseResponseInterceptor> _responseInterceptors;

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(Permission_Allow_All, this);
            permission.RegisterPermission(Permission_Allow_ScrapHeli, this);
            permission.RegisterPermission(Permission_Allow_MiniCopter, this);
            permission.RegisterPermission(Permission_Allow_RHIB, this);
            permission.RegisterPermission(Permission_Allow_Rowboat, this);
            permission.RegisterPermission(Permission_Allow_DuoSub, this);
            permission.RegisterPermission(Permission_Allow_SoloSub, this);

            permission.RegisterPermission(Permission_Ownership_All, this);
            permission.RegisterPermission(Permission_Ownership_ScrapHeli, this);
            permission.RegisterPermission(Permission_Ownership_MiniCopter, this);
            permission.RegisterPermission(Permission_Ownership_RHIB, this);
            permission.RegisterPermission(Permission_Ownership_Rowboat, this);
            permission.RegisterPermission(Permission_Ownership_DuoSub, this);
            permission.RegisterPermission(Permission_Ownership_SoloSub, this);
            permission.RegisterPermission(Permission_Ownership_RidableHorse, this);

            permission.RegisterPermission(Permission_Free_All, this);
            permission.RegisterPermission(Permission_Free_ScrapHeli, this);
            permission.RegisterPermission(Permission_Free_Minicopter, this);
            permission.RegisterPermission(Permission_Free_RHIB, this);
            permission.RegisterPermission(Permission_Free_Rowboat, this);
            permission.RegisterPermission(Permission_Free_DuoSub, this);
            permission.RegisterPermission(Permission_Free_SoloSub, this);
            permission.RegisterPermission(Permission_Free_RidableHorse, this);

            _config.Vehicles.RegisterCustomPricePermissions(this);

            _responseInterceptors = new List<BaseResponseInterceptor>
            {
                new PermissionInterceptor(ConversationUtils.ResponseActions.BuyScrapHeli, () => _config.Vehicles.ScrapTransport, Permission_Allow_ScrapHeli),
                new PermissionInterceptor(ConversationUtils.ResponseActions.BuyMinicopter, () => _config.Vehicles.Minicopter, Permission_Allow_MiniCopter),
                new PermissionInterceptor(ConversationUtils.ResponseActions.BuyRHIB, () => _config.Vehicles.RHIB, Permission_Allow_RHIB),
                new PermissionInterceptor(ConversationUtils.ResponseActions.BuyRowboat, () => _config.Vehicles.Rowboat, Permission_Allow_Rowboat),
                new PermissionInterceptor(ConversationUtils.ResponseActions.BuyDuoSub, () => _config.Vehicles.DuoSub, Permission_Allow_DuoSub),
                new PermissionInterceptor(ConversationUtils.ResponseActions.BuySoloSub, () => _config.Vehicles.SoloSub, Permission_Allow_SoloSub),

                new PaymentInterceptor(ConversationUtils.ResponseActions.BuyScrapHeli, () => _config.Vehicles.ScrapTransport, Permission_Free_ScrapHeli),
                new PaymentInterceptor(ConversationUtils.ResponseActions.BuyMinicopter, () => _config.Vehicles.Minicopter, Permission_Free_Minicopter),
                new PaymentInterceptor(ConversationUtils.ResponseActions.BuyRHIB, () => _config.Vehicles.RHIB, Permission_Free_RHIB),
                new PaymentInterceptor(ConversationUtils.ResponseActions.BuyRowboat, () => _config.Vehicles.Rowboat, Permission_Free_Rowboat),
                new PaymentInterceptor(ConversationUtils.ResponseActions.BuyDuoSub, () => _config.Vehicles.DuoSub, Permission_Free_DuoSub),
                new PaymentInterceptor(ConversationUtils.ResponseActions.BuySoloSub, () => _config.Vehicles.SoloSub, Permission_Free_SoloSub),

                new PayPromptInterceptor(ConversationUtils.ResponseActions.BuyScrapHeli, () => _config.Vehicles.ScrapTransport, Permission_Free_ScrapHeli),
                new PayPromptInterceptor(ConversationUtils.ResponseActions.BuyMinicopter, () => _config.Vehicles.Minicopter, Permission_Free_Minicopter),
                new PayPromptInterceptor(ConversationUtils.ResponseActions.BuyRHIB, () => _config.Vehicles.RHIB, Permission_Free_RHIB),
                new PayPromptInterceptor(ConversationUtils.ResponseActions.BuyRowboat, () => _config.Vehicles.Rowboat, Permission_Free_Rowboat),
                new PayPromptInterceptor(ConversationUtils.ResponseActions.BuyDuoSub, () => _config.Vehicles.DuoSub, Permission_Free_DuoSub),
                new PayPromptInterceptor(ConversationUtils.ResponseActions.BuySoloSub, () => _config.Vehicles.SoloSub, Permission_Free_SoloSub),
            };
        }

        private void OnServerInitialized()
        {
            _scrapItem = ItemManager.CreateByItemID(ScrapItemId);
        }

        private void Unload()
        {
            CostLabelUI.DestroyAll();
            _scrapItem?.Remove();
        }

        private void OnEntitySpawned(MiniCopter vehicle) => HandleSpawn(vehicle);

        private void OnEntitySpawned(MotorRowboat vehicle) => HandleSpawn(vehicle);

        private void OnEntitySpawned(BaseSubmarine vehicle) => HandleSpawn(vehicle);

        private object OnRidableAnimalClaim(RidableHorse horse, BasePlayer player)
        {
            if (!horse.IsForSale() || !HasPermissionAny(player.UserIDString, Permission_Free_All, Permission_Free_RidableHorse))
                return null;

            horse.SetFlag(BaseEntity.Flags.Reserved2, false);
            horse.AttemptMount(player, doMountChecks: false);
            Interface.CallHook("OnRidableAnimalClaimed", horse, player);
            return False;
        }

        private void OnRidableAnimalClaimed(RidableHorse horse, BasePlayer player) =>
            SetOwnerIfPermission(horse, player);

        private object OnNpcConversationRespond(VehicleVendor vendor, BasePlayer player, ConversationData conversationData, ResponseNode responseNode)
        {
            CostLabelUI.Destroy(player);

            foreach (var interceptor in _responseInterceptors)
            {
                var forceNextSpeechNode = interceptor.Intercept(this, vendor, player, conversationData, responseNode);
                if (forceNextSpeechNode != string.Empty)
                {
                    ConversationUtils.ForceSpeechNode(vendor, player, forceNextSpeechNode);
                    return False;
                }
            }

            return null;
        }

        private void OnNpcConversationEnded(VehicleVendor vendor, BasePlayer player)
        {
            CostLabelUI.Destroy(player);
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

                vehicle.spawnTime += vehicleConfig.DespawnProtectionSeconds - VanillaDespawnProtectionTime;
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
            {
                vehicle.OwnerID = basePlayer.userID;
            }
        }

        private bool HasPermissionAny(string userIdString, params string[] permissionNames)
        {
            foreach (var perm in permissionNames)
            {
                if (perm != null && permission.UserHasPermission(userIdString, perm))
                    return true;
            }

            return false;
        }

        private bool HasPermissionAny(IPlayer player, params string[] permissionNames)
        {
            return HasPermissionAny(player.Id, permissionNames);
        }

        private string GetOwnershipPermission(BaseVehicle vehicle)
        {
            // Must go before MiniCopter.
            if (vehicle is ScrapTransportHelicopter)
                return Permission_Ownership_ScrapHeli;

            if (vehicle is MiniCopter)
                return Permission_Ownership_MiniCopter;

            // Must go before MotorRowboat.
            if (vehicle is RHIB)
                return Permission_Ownership_RHIB;

            if (vehicle is MotorRowboat)
                return Permission_Ownership_Rowboat;

            if (vehicle is RidableHorse)
                return Permission_Ownership_RidableHorse;

            // Must go before BaseSubmarine.
            if (vehicle is SubmarineDuo)
                return Permission_Ownership_DuoSub;

            if (vehicle is BaseSubmarine)
                return Permission_Ownership_SoloSub;

            return null;
        }

        #endregion

        #region Player Inventory Utilities

        private static class PlayerInventoryUtils
        {
            public static int GetPlayerNeededScrap(BasePlayer player, int amountRequired)
            {
                return amountRequired - player.inventory.GetAmount(ScrapItemId);
            }

            public static void Refresh(BasePlayer player)
            {
                player.inventory.SendUpdatedInventory(PlayerInventory.Type.Main, player.inventory.containerMain);
            }

            public static void UpdateWithFakeScrap(BasePlayer player, Item scrapItem, int amountDiff)
            {
                using (var containerUpdate = Facepunch.Pool.Get<ProtoBuf.UpdateItemContainer>())
                {
                    containerUpdate.type = (int)PlayerInventory.Type.Main;
                    containerUpdate.container = Facepunch.Pool.Get<List<ProtoBuf.ItemContainer>>();

                    var containerInfo = player.inventory.containerMain.Save();
                    var itemSlot = AddFakeScrapToContainerUpdate(containerInfo, scrapItem, amountDiff);

                    containerUpdate.container.Capacity = itemSlot + 1;
                    containerUpdate.container.Add(containerInfo);
                    player.ClientRPCPlayer(null, player, "UpdatedItemContainer", containerUpdate);
                }
            }

            private static int AddFakeScrapToContainerUpdate(ProtoBuf.ItemContainer containerInfo, Item scrapItem, int scrapAmount)
            {
                // Always use a separate item so it can be placed out of view.
                var itemInfo = scrapItem.Save();
                itemInfo.amount = scrapAmount;
                itemInfo.slot = GetNextAvailableSlot(containerInfo);
                containerInfo.contents.Add(itemInfo);
                return itemInfo.slot;
            }

            private static int GetNextAvailableSlot(ProtoBuf.ItemContainer containerInfo)
            {
                var highestSlot = MinHiddenSlot;
                foreach (var item in containerInfo.contents)
                {
                    if (item.slot > highestSlot)
                        highestSlot = item.slot;
                }

                return highestSlot;
            }
        }

        #endregion

        #region Conversation Utilities

        private static class ConversationUtils
        {
            public static class SpeechNodes
            {
                public const string Goodbye = "goodbye";
            }

            public static class ResponseActions
            {
                public const string BuyScrapHeli = "buytransport";
                public const string BuyMinicopter = "buyminicopter";
                public const string BuyRHIB = "buyrhib";
                public const string BuyRowboat = "buyboat";
                public const string BuyDuoSub = "buysubduo";
                public const string BuySoloSub = "buysub";
            }

            public static bool ResponseHasScrapPrice(ResponseNode responseNode, out int price)
            {
                foreach (var condition in responseNode.conditions)
                {
                    if (condition.conditionType == ConversationCondition.ConditionType.HASSCRAP)
                    {
                        price = Convert.ToInt32(condition.conditionAmount);
                        return true;
                    }
                }

                price = 0;
                return false;
            }

            public static bool PrecedesPaymentOption(ConversationData conversationData, ResponseNode responseNode, string matchResponseAction, out int scrapPrice)
            {
                var resultingSpeechNode = FindSpeechNodeByName(conversationData, responseNode.resultingSpeechNode);
                if (resultingSpeechNode == null)
                {
                    scrapPrice = 0;
                    return false;
                }

                foreach (var futureResponseOption in resultingSpeechNode.responses)
                {
                    if (!string.IsNullOrEmpty(matchResponseAction)
                        && matchResponseAction == futureResponseOption.actionString
                        && ResponseHasScrapPrice(futureResponseOption, out scrapPrice))
                        return true;
                }

                scrapPrice = 0;
                return false;
            }

            public static void ForceSpeechNode(NPCTalking npcTalking, BasePlayer player, string speechNodeName)
            {
                var speechNodeIndex = npcTalking.GetConversationFor(player).GetSpeechNodeIndex(speechNodeName);
                npcTalking.ForceSpeechNode(player, speechNodeIndex);
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
        }

        #endregion

        #region UI

        private static class CostLabelUI
        {
            private const string Name = "VehicleVendorOptions";

            public static void Destroy(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, Name);
            }

            public static void DestroyAll()
            {
                foreach (var player in BasePlayer.activePlayerList)
                    Destroy(player);
            }

            public static void Create(VehicleVendorOptions plugin, BasePlayer player, PriceConfig priceConfig)
            {
                var itemPrice = priceConfig.Amount == 0
                    ? plugin.GetMessage(player, "UI.Price.Free")
                    : priceConfig.PaymentProvider is EconomicsPaymentProvider
                    ? plugin.GetMessage(player, "UI.Currency.Economics", priceConfig.Amount)
                    : priceConfig.PaymentProvider is ServerRewardsPaymentProvider
                    ? plugin.GetMessage(player, "UI.Currency.ServerRewards", priceConfig.Amount)
                    : $"{priceConfig.Amount} {plugin.GetMessage(player, plugin.GetItemNameLocalizationKey(priceConfig.ItemShortName))}";

                var cuiElements = new CuiElementContainer
                {
                    {
                        new CuiLabel
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.5 0.5",
                                AnchorMax = "0.5 0.5",
                                OffsetMin = "152 -10",
                                OffsetMax = "428 10"
                            },
                            Text =
                            {
                                Text = plugin.GetMessage(player, "UI.ActualPrice", itemPrice),
                                FontSize = 11,
                                Font = "robotocondensed-regular.ttf",
                                Align = UnityEngine.TextAnchor.MiddleLeft
                            }
                        },
                        "Overlay",
                        Name
                    }
                };

                CuiHelper.AddUi(player, cuiElements);
            }
        }

        #endregion

        #region Response Interceptors

        private abstract class BaseResponseInterceptor
        {
            protected Func<VehicleConfig> _getVehicleConfig;
            protected string _freePermission;

            protected BaseResponseInterceptor(Func<VehicleConfig> getVehicleConfig, string freePermission)
            {
                _getVehicleConfig = getVehicleConfig;
                _freePermission = freePermission;
            }

            public abstract string Intercept(VehicleVendorOptions plugin, NPCTalking npcTalking, BasePlayer player, ConversationData conversationData, ResponseNode responseNode);
        }

        private class PayPromptInterceptor : BaseResponseInterceptor
        {
            private string _matchResponseAction;

            public PayPromptInterceptor(string matchResponseAction, Func<VehicleConfig> getVehicleConfig, string freePermission) : base(getVehicleConfig, freePermission)
            {
                _matchResponseAction = matchResponseAction;
            }

            public override string Intercept(VehicleVendorOptions plugin, NPCTalking npcTalking, BasePlayer player, ConversationData conversationData, ResponseNode responseNode)
            {
                int vanillaPrice;
                if (!ConversationUtils.PrecedesPaymentOption(conversationData, responseNode, _matchResponseAction, out vanillaPrice))
                    return string.Empty;

                var priceConfig = _getVehicleConfig().GetPriceForPlayer(plugin, player.IPlayer, _freePermission);
                if (priceConfig == null || priceConfig.MatchesVanillaPrice(vanillaPrice))
                    return string.Empty;

                var neededScrap = PlayerInventoryUtils.GetPlayerNeededScrap(player, vanillaPrice);
                var canAffordVanillaPrice = neededScrap <= 0;
                var canAffordCustomPrice = priceConfig.CanPlayerAfford(player);

                CostLabelUI.Create(plugin, player, priceConfig);

                if (canAffordCustomPrice == canAffordVanillaPrice)
                    return string.Empty;

                if (!canAffordCustomPrice)
                {
                    // Reduce scrap that will be removed to just below the amount they need for vanilla.
                    neededScrap--;
                }

                // Add or remove scrap so the vanilla logic for showing the payment option will match the custom payment logic.
                PlayerInventoryUtils.UpdateWithFakeScrap(player, plugin._scrapItem, neededScrap);

                // This delay needs to be long enough for the text to print out, which could vary by language.
                player.Invoke(() => PlayerInventoryUtils.Refresh(player), 3f);

                return string.Empty;
            }
        }

        private class PaymentInterceptor : BaseResponseInterceptor
        {
            private string _matchResponseAction;

            public PaymentInterceptor(string matchResponseAction, Func<VehicleConfig> getVehicleConfig, string freePermission) : base(getVehicleConfig, freePermission)
            {
                _matchResponseAction = matchResponseAction;
            }

            public override string Intercept(VehicleVendorOptions plugin, NPCTalking npcTalking, BasePlayer player, ConversationData conversationData, ResponseNode responseNode)
            {
                if (responseNode.actionString != _matchResponseAction)
                    return string.Empty;

                int vanillaPrice;
                if (!ConversationUtils.ResponseHasScrapPrice(responseNode, out vanillaPrice))
                    return string.Empty;

                var priceConfig = _getVehicleConfig().GetPriceForPlayer(plugin, player.IPlayer, _freePermission);
                if (priceConfig == null || priceConfig.MatchesVanillaPrice(vanillaPrice))
                    return string.Empty;

                if (!priceConfig.CanPlayerAfford(player))
                    return ConversationUtils.SpeechNodes.Goodbye;

                // Add scrap so the vanilla checks will pass. Add full amount for simplicity.
                player.inventory.containerMain.AddItem(ItemManager.itemDictionary[ScrapItemId], vanillaPrice);

                // Check conditions just in case, to make sure we don't give free scrap.
                if (!responseNode.PassesConditions(player, npcTalking))
                {
                    plugin.LogError($"Price adjustment unexpectedly failed for price config (response: '{_matchResponseAction}', player: {player.userID}).");
                    player.inventory.containerMain.AddItem(ItemManager.itemDictionary[ScrapItemId], -vanillaPrice);
                    return string.Empty;
                }

                priceConfig.TryChargePlayer(player);

                return string.Empty;
            }
        }

        private class PermissionInterceptor : BaseResponseInterceptor
        {
            private string _matchResponseAction;
            private string _allowPermission;

            public PermissionInterceptor(string matchResponseAction, Func<VehicleConfig> getVehicleConfig, string allowPermission) : base(getVehicleConfig, null)
            {
                _matchResponseAction = matchResponseAction;
                _allowPermission = allowPermission;
            }

            public override string Intercept(VehicleVendorOptions plugin, NPCTalking npcTalking, BasePlayer player, ConversationData conversationData, ResponseNode responseNode)
            {
                int vanillaPrice;
                if (!ConversationUtils.PrecedesPaymentOption(conversationData, responseNode, _matchResponseAction, out vanillaPrice))
                    return string.Empty;

                if (!_getVehicleConfig().RequiresPermission)
                    return string.Empty;

                if (plugin.HasPermissionAny(player.UserIDString, Permission_Allow_All, _allowPermission))
                    return string.Empty;

                plugin.ChatMessage(player, "Error.Vehicle.NoPermission");
                return ConversationUtils.SpeechNodes.Goodbye;
            }
        }

        #endregion

        #region Payment Providers

        private interface IPaymentProvider
        {
            bool IsAvailable { get; }
            int GetBalance(BasePlayer player);
            void TakeBalance(BasePlayer player, int amount);
        }

        private class EconomicsPaymentProvider : IPaymentProvider
        {
            private readonly VehicleVendorOptions _plugin;
            private Plugin _ownerPlugin => _plugin.Economics;

            public EconomicsPaymentProvider(VehicleVendorOptions plugin)
            {
                _plugin = plugin;
            }

            public bool IsAvailable => _ownerPlugin != null;

            public int GetBalance(BasePlayer player)
            {
                return Convert.ToInt32(_ownerPlugin.Call("Balance", player.userID));
            }

            public void TakeBalance(BasePlayer player, int amount)
            {
                _ownerPlugin.Call("Withdraw", player.userID, Convert.ToDouble(amount));
            }
        }

        private class ServerRewardsPaymentProvider : IPaymentProvider
        {
            private readonly VehicleVendorOptions _plugin;
            private Plugin _ownerPlugin => _plugin.ServerRewards;

            public ServerRewardsPaymentProvider(VehicleVendorOptions plugin)
            {
                _plugin = plugin;
            }

            public bool IsAvailable => _ownerPlugin != null;

            public int GetBalance(BasePlayer player)
            {
                return Convert.ToInt32(_ownerPlugin.Call("CheckPoints", player.userID));
            }

            public void TakeBalance(BasePlayer player, int amount)
            {
                _ownerPlugin.Call("TakePoints", player.userID, amount);
            }
        }

        private class ItemsPaymentProvider : IPaymentProvider
        {
            public bool IsAvailable => true;

            private int _itemId;

            public ItemsPaymentProvider(int itemId)
            {
                _itemId = itemId;
            }

            public int GetBalance(BasePlayer player) =>
                player.inventory.GetAmount(_itemId);

            public void TakeBalance(BasePlayer player, int amount) =>
                player.inventory.Take(null, _itemId, amount);
        }

        #endregion

        #region Configuration

        private VehicleConfig GetVehicleConfig(BaseVehicle vehicle)
        {
            // Must go before MiniCopter.
            if (vehicle is ScrapTransportHelicopter)
                return _config.Vehicles.ScrapTransport;

            if (vehicle is MiniCopter)
                return _config.Vehicles.Minicopter;

            // Must go before MotorRowboat.
            if (vehicle is RHIB)
                return _config.Vehicles.RHIB;

            if (vehicle is MotorRowboat)
                return _config.Vehicles.Rowboat;

            // Must go before BaseSubmarine.
            if (vehicle is SubmarineDuo)
                return _config.Vehicles.DuoSub;

            if (vehicle is BaseSubmarine)
                return _config.Vehicles.SoloSub;

            return null;
        }

        private class VehicleConfigMap
        {
            [JsonProperty("ScrapTransport")]
            public VehicleConfig ScrapTransport = new VehicleConfig()
            {
                FuelAmount = 100,
                PricesRequiringPermission = new[]
                {
                    new PriceConfig { Amount = 800 },
                    new PriceConfig { Amount = 400 },
                },
            };

            [JsonProperty("Minicopter")]
            public VehicleConfig Minicopter = new VehicleConfig()
            {
                FuelAmount = 100,
                PricesRequiringPermission = new[]
                {
                    new PriceConfig { Amount = 500 },
                    new PriceConfig { Amount = 250 },
                },
            };

            [JsonProperty("RHIB")]
            public VehicleConfig RHIB = new VehicleConfig()
            {
                FuelAmount = 50,
                PricesRequiringPermission = new[]
                {
                    new PriceConfig { Amount = 200 },
                    new PriceConfig { Amount = 100 },
                },
            };

            [JsonProperty("Rowboat")]
            public VehicleConfig Rowboat = new VehicleConfig()
            {
                FuelAmount = 50,
                PricesRequiringPermission = new[]
                {
                    new PriceConfig { Amount = 80 },
                    new PriceConfig { Amount = 40 },
                },
            };

            [JsonProperty("DuoSub")]
            public VehicleConfig DuoSub = new VehicleConfig()
            {
                FuelAmount = 50,
                PricesRequiringPermission = new[]
                {
                    new PriceConfig { Amount = 200 },
                    new PriceConfig { Amount = 100 },
                },
            };

            [JsonProperty("SoloSub")]
            public VehicleConfig SoloSub = new VehicleConfig()
            {
                FuelAmount = 50,
                PricesRequiringPermission = new[]
                {
                    new PriceConfig { Amount = 125 },
                    new PriceConfig { Amount = 50 },
                },
            };

            public void RegisterCustomPricePermissions(VehicleVendorOptions plugin)
            {
                ScrapTransport.InitAndValidate(plugin, ShortName_ScrapHeli);
                Minicopter.InitAndValidate(plugin, ShortName_Minicopter);
                RHIB.InitAndValidate(plugin, ShortName_RHIB);
                Rowboat.InitAndValidate(plugin, ShortName_Rowboat);
                DuoSub.InitAndValidate(plugin, ShortName_DuoSub);
                SoloSub.InitAndValidate(plugin, ShortName_SoloSub);
            }
        }

        private class VehicleConfig
        {
            private static PriceConfig FreePriceConfig = new PriceConfig { Amount = 0 };

            [JsonProperty("RequiresPermission")]
            public bool RequiresPermission = false;

            [JsonProperty("FuelAmount")]
            public int FuelAmount = 100;

            [JsonProperty("DespawnProtectionSeconds")]
            public float DespawnProtectionSeconds = 300;

            [JsonProperty("PricesRequiringPermission")]
            public PriceConfig[] PricesRequiringPermission = new PriceConfig[0];

            public void InitAndValidate(VehicleVendorOptions plugin, string vehicleType)
            {
                foreach (var priceConfig in PricesRequiringPermission)
                {
                    priceConfig.InitAndValidate(plugin, vehicleType);
                    plugin.permission.RegisterPermission(priceConfig.Permission, plugin);
                }
            }

            public PriceConfig GetPriceForPlayer(VehicleVendorOptions plugin, IPlayer player, string freePermission)
            {
                if (plugin.HasPermissionAny(player, Permission_Free_All, freePermission))
                    return FreePriceConfig;

                if (PricesRequiringPermission == null)
                    return null;

                for (var i = PricesRequiringPermission.Length - 1; i >= 0; i--)
                {
                    var priceConfig = PricesRequiringPermission[i];
                    if (priceConfig.IsValid && player.HasPermission(priceConfig.Permission))
                        return priceConfig;
                }

                return null;
            }
        }

        private class PriceConfig
        {
            [JsonProperty("UseEconomics", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool UseEconomics = false;

            [JsonProperty("UseServerRewards", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool UseServerRewards = false;

            [JsonProperty("ItemShortName", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string ItemShortName = "scrap";

            [JsonProperty("Amount")]
            public int Amount;

            [JsonIgnore]
            public string Permission;

            [JsonIgnore]
            public IPaymentProvider PaymentProvider;

            [JsonIgnore]
            public bool IsValid => (PaymentProvider?.IsAvailable ?? false) && Permission != string.Empty;

            private ItemDefinition _itemDefinition;
            [JsonIgnore]
            public ItemDefinition ItemDef
            {
                get
                {
                    if (_itemDefinition != null)
                        return _itemDefinition;

                    // This could be called during server boot, in which case the ItemManager is not yet initialized.
                    // It's generally safe and performant to call ItemManager.Initialize() any time, and it will only initialize once.
                    ItemManager.Initialize();
                    ItemManager.itemDictionaryByName.TryGetValue(ItemShortName, out _itemDefinition);
                    return _itemDefinition;
                }
            }

            public bool MatchesVanillaPrice(int vanillaPrice)
            {
                return PaymentProvider is ItemsPaymentProvider
                    && ItemShortName == "scrap"
                    && Amount == vanillaPrice;
            }

            public void InitAndValidate(VehicleVendorOptions plugin, string vehicleType)
            {
                Permission = GeneratePermission(vehicleType);
                PaymentProvider = CreatePaymentProvider(plugin);
            }

            private IPaymentProvider CreatePaymentProvider(VehicleVendorOptions plugin)
            {
                if (UseEconomics)
                    return new EconomicsPaymentProvider(plugin);

                if (UseServerRewards)
                    return new ServerRewardsPaymentProvider(plugin);

                if (ItemDef == null)
                {
                    plugin.LogError($"Price config contains an invalid item short name: '{ItemShortName}'.");
                    return null;
                }

                return new ItemsPaymentProvider(ItemDef.itemid);
            }

            private string GeneratePermission(string vehicleType)
            {
                if (Amount == 0)
                {
                    Permission = $"{Permission_Price_Prefix}.{vehicleType}.free";
                }
                else
                {
                    var currencyType = UseEconomics ? "economics"
                        : UseServerRewards ? "serverrewards"
                        : ItemShortName;

                    if (string.IsNullOrEmpty(ItemShortName))
                        return string.Empty;

                    Permission = $"{Permission_Price_Prefix}.{vehicleType}.{currencyType}.{Amount}";
                }

                return Permission;
            }

            public bool CanPlayerAfford(BasePlayer player)
            {
                if (Amount <= 0)
                    return true;

                return PaymentProvider.GetBalance(player) >= Amount;
            }

            public bool TryChargePlayer(BasePlayer player)
            {
                if (Amount <= 0)
                    return true;

                PaymentProvider.TakeBalance(player, Amount);
                return true;
            }
        }

        private class Configuration : BaseConfiguration
        {
            [JsonProperty("Vehicles")]
            public VehicleConfigMap Vehicles = new VehicleConfigMap();
        }

        private Configuration GetDefaultConfig() => new Configuration();

        #endregion

        #region Configuration Helpers

        internal class BaseConfiguration
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

        private bool MaybeUpdateConfig(BaseConfiguration config)
        {
            var currentWithDefaults = config.ToDictionary();
            var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);
            return MaybeUpdateConfigDict(currentWithDefaults, currentRaw);
        }

        private bool MaybeUpdateConfigDict(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            var changed = false;

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

        protected override void LoadDefaultConfig() => _config = GetDefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null)
                {
                    throw new JsonException();
                }

                if (MaybeUpdateConfig(_config))
                {
                    LogWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch (Exception e)
            {
                LogError(e.Message);
                LogWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Log($"Configuration changes saved to {Name}.json");
            Config.WriteObject(_config, true);
        }

        #endregion

        #region Localization

        private void ChatMessage(BasePlayer player, string messageName, params object[] args) =>
            player.ChatMessage(string.Format(GetMessage(player.UserIDString, messageName), args));

        private string GetMessage(BasePlayer player, string messageName, params object[] args) =>
            GetMessage(player.IPlayer, messageName, args);

        private string GetMessage(IPlayer player, string messageName, params object[] args) =>
            GetMessage(player.Id, messageName, args);

        private string GetMessage(string playerId, string messageName, params object[] args)
        {
            var message = lang.GetMessage(messageName, this, playerId);
            return args.Length > 0 ? string.Format(message, args) : message;
        }

        private string GetItemNameLocalizationKey(string itemShortName) => $"Item.{itemShortName}";

        private void AddEnglishItemNamesForPriceConfigs(Dictionary<string, string> messages, PriceConfig[] priceConfigs)
        {
            foreach (var priceConfig in priceConfigs)
            {
                if (string.IsNullOrEmpty(priceConfig.ItemShortName))
                    continue;

                var localizationKey = GetItemNameLocalizationKey(priceConfig.ItemShortName);
                messages[localizationKey] = priceConfig.ItemDef.displayName.english;
            }
        }

        protected override void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                ["Error.Vehicle.NoPermission"] = "You don't have permission to buy that vehicle.",
                ["UI.ActualPrice"] = "Actual price: {0}",
                ["UI.Price.Free"] = "Free",
                ["UI.Currency.Economics"] = "{0:C}",
                ["UI.Currency.ServerRewards"] = "{0} reward points",
            };

            AddEnglishItemNamesForPriceConfigs(messages, _config.Vehicles.ScrapTransport.PricesRequiringPermission);
            AddEnglishItemNamesForPriceConfigs(messages, _config.Vehicles.Minicopter.PricesRequiringPermission);
            AddEnglishItemNamesForPriceConfigs(messages, _config.Vehicles.RHIB.PricesRequiringPermission);
            AddEnglishItemNamesForPriceConfigs(messages, _config.Vehicles.Rowboat.PricesRequiringPermission);
            AddEnglishItemNamesForPriceConfigs(messages, _config.Vehicles.DuoSub.PricesRequiringPermission);
            AddEnglishItemNamesForPriceConfigs(messages, _config.Vehicles.SoloSub.PricesRequiringPermission);

            lang.RegisterMessages(messages, this, "en");
        }

        #endregion
    }
}
