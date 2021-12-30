## Features

This plugin allows configuring vehicles spawned by NPC vendors.

- Allows configuring initial fuel amount for each vehicle type, overriding the vanilla default (50 for boats, and 20% of max stack size for helicopters)
- Allows assigning ownership when players purchase vehicles, so other plugins can enable features based on their permissions
- Allows adjusting vehicle prices for players with permission
- Allows selling vehicles for Economics or Server Rewards currency
- Allows restricting vehicles to permission
- Allows configuring how long purchased vehicles can safely occupy the spawn location

## Permissions

### Vehicle ownership

- `vehiclevendoroptions.ownership.allvehicles` - Granting this to a player makes it so all vehicles they purchase from NPC vendors will spawn with the vehicle's `OwnerID` set to the player's Steam ID. This allows various plugins to enable features for the vehicle based on that player's permissions, such as no decay with [Vehicle Decay Protection](https://umod.org/plugins/vehicle-decay-protection).

Alternatively, you can grant permissions by vehicle type:

- `vehiclevendoroptions.ownership.scraptransport`
- `vehiclevendoroptions.ownership.minicopter`
- `vehiclevendoroptions.ownership.rhib`
- `vehiclevendoroptions.ownership.rowboat`
- `vehiclevendoroptions.ownership.duosub`
- `vehiclevendoroptions.ownership.solosub`
- `vehiclevendoroptions.ownership.ridablehorse`

### Vehicle prices

The following permissions come with this plugin's **default configuration**. Granting one to a player determines the price they must pay for the corresponding vehicle type.

- `vehiclevendoroptions.price.scraptransport.scrap.800`
- `vehiclevendoroptions.price.scraptransport.scrap.400`
- `vehiclevendoroptions.price.minicopter.scrap.500`
- `vehiclevendoroptions.price.minicopter.scrap.250`
- `vehiclevendoroptions.price.rhib.scrap.200`
- `vehiclevendoroptions.price.rhib.scrap.100`
- `vehiclevendoroptions.price.rowboat.scrap.80`
- `vehiclevendoroptions.price.rowboat.scrap.40`
- `vehiclevendoroptions.price.duosub.scrap.200`
- `vehiclevendoroptions.price.duosub.scrap.100`
- `vehiclevendoroptions.price.solosub.scrap.125`
- `vehiclevendoroptions.price.solosub.scrap.50`

You can add more custom prices in the plugin configuration under each vehicle type (`PricesRequiringPermission`), and the plugin will automatically generate a permission of the format `vehiclevendoroptions.price.<vehicle>.<item>.<amount>`. If a player has permission to multiple prices for a given vehicle type, only the last will apply, based on the order in the config.

### Free vehicles

- `vehiclevendoroptions.free.allvehicles` -- Granting this to a player allows them to obtain vehicles from NPC vendors for free.

Alternatively, you can grant permission by vehicle type:

- `vehiclevendoroptions.free.scraptransport`
- `vehiclevendoroptions.free.minicopter`
- `vehiclevendoroptions.free.rhib`
- `vehiclevendoroptions.free.rowboat`
- `vehiclevendoroptions.free.duosub`
- `vehiclevendoroptions.free.solosub`
- `vehiclevendoroptions.free.ridablehorse`

Note: Having permission to free horses still requires a saddle to be in your inventory for the claim option to appear client-side, but claiming the horse will not consume the saddle.

### Restrict vehicles

To restrict vehicles to permission, set `RequiresPermission` to `true` in the plugin configuration under each vehicle type that you want to restrict. When a vehicle is restricted, players will only be allowed to purchase it if they have the corresponding permissions below.

- `vehiclevendoroptions.allow.all` (all vehicles)
- `vehiclevendoroptions.allow.scraptransport`
- `vehiclevendoroptions.allow.minicopter`
- `vehiclevendoroptions.allow.rhib`
- `vehiclevendoroptions.allow.rowboat`
- `vehiclevendoroptions.allow.duosub`
- `vehiclevendoroptions.allow.solosub`

## Configuration

Default configuration:

```json
{
  "Vehicles": {
    "ScrapTransport": {
      "RequiresPermission": false,
      "FuelAmount": 100,
      "DespawnProtectionSeconds": 300.0,
      "PricesRequiringPermission": [
        {
          "ItemShortName": "scrap",
          "Amount": 800
        },
        {
          "ItemShortName": "scrap",
          "Amount": 400
        }
      ]
    },
    "Minicopter": {
      "RequiresPermission": false,
      "FuelAmount": 100,
      "DespawnProtectionSeconds": 300.0,
      "PricesRequiringPermission": [
        {
          "ItemShortName": "scrap",
          "Amount": 500
        },
        {
          "ItemShortName": "scrap",
          "Amount": 250
        }
      ]
    },
    "RHIB": {
      "RequiresPermission": false,
      "FuelAmount": 50,
      "DespawnProtectionSeconds": 300.0,
      "PricesRequiringPermission": [
        {
          "ItemShortName": "scrap",
          "Amount": 200
        },
        {
          "ItemShortName": "scrap",
          "Amount": 100
        }
      ]
    },
    "Rowboat": {
      "RequiresPermission": false,
      "FuelAmount": 50,
      "DespawnProtectionSeconds": 300.0,
      "PricesRequiringPermission": [
        {
          "ItemShortName": "scrap",
          "Amount": 80
        },
        {
          "ItemShortName": "scrap",
          "Amount": 40
        }
      ]
    },
    "DuoSub": {
      "RequiresPermission": false,
      "FuelAmount": 50,
      "DespawnProtectionSeconds": 300.0,
      "PricesRequiringPermission": [
        {
          "ItemShortName": "scrap",
          "Amount": 200
        },
        {
          "ItemShortName": "scrap",
          "Amount": 100
        }
      ]
    },
    "SoloSub": {
      "RequiresPermission": false,
      "FuelAmount": 50,
      "DespawnProtectionSeconds": 300.0,
      "PricesRequiringPermission": [
        {
          "ItemShortName": "scrap",
          "Amount": 125
        },
        {
          "ItemShortName": "scrap",
          "Amount": 50
        }
      ]
    }
  }
}
```

Each vehicle has the following options.

- `RequiresPermission` (`true` or `false`) -- While `true`, players must have the `vehiclevendoroptions.allow.<vehicle>` permission to purchase vehicles of this type. While `false`, the vehicle type is open to all players.
- `FuelAmount` -- The amount of low grade fuel to put in the vehicle's fuel tank when it spawns.
  - Set to `-1` for the max stack size of low grade fuel on your server.
- `DespawnProtectionSeconds` -- The amount of time that purchased vehicles will be safe in the spawn location before new ones can be spawned in their place. Reducing this is favorable in high population servers and/or where vehicles are relatively cheap.
  - Note: If you are having an issue where players cannot buy vehicles quickly enough in succession, you may want to consider using [Monument Addons](https://umod.org/plugins/monument-addons) to add more vehicle vendors to monuments.
- `PricesRequiringPermission` -- List of prices that can be granted to players with permission. Each price config generates a permission of the format `vehiclevendoroptions.price.<vehicle>.<item>.<amount>`. Granting one to a player overrides the price they will be charged. They will also see UI text on the screen indicating what the real price is, since the vanilla UI cannot be changed.
  - `ItemShortName` (default: `"scrap"`) -- The short name of the item to charge the player.
  - `Amount` -- The amount of items, Economics currency or Server Rewards points required to purchase the vehicle.
  - `UseEconomics` (`true` or `false`) -- While `true`, the `Amount` represents the price in [Economics](https://umod.org/plugins/economics) currency.
    - Generated permission format: `vehiclevendoroptions.price.<vehicle>.economics.<amount>`
    - If the Economics plugin is not loaded when the player talks to the vendor, another price config will be selected for the user if they have permission to any, or else the vanilla price will be used.
  - `UseServerRewards` (`true` or `false`) -- While `true`, the `Amount` represents the price in [Server Rewards](https://umod.org/plugins/server-rewards) points.
    - Generated permission format: `vehiclevendoroptions.price.<vehicle>.serverrewards.<amount>`
    - If the Server Rewards plugin is not loaded when the player talks to the vendor, another price config will be selected for the user if they have permission to any, or else the vanilla price will be used.

For reference, here are the vanilla scrap prices for vehicles.
- ScrapTransport: `1250`
- Minicopter: `750`
- RHIB: `300`
- Rowboat: `125`
- Duo sub: `300`
- Solo sub: `200`

## Localization

If you are using custom prices, you can translate the displayed item name by using the localization key `"Item.<shortname>"`, such as `"Item.scrap": "Scrap"`. These will be automatically generated for English based on the item short names in the `PricesRequiringPermission` configs.

```json
{
  "Error.Vehicle.NoPermission": "You don't have permission to buy that vehicle.",
  "UI.ActualPrice": "Actual price: {0}",
  "UI.Price.Free": "Free",
  "UI.Currency.Economics": "{0:C}",
  "UI.Currency.ServerRewards": "{0} reward points",
  "Item.scrap": "Scrap"
}
```
