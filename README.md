[![Thunderstore Version](https://img.shields.io/thunderstore/v/Toybox/LittleCompany?style=for-the-badge&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/Toybox/LittleCompany/)
[![Thunderstore Downloads](https://img.shields.io/thunderstore/dt/Toybox/LittleCompany?style=for-the-badge&color=yellow&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/Toybox/LittleCompany/)
[![Discord invite](https://img.shields.io/discord/1192981794451107993?style=for-the-badge&logo=discord&logoColor=white&label=Discord&link=https%3A%2F%2Fdiscord.gg%2F63KdxhQ2Dn)](https://discord.gg/63KdxhQ2Dn)
[![Wiki](https://img.shields.io/badge/Wiki-b?logo=github&style=for-the-badge&logoColor=white&label=GitHub&color=white&link=https%3A%2F%2Fgithub.com%2FMehimoNemo%2FLethalCompanyShrinkRay%2Fwiki)](https://github.com/MehimoNemo/LethalCompanyShrinkRay/wiki)

# Honey, I shrunk the employees! #

In this more than cosmetic Lethal Company mod, players can experience the game from a new perspective. The pros and cons of being small are many, and unique interactions are yet to be discovered...

## Configuration ##
|  Group       |          Option                         |                           Description                                                        | Possible values                        | Default |
| ------------ | --------------------------------------- | -------------------------------------------------------------------------------------------- | -------------------------------------- | ------- |
| General      | ShrinkRayCost                           | Store cost of the shrink ray                                                                 | any number                             | 1000    |
|              | ShrinkRayShotsPerCharge                 | Amount of shots per charge for the shrink ray. Set to 0 for unlimited.                       | Any number                             | 7       |
|              | ShrinkRayNoRecharge                     | If true, the shrink ray can't be recharged and will overheat once battery is at zero.        | true / false                           | false   |
|              | DeathShrinking                          | If true, a player can be shrunk below 0.2, resulting in an instant death.                    | true / false                           | false   |
| Sizing       | DefaultPlayerSize                       | The default player size when joining a lobby or reviving.                                    | 0.2 - 1.7                              | 1       |
|              | MaximumPlayerSize                       | Defines, how tall a player can become (1.7 is the maximum for the ship inside and doors!)    | Max(1, DefaultPlayerSize) - any number | 1.7     |
|              | PlayerSizeChangeStep                    | Defines how much a player shrinks/enlarges.                                                  | 0.05 - 0.8                             | 0.4     |
|              | ItemSizeChangeStep                      | Defines how much an item shrinks/enlarges. Set to 0 for disabling item scaling.              | 0 - 0.8                                | 0.4     |
|              | ItemScalingVisualOnly                   | If true, scaling items has no special effects.                                               | true / false                           | false   |
|              | ShipItemSizeChangeStep                  | Defines how much a ship object shrinks/enlarges. Set to 0 for disabling ship object scaling. | 0 - 0.8                                | 0.2     |
|              | EnemySizeChangeStep                     | Defines how much an enemy shrinks/enlarges. Set to 0 for disabling item scaling.             | 0 - 0.8                                | 0.5     |
|              | vehicleSizeChangeStep                   | Defines how much a vehicle shrinks/enlarges in one step. Set to 0 to disable this feature.   | 0 - 0.8                                | 0.2     |
| Shrunken     | MovementSpeedMultiplier                 | Speed multiplier for shrunken players                                                        | 0.5 (slow) - 1.5 (fast)                | 1.3     |
|              | JumpHeightMultiplier                    | Jump-height multiplier for shrunken players                                                  | 0.5 (lower) - 2 (higher)               | 1.3     |
|              | WeightMultiplier                        | Weight multiplier applied on held items of shrunken players                                  | 0.5 (lighter) - 2 (heavier)            | 1.5     |
|              | CanUseVents                             | If true, shrunken players can teleport between vents.                                        | true / false                           | true    |
|              | PitchDistortionIntensity \*             | Intensity of the pitch distortion for players with a different size than the local player.   | 0 (unchanged) - 0.5 (strong pitch)     | 0.3     |
|              | CanEscapeGrab                           | If true, a player who got grabbed can escape by jumping.                                     | true / false                           | true    |
|              | CantOpenStorageCloset                   | If true, a shrunken player can't open or close the storage closet. For the evil minded..     | true / false                           | false   |
| Interactions | JumpOnShrunkenPlayers                   | If true, larger players can harm smaller ones by jumping on them.                            | true / false                           | true    |
|              | ThrowablePlayers                        | If true, shrunken players can be thrown by their holder.                                     | true / false                           | true    |
|              | SellablePlayers                         | If true, shrunken players can be sold to the company.                                        | true / false                           | true    |
| Enemies      | EnemyPitchDistortionIntensity \*        | Intensity of the pitch distortion for enemies with a different size than the local player.   | 0 (unchanged) - 0.5 (strong pitch)     | 0.2     |
|              | HoarderBugBehaviour                     | Defines if hoarding bugs should be able to grab you and how likely that is.                  | Default, NoGrab, Addicted              | Default |
|              | DeathShrinkEventChance                  | Chance for an event when shrinking an enemy to death.                                        | 0 - 100                                | 100     |
|              | ThumperBehaviour                        | Defines the way Thumpers react on shrunken players.	                                        | Default, One-Shot, Bumper              | Bumper  |
| Potions      | ShrinkPotionShopPrice                   | Sets the store price. 0 to removed potion from store.	                                    | 0 - 500                                | 30      |
|              | ShrinkPotionScrapRarity                 | Sets the scrap rarity. 0 makes it unable to spawn inside.	                                | 0 - 100                                | 10      |
|              | EnlargePotionStorePrice                 | Sets the store price. 0 to removed potion from store.	                                    | 0 - 500                                | 50      |
|              | EnlargePotionScrapRarity                | Sets the scrap rarity. 0 makes it unable to spawn inside.	                                | 0 - 100                                | 5       |
| Vehicles     | ResizeWhenInVehicle                     | If set to false, you will not get resized when entering a vehicle.                           | true / false                           | false   |
| Experimental | UseLethalLevelLoaderForItemRegistration | If true, LLL is the prefered way for loading items. If false, LethalLib is used.             | true / false                           | false   |
|              | RemoveMinimumSizeLimit                  | If true, the minimum size limit changes from 0.2 to 0.05. This may cause bugs!               | true / false                           | false   |
> client-sided options are marked with a \*, others will by synced with the host.

## Known bugs ##
A list of known bugs can be found on our Github repository in the [issues section](https://github.com/MehimoNemo/LethalCompanyShrinkRay/issues)

## Planned features ##
A list of planned features can be found on our Github wiki in the [content ideas section](https://github.com/MehimoNemo/LethalCompanyShrinkRay/wiki/Content-ideas)

## Related mods ##
[RandomEnemiesSize](https://thunderstore.io/c/lethal-company/p/Wexop/RandomEnemiesSize/) - Set random spawn sizes for enemies and map hazards

## Got more ideas? ##
Send us a message on discord
+ big_nemo
+ niro1996
+ abovefire

or leave a message in the following discord server:
+ [LittleCompany Discord](https://discord.gg/63KdxhQ2Dn)
+ [Unofficial Modding Discord](https://discord.gg/nYcQFEpXfU) \| [LittleCompany-Thread](https://discord.com/channels/1169792572382773318/1190100786357743646)
+ [Modding Discord](https://discord.gg/nYcQFEpXfU) \| [LittleCompany-Thread](https://discord.com/channels/1168655651455639582/1206337352608256010)

## Credits
+ Created by [Nemo](https://github.com/MehimoNemo) and [Niro](https://github.com/NiroDev)
+ Development help in terms of compatibility and stability by [AboveFire](https://github.com/AboveFire)
+ The ShrinkRay model created by [@peppermint_2859](https://twitter.com/ItsJOEYthe)
+ Intensive testing and feedback through github by [CoolLKKPS](https://github.com/CoolLKKPS)
+ The SrhinkRay beam made by [Sakiskid](https://github.com/Sakiskid)
+ [MissScarlett](https://github.com/QueenScarlett23) for contributing to the mod.
+ Ellethwen on discord for the potion & player item icons!
+ Spinmaster on discord for creating all the sounds this mod includes!
+ Everyone who helped with testing: [StrawberriStorm](https://twitter.com/strawberristorm) | [Tcorn](https://twitter.com/TcorntheLazy) | [Sirdog](https://youtu.be/6ItPIiegBms?si=zH-Cf467VIOtVTMt) | [IceSigil](https://twitter.com/IceSigil) | [NimNom](https://www.twitch.tv/nimnom)

