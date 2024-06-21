# What's new in this beta
- Scaling ship items!
  - You can now scale ship items that are placed
  - The size step change depends on *[Sizing]ShipItemSizeChangeStep*. Set it to 0 to disable this feature
  - If ship objects reach size 0, they will be automatically sent to the storage and scaled back to the previous size

**Tweaks**
- Larger shotguns now have a recoil (relative to player size)
- Changed *[Sizing]ItemSizeChangeStep* from 0.5 to 0.4, as it's likelier that players don't want to make them disappear in 2 steps
