# Honey, I shrunk the employees! #

In this more than cosmetic Lethal Company mod, players can experience the game from a new perspective. The pros and cons of being small are many, and unique interactions are yet to be discovered...

## Wiki ##
A wiki containing the list of features this mod adds can be found in our [github repository](https://github.com/MehimoNemo/LethalCompanyShrinkRay/wiki)

## Configuration ##
|  Group       |          Option                  |                           Description                                                      | Possible values                        | Default |
| ------------ | -------------------------------- | ------------------------------------------------------------------------------------------ | -------------------------------------- | ------- |
| General      | ShrinkRayCost                    | Store cost of the shrink ray                                                               | any number                             | 0 (BETA)|
|              | DeathShrinking                   | If true, a player can be shrunk below 0.2, resulting in an instant death.                  | true / false                           | false   |
|              | ShrinkRayTargetHighlighting      | Defines, when a target gets highlighted. Set to OnLoading on performance issues.           | Off, OnHit, OnLoading                  | OnHit   |
| Sizing       | DefaultPlayerSize                | The default player size when joining a lobby or reviving.                                  | 0.2 - 1.7                              | 1       |
|              | MaximumPlayerSize                | Defines, how tall a player can become (1.7 is the maximum for the ship inside and doors!)  | Max(1, DefaultPlayerSize) - any number | 1.7     |
|              | PlayerSizeChangeStep             | Defines how much a player shrinks/enlarges.                                                | 0.05 - 0.8                             | 0.4     |
|              | ItemSizeChangeStep               | Defines how much an item shrinks/enlarges. Set to 0 for disabling item scaling.            | 0 - 0.8                                | 0.5     |
|              | ItemScalingVisualOnly            | If true, scaling items has no special effects.                                             | true / false                           | false   |
|              | EnemySizeChangeStep              | Defines how much an enemy shrinks/enlarges. Set to 0 for disabling item scaling.           | 0 - 0.8                                | 0.5     |
| Shrunken     | MovementSpeedMultiplier          | Speed multiplier for shrunken players                                                      | 0.5 (slow) - 1.5 (fast)                | 1.3     |
|              | JumpHeightMultiplier             | Jump-height multiplier for shrunken players                                                | 0.5 (lower) - 2 (higher)               | 1.3     |
|              | WeightMultiplier                 | Weight multiplier applied on held items of shrunken players                                | 0.5 (lighter) - 2 (heavier)            | 1.5     |
|              | CanUseVents                      | If true, shrunken players can teleport between vents.                                      | true / false                           | true    |
|              | PitchDistortionIntensity \*      | Intensity of the pitch distortion for players with a different size than the local player. | 0 (unchanged) - 0.5 (strong pitch)     | 0.3     |
|              | CanEscapeGrab                    | If true, a player who got grabbed can escape by jumping.                                   | true / false                           | true    |
|              | CantOpenStorageCloset            | If true, a shrunken player can't open or close the storage closet. For the evil minded..   | true / false                           | false   |
| Interactions | JumpOnShrunkenPlayers            | If true, larger players can harm smaller ones by jumping on them.                          | true / false                           | true    |
|              | ThrowablePlayers                 | If true, shrunken players can be thrown by their holder.                                   | true / false                           | true    |
|              | FriendlyFlight                   | If true, held players can grab their holder, causing comedic, but game breaking effects.   | true / false                           | false   |
| Enemies      | EnemyPitchDistortionIntensity \* | Intensity of the pitch distortion for enemies with a different size than the local player. | 0 (unchanged) - 0.5 (strong pitch)     | 0.2     |
|              | HoarderBugBehaviour              | Defines if hoarding bugs should be able to grab you and how likely that is                 | Default, NoGrab, Addicted              | Default |
|              | ThumperBehaviour                 | Defines the way Thumpers react on shrunken players.	                                       | Default, One-Shot, Bumper              | Bumper  |
> client-sided options are marked with a \*, others will by synced with the host.

## Known bugs ##
A list of known bugs can be found on our Github repository in the [issues section](https://github.com/MehimoNemo/LethalCompanyShrinkRay/issues)

## Planned features ##
A list of planned features can be found on our Github wiki in the [content ideas section](https://github.com/MehimoNemo/LethalCompanyShrinkRay/wiki/Content-ideas)

## Got more ideas? ##
Send us a message on discord
+ big_nemo
+ niro1996
+ abovefire

or leave a message in the following discord server:
[LittleCompany Discord](https://discord.gg/63KdxhQ2Dn)
[Unofficial Modding Discord](https://discord.gg/nYcQFEpXfU) \| [LittleCompany-Thread](https://discord.com/channels/1169792572382773318/1190100786357743646)
[Modding Discord](https://discord.gg/nYcQFEpXfU) \| [LittleCompany-Thread](https://discord.com/channels/1168655651455639582/1206337352608256010)

## Special Thanks To
[AboveFire](https://github.com/AboveFire) for development help!
[@peppermint_2859](https://twitter.com/ItsJOEYthe) on discord for the ShrinkRay model!
[Sakiskid](https://github.com/Sakiskid) for the ShrinkRay beam!
[MissScarlett](https://github.com/QueenScarlett23) for contributing to the mod.
Ellethwen on discord for the potion & player item icons!
Spinmaster on discord for creating all the sounds this mod includes!


## And the testers too (sorry for making you join and leave the lobby a billion times)!
[StrawberriStorm](https://twitter.com/strawberristorm) | [Tcorn](https://twitter.com/TcorntheLazy) | [Sirdog](https://youtu.be/6ItPIiegBms?si=zH-Cf467VIOtVTMt) | [IceSigil](https://twitter.com/IceSigil) | [NimNom](https://www.twitch.tv/nimnom)

