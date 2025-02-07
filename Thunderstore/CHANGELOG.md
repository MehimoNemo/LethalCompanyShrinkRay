### 1.3.18 ###
**New**
- Balanced the item scaling value change to make it less strong and also fixed bugs with it.
- Added many new configs to tweak the item scaling values in order to allow more balancing within a modpack.

**Fixes**
- Fixed item value scaling not working properly.

### 1.3.17 ###
**Fixes**
- Fixed beltbag not scaling correctly. It's still not perfect when huge, but it's way better.

### 1.3.16 ###
**Fixes**
- Removed obsolete LCOffice compatibility patch

### 1.3.15 ###
**New**
- Experimental config option "RemoveMinimumSizeLimit" to change the minimum size of enemies and players before dieing from 0.2 to 0.05
- This may cause bugs!

### 1.3.14 ###
**Fixes**
- Fix compatibility issue with v60/v61

### 1.3.13 ###
**Tweaks**
- Added a setting to disable the automatic size adjustment when in the cruiser. If disabled, players won't be able to enter cruisers smaller than them.

### 1.3.12 ###
**Tweaks**
- Added compatibility for MoreCompany cosmetics on ModelReplacementApi models

**Fixes**
- Fix size desynch when exiting the cruiser. (fr fr this time)

### 1.3.11 ###
**Fixes**
- Fix log spam when playing with more than 4 players with MoreCompany
- Fix size desynch when exiting the cruiser. (for real this time)

### 1.3.10 ###
**Fixes**
- Fix size regulation when exiting the cruiser.

### 1.3.9 ###
**Fixes**
- Fix compatibility issues with mod SCP956
- Exiting the cruiser scales you back to the correct size more reliably now.

### 1.3.8 ###
**Tweaks**
- New config for disabling the glassification of items that are too big and doesnt allow you to see.
- Tweaks to the small cruiser so it drives a bit better. (Still very hard to drive)

**Fixes**
- Fixed models from ModelReplacementApi not being resized when shrinking/growing masked people wearing them due to Mirage
- Exiting the cruiser scales you back to the correct size more reliably now.
- Fix error with scaled reserved items (ReservedSlot) on masked people using Mirage.
- Attempt to fix rare error caused by Forest Giants AI when seeing small players.

### 1.3.7 ###
**What's new**
- Scaling the company cruiser is now possible!

**Tweaks**
- New config for adjusting the speed at which the items grow when shot with the shrink ray gun (ItemSizeChangeSpeed)

**Fixes**
- Fixed models from ModelReplacementApi being off when appearing on masked enemies of Mirage mod if you're resized.
- Attempt fixing weird weight issues when resizing items
- Fix error log for updating pitch of players in groups of more than 4 using MoreCompany

### 1.3.6 ###
**Fixes**
- Fixed exception when spawning BurningRobotToy
- Fixed exception when scaling player that uses the terminal
- Fixed enemy not despawning if death shrink event didn't occure

### 1.3.5 ###
**Fixes**
- Fixed typo causing previously implemented config option to not always be counted correctly

### 1.3.4 ###
**New**
- Added *[Enemies]deathShrinkEventChance* config option to define to how likely it is for an enemy to cause an event when shrunken to death

**Fixes**
- Fixed a bug where throwing players that are in the Goomba animation (e.g. after shovel hit) leads to the player being stuck
- Weight now can't go below 0. This doesn't fix the -2b lb bug, but limits its effects on the gameplay

### 1.3.3 ###
**Fixes**
- Fixed clipping through ground after using scaled terminal
- Fixed error message when shooting at landmines

### 1.3.2 ###
**Fixes**
- Fallback for ShrinkRay being unusable after exception / error.

### 1.3.1 ###
**Fixes**
- Attempt to fix ShrinkRay sometimes being unusable when reloading a savefile

## 1.3.0 ##
**What's new**
- Scaling ship items!
  - You can now scale ship items that are placed
  - The size step change depends on *[Sizing]ShipItemSizeChangeStep*. Set it to 0 to disable this feature
  - If ship objects reach size 0, they will be automatically sent to the storage and scaled back to the previous size

**Tweaks**
- Larger shotguns now have a recoil (relative to player size)
- Changed *[Sizing]ItemSizeChangeStep* from 0.5 to 0.4, as it's likelier that players don't want to make them disappear in 2 steps

### 1.2.1 ###
**Fixes**
- Fixed pitch warnings

## 1.2.0 ##
**What's new**
- Added *[Sizing]PlayerSizeStopAtDefault*, which scales to PlayerDefaultSize, if a scaling step would go over it

**Compatibility**
- Increased ModelReplacementAPI compatibility (e.g. LethalCreatures)

**Tweaks**
- Spray paint size is now relative to the can size
- Shotgun can only be reloaded with equally scaled ammo
- Shotgun damage relative to scale
- Players can now scale to the MaximumPlayerSize, even if they would usually scale to a higher value
- Fixed inventory bug when holding an item that gets shrunken to size 0 and disappears
- Fixed ShrinkRay target circle staying upon death
- ShrinkRay SizeStepChange is now relative to its scale

**Fixes**
- Hide hologram while item is pocketed
- Fixed item scales not being saved through rounds
- Laser pointer is no longer affected by FLashlight scaling changes. Will receive its own changes later on
- Fixed exception being thrown when scaling items without an item event handler

### 1.1.1 ###
**Fixes**
- If ShrinkRayShotsPerCharge are set to 0, the ShrinkRay can no longer overheat

## 1.1.0 ##
**What's new**
- Added *[General]LogicalMultiplier*
  - Sets the jump height & movement speed to logical values.
  - If set, JumpHeightMultiplier & SpeedMultiplier will be overwritten.

**Tweaks**
- The ShrinkRay now requires battery to run
  - Configurable through *[General]ShrinkRayShotsPerCharge* in the config.
  - "Shots left" are saved through rounds
  - Setting this option to 0 will disable the battery usage.
  - Added *[General]ShrinkRayNoRecharge*. If set, the ShrinkRay will overheat when the battery is used up and can't be recharged.
- Scaling keys will make them useless.. just don't.
- Adjusted pitch for scaled Airhorn & ClownHorn
- Removed *[General]ShrinkRayTargetHighlighting*, as OnHit didn't have a noticable performance impact
- Player-scaling from consuming potions now depends on the potion size (simple multiplication of potion size with *PlayerSizeChangeStep*)

**Fixes**
- Fixed a bug when scaling objects without a spawnPrefab
- Fixed size desync for players who are getting scaled while using the terminal. They will now automatically quit the terminal when scaled.
- Fixed scaled items disappearing when reloading a savefile
- Fixed Potions being consumable again after reloading a savefile

### 1.0.13 ###
**Fixes**
- Fixed item scaling effects affecting any item of the same type during the first milliseconds of scaling an item

**Tweaks**
- Enlarged flashlights now cover a larger radius
- Reworked battery cost calculation for scaled flashlights
- Hits from enlarged shovels now take significantly more damage

### 1.0.12 ###
**Compatibility**
- LC_Office item-scaling compatibility

**Tweaks**
- Flashlight intensity & battery usage are now in relation to the item size

### 1.0.11 ###
**Fixes**
- Fixed giftbox presents causing errors and being invisible
- Fixed error after dropped by a hoarding bug
- Fixed error when dropping hoarding bugs target item right when it gets angry

**Tweaks**
- GiftBox presents will now have the same relative scale as the gift box itself

### 1.0.10 ###
**Tweaks**
Reworked HoardingBug behaviour
- being small isn't a greencard for not being targetable anymore (they followed you like pets, unable to get their desired item which a smaller player held)
- hoarding bugs will grab players smaller than them on collision
- enlarging a hoarding bug will make any smaller players immediately grabbable
- they're not fully muted anymore.. yippie!

**Compatibility**
- Added config option *[Experimental]UseLethalLevelLoaderForItemRegistration* as a way to disable usage of LethalLevelLoader, which caused bugs when loading items.

### 1.0.9 ###
**Fixes**
- Fixed: Putting speed/jump multiplier at 1 won't reset them back to original values (for mod compatibility purposes)

### 1.0.8 ###
**Fixes**
- Fixed error log messages for enemies with creatureVoice but no creatureSFX audio
- Fixed scaling of items not working if resized by another mod or before joining
- DocileLocustBees (Harmless roaming ones) can be scaled now.. even though it still looks a bit weird

### 1.0.7 ###
**Compatibility**
- [LethalVRM](https://thunderstore.io/c/lethal-company/p/Ooseykins/LethalVRM/) support
- Putting speed/jump multiplier at 1 won't modify the original values

**Tweaks**
- EnemyPitchDistortionIntensity now affects audioSources other than creatureVoice

**Fixes**
- Fixed turrets being able to shoot through walls
- Fixed turrets shooting too high for scaled players

### 1.0.6 ###
**Fixes**
- Fixed DefaultPlayerSize not being synced in time between host and client

### 1.0.5 ###
**Tweaks**
- Items below a size of 0.2 can now exist. Only items with a size of 0 will disappear

**Fixes**
- Attempt to fix OverflowException when sending config options from host to clients
- Fixed goomba animation stacking when hit multiple times by a shovel
- Fixed goomba animation starting at full size, regardless of player size
- Added cooldown for grabbing the same player again, to counter a bug where re-grabbing a player too fast is rarely causing a bugged interact state

### 1.0.4 ###
**Tweaks**
- Additional option *[Enemies]EnemyPitchDistortionIntensity* to change voice pitch of enemies, relative to their scale and the local player scale

### 1.0.3 ###
**Fixes**
- Fixed turrets being unable to see shrunken players
- Fixed some items (like extension ladder) causing log errors when shrunken to zero

### 1.0.2 ###
**Fixes**
- Fixed glassification not working
- Fixed cases where MoreCompany cosmetics didnt scale accordingly

### 1.0.1 ###
**What's new**
- Added "ItemScalingVisualOnly" option. If true, scaling items doesn't change their weight & value.

**Tweaks**
- Removed upper limit for "MaxPlayerSize". Gogo giants!

**Fixes**
- Attempt to fix a bug where escaping from a grab could result in a bugged item-interact state for the holder

# 1.0.0 #
**What's new**
- Added "DefaultPlayerSize" config option. Players start with this size.
- Players can now only grab smaller players. This includes enlarged players grabbing normal (or any smaller) sized ones!
- Enemy scaling!
 - Configurable
 - Enemies can be shrunken to death.. but be warned.. there might be consequences.
 - Hoarding bugs can only grab smaller sized players. *Maybe it's not wise to enlarge them..*
- Item scaling!
 - Configurable
 - Scaling items will affect their weight and scrap value.
 - Shrinking items will be instantly, while enlarging items will take effect over time. *To prevent people from only using this in front of the sell counter, while not fully blocking it*
 - The limit for scrap value is x2, reached at x10 size. Size however isn't limited, but scaling will slow down the larger the item gets
- Support for LethalLevelLoader
- Our own [Wiki](https://github.com/MehimoNemo/LethalCompanyShrinkRay/wiki)!

**Tweaks**
- Throw force of grabbable players is now dependant on the size difference. *Anything above 0.66 in difference is stronger than before.*
- Glassification of held items is now based on their screen position (center = glassify), instead of being only applied on two-handed items
- Config folder is now called Toybox.LittleCompany without the additional "BETA" inside the name. **Old config settings will be lost with this version!**

**Fixes**
- Fixed arms and visor being misplaced or having wrong offset after being scaled
- Fixed holding players at gameOver animation causing the held player to glue at the holder without being held
- Fixed any ShrinkRay triggering when one is shoot in enlarging mode
- Adjusted quicksand sinking depth relative to player size
- Fixed players being grabbable while being scaled
- Fixed bought potions being sellable

### 0.5.8 ###
- fixed a bug where vents couldn't be found if a previous error occured, leading to being unable to exit the lobby

### 0.5.7 ###
- fixed resizing not working correctly on players who were small before joining
- updated recommended LethalLib dependency version to 0.15.1 for v50 support
- Fixed arm offset when resized
- Lethal Phone compatibility
- ModelReplacementApi compatibility

### 0.5.6 ###
- fixed sometimes falling through ground after using vents

### 0.5.5 ###
- compatibility for v50 public beta
- fixed terminal window not visible for enlarged players (at the cost of not moving the player up/down from the perspective of others. will get a full rework later on)

### 0.5.4 ###
- fixed desync from taking potion while getting hit by ShrinkRay
- fixed clipping through ground sometimes when taking vents while being tiny
- fixed being grabbable while climbing ladders, leading to weird behaviours. let them climb in peace c:
- attempt to fix "Ungrab : [E]" being displayed while not grabbed
- attempt to fix increased movement speed & jump height bug, even while being normal sized
- fixed Potions & ShrinkRay disappearing on reloading a savefile

### 0.5.3 ###
- fixed audio error leading to bugged interaction state (previous fix was a workaround, this fixes the cause)

### 0.5.2 ###
- Added a new config option "CantOpenStorageCloset" in section "Shrunken"
*This will prevent shrunken players from opening and closing the storage closet door.. leaving those stuck, that weren't a great asset..*

### 0.5.1 ###
- fixed shrinking/enlarging not working when hitting exactly 0.2 or MaximumPlayerSize
- added new config options to description

## 0.5.0 [ShrinkRay targeting] ##
**What's new**
- ShrinkRay Targeting is finally here! The holder will see a laser pointer and targets will get highlighted (on activate by default, configurable).
- 4 new config options:
 - DeathShrinking: Same as multipleShrinking was before. Allows shrinking players to death if set. Players below 0.2 in size will die.
 - ShrinkRayTargetHighlighting: A performance option that manages, when targets should be highlighted.
 - SizeChangeStep: The size change of a single modification *Enlarging has no benefits currently and won't make normal sized players grabbable. This is planned for 0.6.0!*
 - MaxPlayerSize: Defines how tall a player can become. 1.7 is the default and also the highest value for standing inside the ship and going through doors.
- Smoke VFX when being shrunken to death

**Fixes**
- fixed players not getting normal again after rejoining (till reworked in 0.6.0)
- fixed audio error leading to bugged interaction state
- fixed host being able to grab themself
- fixed goomba not working when jumping over a shrunken player
- fixed weightMultiplier not being applied
- fixed grabbed items not being scaled correctly
- fixed bug where item became glassified on non-holders
- added fallback for grabbablePlayerObject not being loaded correctly and spamming the log

### 0.4.6 ###
- Fixed a crash when grabbing something

### 0.4.5 ###
- replacing materials instead of sharedMaterials, to only modify the specific item, instead of any of their type

### 0.4.4 ###
- fixed desync for ShrinkRay when trying to shrink the holder as the grabbed player

### 0.4.3 ###
- shrunken players will now see the terminal screen upon usage

### 0.4.2 ###
- weight-updating is now called on demand, instead of checking for updates every frame

### 0.4.1 ###
- using main folder as fallback if there is no audio / icons folder (e.g. when downloaded via thunderstore)

## 0.4.0 [Singleplayer & Sounds update] ##
**What's new**
- Added two potions: Light & Heavy
  - Buyable in store including terminal notes & available as rare scrap in the dungeons!
  - Fully configurable in the config after first startup.
  - Once consumed it will stay as an empty potion with a maximum scrap value of 5
  - *This was wished by the community as a way to shrink yourself in singleplayer, as the shrink ray can only target others. Enjoy a drink!*
- Added sounds for the following interactions:
  - Grabbing, dropping and throwing players
  - Grabbing, dropping and consuming potions
  - Shrink ray beam
  - Death through shrinking
- Added loading / unloading phase for ShrinkRay
- Added ScanNodes for potions, shrink ray & grabbable players (company-moon-only)
- Added effect for hitting shrunken players with a shovel

**Tweaks**
- multipleShrinking is now disabled by default (until targeting for the ShrinkRay is reworked)
- ShrinkRay will focus the closest possible target (until targeting for the ShrinkRay is reworked)
- slightly increased default MovementSpeedMultiplier
- changed ThumperBehaviour to Bumper to make the default gameplay experience more spicy

**Fixes**
- fixed "Grab" trigger blocking view while doing third person emotes
- fixed being unable to grab anything after grabbed player grew to normal size or died
- fixed player visor being in the view after revive
- fixed weight of grabbed player not updating when they grab/discard items
- fixed players giving 0 credits when being sold on the counter
- fixed items not aligning correctly in hand after being held while changing player size

### 0.3.1 ###
- fixed players being unable to grab anything when holding a player who died or became ungrabbable in the meantime

## 0.3.0 [Yippie] ##
**What's new?**
- Shrunken players are now grabbable by Hoarding Bugs! There are 3 modes available:
  - **Default:** Hoarding bugs grab you occasionally, like any other item.
  - **NoGrab:** Hoarding bugs treat you as a normal player.
  - **Addicted:** Hoarding bugs looove to grab you, more than anything else! Yippie!! <3 (They won't let you go, if they have the chance to)
- Icon for grabbed players (Special thanks to Ellethwen [swubbelbubbel]!)

**Fixes**
- fixed lighting & weather not updating for person who didn't actively joined the dungeon (e.g when being held)
- fixed fall damage applying in relation to the position a player was grabbed, rather than the position they got dropped
- fixed a bug where shrinking someone to death while they got a centipede on their head caused a lot of errors
- fixed a bug where host was seen as grabbable internally from the start of the game

### 0.2.1 ###
- Added new ThumperBehaviour: Bumper -> With their immense power, thumpers are unable to grab shrunken players. Instead they will send them flying on contact!
- Fixed bug where ThumperBehaviour.OneShot was throwing errors in multiplayer

## 0.2.0 [Stability update] ##
- Major GrabbablePlayerList overhaul (simplified & removed RPC calls as they weren't needed)
- DeskPatch (Playerselling) overhaul (simplified & bugfixing)
- Fixed infinite shrink/enlarge bug at end of round if player died from shrinking or was sold
- Fixed player pitch not working (hopefully, worked in LAN)
- Changed project name from LCShrinkRay.BETA to LittleCompany.BETA. **The config file will reset** due to the folder-change!

**What's new?**
- GrabbablePlayerObjects are now persistent through rounds
- Shrunken players are now throwable

### 0.1.3 ###
- Fixed bugging through ground on modded dungeons (tested with [ScoopysVarietyMod](https://thunderstore.io/c/lethal-company/p/scoopy/Scoopys_Variety_Mod)) upon entering as shrunken player

### 0.1.2 ###
- Grabbed players are now unable to shrink/enlarge the player holding them

### 0.1.1 ###
- Fixed a bug where jump height & speed multiplier not resetting upon enlarging

### 0.1.0 ###
- First beta version