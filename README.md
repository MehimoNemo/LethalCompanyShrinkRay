# Honey, I shrunk the employees! #

In this more than cosmetic Lethal Company mod, players can experience the game from a new perspective. The pros and cons of being small are many, and unique interactions are yet to be discovered...

## Configuration ##
|  Group       |          Option             |                           Description                                                     | Possible values                        | Default |
| ------------ | --------------------------- | ----------------------------------------------------------------------------------------- | -------------------------------------- | ------- |
| General      | ShrinkRayCost               | Store cost of the shrink ray                                                              | 0                                      | 0       |
|              | MultipleShrinking           | If true, a player can shrink multiple times.. unfortunatly.                               | false                                  | false   |
| Shrunken     | MovementSpeedMultiplier     | Speed multiplier for shrunken players                                                     | 0.5 (slow) to 2 (fast)                 | 1.2     |
|              | JumpHeightMultiplier        | Jump-height multiplier for shrunken players                                               | 0.5 (lower) to 2 (higher)              | 1.5     |
|              | WeightMultiplier            | Weight multiplier applied on held items of shrunken players                               | 0.5 (lighter) to 2 (heavier)           | 1.5     |
|              | CanUseVents                 | If true, shrunken players can teleport between vents.                                     | true                                   | true    |
|              | PitchDistortionIntensity \* | Intensity of the pitch distortion for shrunken players.                                   | 0 (normal voice) to 0.5 (high pitched) | 0.3     |
|              | CanEscapeGrab               | If true, a player who got grabbed can escape by jumping.                                  | true                                   | true    |
| Interactions | JumpOnShrunkenPlayers       | If true, normal-sized players can harm shrunken players by jumping on them.               | true                                   | true    |
|              | ThrowablePlayers            | If true, shrunken players can be thrown by normal sized players.                          | true                                   | true    |
|              | FriendlyFlight              | If true, held players can grab other players, causing comedic, but game breaking effects. | false                                  | false   |
| Enemies      | HoardingBugSteal            | If true, hoarding/loot bugs can treat a shrunken player like an item.                     | true                                   | true    |
|              | ThumperBehaviour            | Defines the way Thumpers react on shrunken players.	                                     | Default, One-Shot, Bumper              | Default |
> client-sided options are marked with a \*, others will by synced with the host.

## Known bugs ##
+ Text "Ungrab: JUMP" not getting shown when held player holds an item
+ Shrunken players sometimes face the wrong direction when climbing ladders
+ HoardingBug not consistently grabbing players
+ Shrinkray floats above the ground when dropped
+ Weight of held players not updating
+ Jetpack offset for shrunken players
+ Clients are sometimes unable to shrinking host when shooting
+ Ray not always showing for other players
+ Collision detection when holding players not accurate enough
+ Held players mask (visor) is in the view


## Planned features ##
- [ ] Throw players further
- [ ] Animations for holding / throwing players
- [ ] Add cooler ray / improved sfx & fx
- [ ] Item slot icon for held player
- [ ] Sellable players
- [ ] Size resetting / Unshrinking
- [ ] Make sandworms unlikely targeting shrunken players

## Got more ideas? ##
Send us a message on discord
+ big_nemo
+ niro1996
+ sakiskid

or write it in our thread on the unofficial lethal companys discord: [Server-Invite](https://discord.gg/nYcQFEpXfU) \| [LittleCompany-Thread](https://discord.com/channels/1169792572382773318/1190100786357743646)

## Special Thanks To
[Niro](https://github.com/NiroDev) for development help 

[@peppermint_2859](https://twitter.com/ItsJOEYthe) on discord for modeling!


## And the testers too (sorry for making you join and leave the lobby a billion times)!
[StrawberriStorm](https://twitter.com/strawberristorm)

[Tcorn](https://twitter.com/TcorntheLazy)

[Sirdog](https://youtu.be/6ItPIiegBms?si=zH-Cf467VIOtVTMt)

[IceSigil](https://twitter.com/IceSigil)

