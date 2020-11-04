## Features

This plugin allows configuring several aspects of vehicles spawned by NPC vendors.

- Configure initial fuel amount for each vehicle type, instead of using the game's default which is 20% of max stack size.
- Properly assign vehicle ownership to the player that purchases it, so other plugins can enable features based on the player's permissions.
- Allow players with permission to obtain vehicles for free.

## Permissions

### Vehicle Ownership

- `vehiclevendoroptions.ownership.allvehicles` - Granting this to a player will make it so all vehicles they purchase from NPC vendors will spawn with the vehicle's `OwnerID` set to the player's Steam ID. This will allow various plugins to enable features for the vehicle based on that player's permissions, such as no decay with [Vehicle Decay Protection](https://umod.org/plugins/vehicle-decay-protection).

Alternatively, you can grant permissions by vehicle type:

- `vehiclevendoroptions.ownership.minicopter`
- `vehiclevendoroptions.ownership.scraptransport`
- `vehiclevendoroptions.ownership.rowboat`
- `vehiclevendoroptions.ownership.rhib`

### Free Vehicles

- `vehiclevendoroptions.free.allvehicles` -- Granting this to a player will allow them to obtain vehicles from NPC vendors for free. This works by skipping the dialog node where they are prompted to pay.

Alternatively, you can grant permission by vehicle type:

- `vehiclevendoroptions.free.minicopter`
- `vehiclevendoroptions.free.scraptransport`
- `vehiclevendoroptions.free.rowboat`
- `vehiclevendoroptions.free.rhib`

## Configuration

Default configuration:
```json
{
  "Vehicles": {
    "Minicopter": {
      "FuelAmount": 100
    },
    "ScrapTransport": {
      "FuelAmount": 100
    },
    "Rowboat": {
      "FuelAmount": 50
    },
    "RHIB": {
      "FuelAmount": 50
    }
  }
}
```

`FuelAmount` -- The amount of low grade fuel to put in the vehicle's fuel tank when it spawns. Set to `-1` for the max stack size of low grade fuel on your server.
