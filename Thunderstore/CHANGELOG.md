### 0.4.0 [Singleplayer & Sounds update] ###
**What's new**
+ Added two potions: Light & Heavy
  + Buyable in store including terminal notes & available as rare scrap in the dungeons!
  + Fully configurable in the config after first startup.
  + *This was wished by the community as a way to shrink yourself in singleplayer, as the shrink ray can only target others. Enjoy a drink!*
+ Added sounds for the following interactions:
  + Grabbing, dropping and throwing players
  + Grabbing, dropping and consuming potions
  + Shrink ray beam
  + Death through shrinking
+ Added loading / unloading phase for ShrinkRay
+ Added ScanNodes for potions, shrink ray & grabbable players (company-moon-only)

**Tweaks**
+ multipleShrinking is now disabled by default, until targeting for the ShrinkRay is reworked
+ slightly increased default MovementSpeedMultiplier
+ changed ThumperBehaviour to Bumper to make the default gameplay experience more spicy

**Fixes**
+ fixed "Grab" trigger blocking view while doing third person emotes
+ fixed being unable to grab anything after grabbed player grew to normal size or died
+ fixed player visor being in the view after revive
+ fixed weight of grabbed player not updating when they grab/discard items
+ fixed players giving 0 credits when being sold on the counter

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