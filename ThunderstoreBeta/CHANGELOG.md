## BETA ##
**What's new**
+

**Fixes**
+

### 0.5.2 ###
+ Added a new config option "CantOpenStorageCloset" in section "Shrunken"
*This will prevent shrunken players from opening and closing the storage closet door.. leaving those stuck, that weren't a great asset..*

### 0.5.1 ###
+ fixed shrinking/enlarging not working when hitting exactly 0.2 or MaximumPlayerSize
+ added new config options to description

## 0.5.0 [ShrinkRay targeting] ##
**What's new**
+ ShrinkRay Targeting is finally here! The holder will see a laser pointer and targets will get highlighted (on activate by default, configurable).
+ 4 new config options:
 + deathShrinking: Same as multipleShrinking was before. Allows shrinking players to death if set.
 + shrinkRayTargetHighlighting: A performance option that manages, when targets should be highlighted.
 + sizeChangeStep: The size change of a single modification *Enlarging has no benefits currently and won't make normal sized players grabbable. This is planned for 0.6.0!*
 + maxPlayerSize: Defines how tall a player can become. 1.7 is the default and also the highest value for standing inside the ship and going through doors.
+ Smoke VFX when being shrunken to death

**Fixes**
+ fixed players not getting normal again after rejoining (till reworked in 0.6.0)
+ fixed audio error leading to bugged interaction state
+ fixed host being able to grab themself
+ fixed goomba not working when jumping over a shrunken player
+ fixed weightMultiplier not being applied
+ fixed grabbed items not being scaled correctly
+ fixed bug where item became glassified on non-holders
+ added fallback for grabbablePlayerObject not being loaded correctly and spamming the log

### 0.4.6 ###
+ Fixed a crash when grabbing something

### 0.4.5 ###
+ replacing materials instead of sharedMaterials, to only modify the specific item, instead of any of their type

### 0.4.4 ###
+ fixed desync for ShrinkRay when trying to shrink the holder as the grabbed player

### 0.4.3 ###
+ shrunken players will now see the terminal screen upon usage

### 0.4.2 ###
+ weight-updating is now called on demand, instead of checking for updates every frame

### 0.4.1 ###
+ using main folder as fallback if there is no audio / icons folder (e.g. when downloaded via thunderstore)

### 0.4.0 [Singleplayer & Sounds update] ###
**What's new**
+ Added two potions: Light & Heavy
  + Buyable in store including terminal notes & available as rare scrap in the dungeons!
  + Fully configurable in the config after first startup.
  + Once consumed it will stay as an empty potion with a maximum scrap value of 5
  + *This was wished by the community as a way to shrink yourself in singleplayer, as the shrink ray can only target others. Enjoy a drink!*
+ Added sounds for the following interactions:
  + Grabbing, dropping and throwing players
  + Grabbing, dropping and consuming potions
  + Shrink ray beam
  + Death through shrinking
+ Added loading / unloading phase for ShrinkRay
+ Added ScanNodes for potions, shrink ray & grabbable players (company-moon-only)
+ Added effect for hitting shrunken players with a shovel

**Tweaks**
+ multipleShrinking is now disabled by default (until targeting for the ShrinkRay is reworked)
+ ShrinkRay will focus the closest possible target (until targeting for the ShrinkRay is reworked)
+ slightly increased default MovementSpeedMultiplier
+ changed ThumperBehaviour to Bumper to make the default gameplay experience more spicy

**Fixes**
+ fixed "Grab" trigger blocking view while doing third person emotes
+ fixed being unable to grab anything after grabbed player grew to normal size or died
+ fixed player visor being in the view after revive
+ fixed weight of grabbed player not updating when they grab/discard items
+ fixed players giving 0 credits when being sold on the counter
+ fixed items not aligning correctly in hand after being held while changing player size

### 0.3.1 ###
+ fixed players being unable to grab anything when holding a player who died or became ungrabbable in the meantime

### 0.3.0 [Yippie] ###
**What's new?**
+ Shrunken players are now grabbable by Hoarding Bugs! There are 3 modes available:
  + **Default:** Hoarding bugs grab you occasionally, like any other item.
  + **NoGrab:** Hoarding bugs treat you as a normal player.
  + **Addicted:** Hoarding bugs looove to grab you, more than anything else! Yippie!! <3 (They won't let you go, if they have the chance to)
+ Icon for grabbed players (Special thanks to Ellethwen [swubbelbubbel]!)

**Fixes**
+ fixed lighting & weather not updating for person who didn't actively joined the dungeon (e.g when being held)
+ fixed fall damage applying in relation to the position a player was grabbed, rather than the position they got dropped
+ fixed a bug where shrinking someone to death while they got a centipede on their head caused a lot of errors
+ fixed a bug where host was seen as grabbable internally from the start of the game

### 0.2.1 ###
+ Added new ThumperBehaviour: Bumper -> With their immense power, thumpers are unable to grab shrunken players. Instead they will send them flying on contact!
+ Fixed bug where ThumperBehaviour.OneShot was throwing errors in multiplayer

### 0.2.0 [Stability update] ###
+ Major GrabbablePlayerList overhaul (simplified & removed RPC calls as they weren't needed)
+ DeskPatch (Playerselling) overhaul (simplified & bugfixing)
+ Fixed infinite shrink/enlarge bug at end of round if player died from shrinking or was sold
+ Fixed player pitch not working (hopefully, worked in LAN)
+ Changed project name from LCShrinkRay.BETA to LittleCompany.BETA. **The config file will reset** due to the folder-change!

**What's new?**
+ GrabbablePlayerObjects are now persistent through rounds
+ Shrunken players are now throwable

### 0.1.3 ###
+ Fixed bugging through ground on modded dungeons (tested with [ScoopysVarietyMod](https://thunderstore.io/c/lethal-company/p/scoopy/Scoopys_Variety_Mod)) upon entering as shrunken player

### 0.1.2 ###
+ Grabbed players are now unable to shrink/enlarge the player holding them

### 0.1.1 ###
+ Fixed a bug where jump height & speed multiplier not resetting upon enlarging

### 0.1.0 ###
+ First beta version