## Features

This plugin allows configuring several aspects of vehicles spawned by NPC vendors.

- Configure initial fuel amount for each vehicle type, instead of using the game's default (50 for boats, and 20% of max stack size for helicopters).
- Properly assign vehicle ownership to the player that purchases it, so other plugins can enable features based on the player's permissions.
- Allows players with permission to obtain vehicles for free.
- Allows adjusting vehicle prices.

## Permissions

### Vehicle Ownership

- `vehiclevendoroptions.ownership.allvehicles` - Granting this to a player will make it so all vehicles they purchase from NPC vendors will spawn with the vehicle's `OwnerID` set to the player's Steam ID. This will allow various plugins to enable features for the vehicle based on that player's permissions, such as no decay with [Vehicle Decay Protection](https://umod.org/plugins/vehicle-decay-protection).

Alternatively, you can grant permissions by vehicle type:

- `vehiclevendoroptions.ownership.minicopter`
- `vehiclevendoroptions.ownership.scraptransport`
- `vehiclevendoroptions.ownership.rowboat`
- `vehiclevendoroptions.ownership.rhib`
- `vehiclevendoroptions.ownership.ridablehorse`

### Free Vehicles

- `vehiclevendoroptions.free.allvehicles` -- Granting this to a player will allow them to obtain vehicles from NPC vendors for free. This works by skipping the dialog node where they are prompted to pay.

Alternatively, you can grant permission by vehicle type:

- `vehiclevendoroptions.free.minicopter`
- `vehiclevendoroptions.free.scraptransport`
- `vehiclevendoroptions.free.rowboat`
- `vehiclevendoroptions.free.rhib`
- `vehiclevendoroptions.free.ridablehorse`

Note: Having permission to free horses still requires a saddle to be in your inventory for the claim option to appear client-side, but claiming the horse will not consume the saddle.

## Configuration

Default configuration:
```json
{
  "Vehicles": {
    "Minicopter": {
      "FuelAmount": 100,
      "ScrapCost": -1
    },
    "ScrapTransport": {
      "FuelAmount": 100,
      "ScrapCost": -1
    },
    "Rowboat": {
      "FuelAmount": 50,
      "ScrapCost": -1
    },
    "RHIB": {
      "FuelAmount": 50,
      "ScrapCost": -1
    }
  }
}
```

- `FuelAmount` -- The amount of low grade fuel to put in the vehicle's fuel tank when it spawns.
  - Set to `-1` for the max stack size of low grade fuel on your server.
- `ScrapCost` -- The amount of scrap required to purchase the vehicle.
  - Note: Changing this will unfortunately not change the price displayed on the UI since that is determined client-side.
  - Set to `-1` for vanilla prices.
    - This option is recommended if you want the prices to update automatically when the game developer changes them.
    - This is the default setting, in case you didn't install the plugin for the purpose of changing prices.
  - For reference, vanilla prices are:
    - Minicopter: `750`
    - ScrapTransport: `1250`
    - Rowboat: `125`
    - RHIB: `300`
