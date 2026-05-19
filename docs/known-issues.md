# Known Issues

## Priest wand `Shoot` cancels in melee range (1.14.x client)

On modern 1.14.x Classic clients the `autoRangedCombat` CVar (default ON) treats wands as ranged weapons and auto-cancels `Shoot` the moment a mob enters melee range, then switches you into auto-attack. Vanilla 1.12 emulators (VMaNGOS, Kronos, CMaNGOS) never expected this — the wand simply dies, you can't finish the mob with it, and you get stuck swinging.

**Workaround — run once in chat:**
```
/console autoRangedCombat 0
```
Or make it persistent by adding this line to `WTF/Config.wtf` before launch:
```
SET autoRangedCombat "0"
```

Priest characters logging in on 1.14+ Classic Era clients receive a one-time chat reminder from the proxy on world-enter. Other classes that occasionally use a wand are affected the same way — apply the same CVar fix if you notice it. Tracked in [#80](https://github.com/Xian55/HermesProxy/issues/80).
