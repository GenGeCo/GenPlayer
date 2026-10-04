# PhoneVolume

**Control your Android phone's media volume from the Windows taskbar.**

A tiny companion tool for GenPlayer (and any other music app): your phone is plugged into the PC via USB, the music plays from the phone, and you adjust the volume from an icon next to the Windows clock — without touching the phone.

- One click on the tray icon → a slider like the Windows volume one
- Mouse wheel / arrow keys: **one volume step per notch**, exactly like the phone's hardware buttons
- **Mute** button that remembers the previous level
- The icon shows the current level; it turns grey when the phone is disconnected
- Works with **any Android phone** (adapts to 15, 25, 30… volume steps)
- Single ~35 KB `.exe`, no installation, no internet, no tracking

---

## Download

**[⬇ PhoneVolume.exe (v1.0.0)](https://github.com/GenGeCo/GenPlayer/raw/main/PhoneVolume/bin/PhoneVolume.exe)** — Windows 10 / 11

> Windows SmartScreen may warn you because the file is not digitally signed: click **More info → Run anyway**. The full source code is in this folder if you prefer to build it yourself.

---

## Requirements

1. **Windows 10 or 11** (uses the .NET Framework already included in Windows)
2. **ADB** — Google's free *Android SDK Platform-Tools*: [download here](https://developer.android.com/tools/releases/platform-tools). Unzip it anywhere.
3. **USB debugging** enabled on the phone

### Enable USB debugging (one time only)

1. On the phone: **Settings → About phone → Software information**
2. Tap **Build number** 7 times → "Developer mode enabled"
3. **Settings → Developer options → USB debugging** → ON
4. Plug the phone into the PC. On the phone, accept **"Allow USB debugging?"** and tick **"Always allow from this computer"**

---

## How to use

1. Run `PhoneVolume.exe`. A phone icon appears in the system tray (bottom-right).
2. If you can't see it, click the **^** arrow near the clock and **drag the icon onto the taskbar** so it is always visible.
3. **Left click** → volume panel  ·  **Right click** → menu

| Menu item | What it does |
|---|---|
| **Phone** | Choose the phone when more than one is connected |
| **Start with Windows** | Launch PhoneVolume automatically at login |
| **Choose adb.exe…** | Point to `adb.exe` manually if it wasn't found automatically |
| **Exit** | Close the program |

### Where it looks for adb.exe

In this order: the same folder as `PhoneVolume.exe` (or a `platform-tools` subfolder), `ANDROID_HOME` / `ANDROID_SDK_ROOT`, the `PATH`, `%LOCALAPPDATA%\Android\Sdk`, `C:\` / `D:\` / `E:\` `\platform-tools`, `\AndroidSdk`, `\Android\Sdk`.
The easiest option: **put `PhoneVolume.exe` inside the `platform-tools` folder**.

---

## How it works

PhoneVolume talks to the phone through ADB:

- main method: `cmd media_session volume --stream 3 --set N` (sets the exact level and reads the maximum)
- fallback for phones that block it: reads the level from `dumpsys audio` and simulates the volume keys

It never restarts or stops the ADB server, so it doesn't interfere with Android Studio or other tools using ADB at the same time. Settings are saved in `%APPDATA%\PhoneVolume`.

## Build from source

No Visual Studio needed — Windows already includes the C# compiler:

```
build.cmd
```

Files: `PhoneVolume.cs` (all the code), `app.ico` (icon), `make_icon.ps1` (regenerates the icon).

---

## 🇮🇹 In italiano

**PhoneVolume** regola il volume media del telefono Android dalla barra di Windows, vicino all'orologio.

1. Installa **ADB** ([Platform-Tools di Google](https://developer.android.com/tools/releases/platform-tools)) e attiva il **debug USB** sul telefono (Impostazioni → Info telefono → Informazioni software → tocca 7 volte *Numero build* → Opzioni sviluppatore → Debug USB).
2. Collega il telefono via USB e accetta *"Consenti sempre da questo computer"*.
3. Avvia **[PhoneVolume.exe](https://github.com/GenGeCo/GenPlayer/raw/main/PhoneVolume/bin/PhoneVolume.exe)**. Se Windows mostra un avviso: *Ulteriori informazioni → Esegui comunque*.
4. Se l'icona non si vede, clicca la freccetta **^** vicino all'orologio e trascinala sulla barra.
5. **Clic sinistro** = cursore del volume (rotella = un livello per scatto) · **Clic destro** = scelta telefono, avvio con Windows, esci.

Il modo più semplice: metti `PhoneVolume.exe` dentro la cartella `platform-tools`.

---

Freeware, Copyright © 2026 GeCo gruppogea — part of the [GenPlayer](https://github.com/GenGeCo/GenPlayer) project.
