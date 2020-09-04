**Vehicle Vendor Options** allows you to alter fuel and ownership of boats and helicopters spawned at NPC vendors like Airwolf and Lobster.

## Permissions

Granting the following permission to a player will make it so all vehicles they purchase from vendors will spawn with that player set as the vehicle's owner (`OwnerID` property set to the player's Steam ID). This will allow various plugins to enable certain features for the vehicle, such as being able to pick it up.

- `vehiclevendoroptions.ownership.allvehicles`

Alternatively, you can grant permissions by vehicle type:

- `vehiclevendoroptions.ownership.minicopter`
- `vehiclevendoroptions.ownership.scraptransport`
- `vehiclevendoroptions.ownership.rowboat`
- `vehiclevendoroptions.ownership.rhib`

## Configuration

Default configuration:
```json
{
  "Vehicles": {
    "Minicopter": {
      "FuelAmount": 100
    },
    "RHIB": {
      "FuelAmount": 50
    },
    "Rowboat": {
      "FuelAmount": 50
    },
    "ScrapTransport": {
      "FuelAmount": 100
    }
  }
}
```

- `FuelAmount` -- The amount of low grade fuel to put in the vehicle's fuel tank when it spawns. Set to `-1` for the max stack size of low grade fuel on your server.
