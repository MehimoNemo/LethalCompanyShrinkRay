# Honey, I shrunk the employees! #

In this more than cosmetic Lethal Company mod, players can experience the game from a new perspective. The pros and cons of being small are many, and unique interactions are yet to be discovered...

## Configuration ##
|  Group       |          Option             |                           Description                                                     | Possible values                        | Default |
| ------------ | --------------------------- | ----------------------------------------------------------------------------------------- | -------------------------------------- | ------- |
| General      | ShrinkRayCost               | Store cost of the shrink ray                                                              | any number                             | 0 (BETA)|
|              | DeathShrinking              | If true, a player can be shrunk below 0.2, resulting in an instant death.                 | true / false                           | false   |
|              | SizeChangeStep              | Defines how much a player shrinks/enlarges in one step.                                   | 0.05 - 0.8                             | 0.4     |
|              | ShrinkRayTargetHighlighting | Defines, when a target gets highlighted. Set to OnLoading on performance issues.          | Off, OnHit, OnLoading                  | OnHit   |
|              | MaximumPlayerSize           | Defines, how tall a player can become (1.7 is the maximum for the ship inside and doors!) | 1 - 10                                 | 1.7     |
| Shrunken     | MovementSpeedMultiplier     | Speed multiplier for shrunken players                                                     | 0.5 (slow) - 1.5 (fast)                | 1.3     |
|              | JumpHeightMultiplier        | Jump-height multiplier for shrunken players                                               | 0.5 (lower) - 2 (higher)               | 1.3     |
|              | WeightMultiplier            | Weight multiplier applied on held items of shrunken players                               | 0.5 (lighter) - 2 (heavier)            | 1.5     |
|              | CanUseVents                 | If true, shrunken players can teleport between vents.                                     | true / false                           | true    |
|              | PitchDistortionIntensity \* | Intensity of the pitch distortion for shrunken players.                                   | 0 (normal voice) - 0.5 (high pitched)  | 0.3     |
|              | CanEscapeGrab               | If true, a player who got grabbed can escape by jumping.                                  | true / false                           | true    |
| Interactions | JumpOnShrunkenPlayers       | If true, larger players can harm smaller ones by jumping on them.               | true / false                           | true    |
|              | ThrowablePlayers            | If true, shrunken players can be thrown by their holder.                          | true / false                           | true    |
|              | FriendlyFlight              | If true, held players can grab their holder, causing comedic, but game breaking effects. | true / false                           | false   |
| Enemies      | HoarderBugBehaviour         | Defines if hoarding bugs should be able to grab you and how likely that is                | Default, NoGrab, Addicted              | Default |
|              | ThumperBehaviour            | Defines the way Thumpers react on shrunken players.	                                     | Default, One-Shot, Bumper              | Bumper  |
> client-sided options are marked with a \*, others will by synced with the host.

## Known bugs ##
+ Shrinkray floats above the ground when dropped
+ Collision detection when holding players not accurate enough

## Planned features ##
- Animations for holding / throwing players
- Make sandworms unlikely targeting shrunken players

## Got more ideas? ##
Send us a message on discord
+ big_nemo
+ niro1996

or write it in our threads on the unofficial lethal companys mod discords:
[Unofficial Modding Discord](https://discord.gg/nYcQFEpXfU) \| [LittleCompany-Thread](https://discord.com/channels/1169792572382773318/1190100786357743646)
[Modding Discord](https://discord.gg/nYcQFEpXfU) \| [LittleCompany-Thread](https://discord.com/channels/1168655651455639582/1206337352608256010)

## Special Thanks To
[AboveFire](https://github.com/AboveFire) & [MissScarlett](https://github.com/QueenScarlett23) for development help!
[@peppermint_2859](https://twitter.com/ItsJOEYthe) on discord for the ShrinkRay model!
[Sakiskid](https://github.com/Sakiskid) for the ShrinkRay beam!
Ellethwen on discord for the potion & player item icons!
Spinmaster on discord for creating all the sounds this mod includes!


## And the testers too (sorry for making you join and leave the lobby a billion times)!
[StrawberriStorm](https://twitter.com/strawberristorm) | [Tcorn](https://twitter.com/TcorntheLazy) | [Sirdog](https://youtu.be/6ItPIiegBms?si=zH-Cf467VIOtVTMt) | [IceSigil](https://twitter.com/IceSigil) | [NimNom](https://www.twitch.tv/nimnom)

