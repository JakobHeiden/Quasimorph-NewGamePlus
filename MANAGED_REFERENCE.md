# Quasimorph `Managed/` folder — reference notes

Path (verified, 121 items total):

```
C:\Program Files (x86)\Steam\steamapps\common\Quasimorph\Quasimorph_Data\Managed\
```

This is the assembly source for `NewGamePlus.csproj` (`$(ManagedPath)`).

## Game code

| File | Notes |
|--|--|
| `Assembly-CSharp.dll` | The game. Dated **22.08.2026**; most other files are 06.06.2026, so this has been patched more recently. **Decompile against this build**, not older snippets. |
| `Assembly-CSharp-firstpass.dll` | Unity's `Plugins/` bucket, 36 KB. Probably irrelevant, but it exists. |

## Shipped by the game — reference, don't bundle

These are already present at runtime. Reference them from `Managed/` with
`Private=false`; do not ship your own copy.

- `0Harmony.dll` — Harmony runtime patching. **Do NOT add the NuGet package or ship your own copy.**
- `Newtonsoft.Json.dll` — for `config_*` JSON work.
- `SimpleJSON.dll` — second JSON library, also present.
- `com.rlabrecque.steamworks.net.dll` — Steam API, only for Workshop-facing code.

## Base class library (Mono, not the SDK's reference pack)

`mscorlib.dll`, `netstandard.dll`, `System.dll`, `System.Core.dll`,
`System.IO.Compression.dll`, `System.Xml.dll`, `System.Data.dll`, and the usual
`System.*` facades.

`System.IO.Compression.dll` specifically: referencing the game's copy resolves
the outstanding **MSB3277** conflict — the SDK reference pack has 4.1.3.0, the
game has 4.2.0.0.

## Engine and third-party — reference only if the code touches them

All `Unity.*` and `UnityEngine.*` modules, plus:

- `DOTween.dll` — animation
- `Rewired_Core.dll` / `Rewired_Windows.dll` — input
- `Cinemachine.dll` — camera
- `Mono.Security.dll`

## Suggested starting reference set

All from this folder, all with `Private=false`, and with
`DisableImplicitFrameworkReferences=true`:

```
mscorlib
System
System.Core
netstandard
System.IO.Compression
Assembly-CSharp
0Harmony
```

Add `UnityEngine.CoreModule` and `Newtonsoft.Json` when the code needs them.
