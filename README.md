Plugin allows users to go invisible for a set period of time using a molotov or inc grenade. These change be changed to whatever weapons you want, just look in the config file thats created on first load. Config messages need to be updated, but everything else works. Make sure gloves are set to default gloves otherwise they will be visible.

Example of config file:

```
{
  "InvisibilityDuration": 5,
  "CooldownDuration": 15,
  "InvisibilityMessage": "🫥 You are now invisible!",
  "VisibilityMessage": "✨ You are visible again!",
  "TriggerWeapons": [
    "weapon_molotov",
    "weapon_incgrenade"
  ]
}
