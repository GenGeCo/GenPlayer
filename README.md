# GenPlayer

**High-Fidelity Audio Player for Android**

GenPlayer is the audio player for audiophiles who want to know *exactly* what's playing. Codec, bitrate, sample rate, protocol — GenPlayer declares everything. Zero secrets, zero ads, zero tracking.

---

## Website

**[www.gruppogea.net/genplayer/](https://www.gruppogea.net/genplayer/)**

---

## Download

--------------------

---

## Screenshots

| Player | Library | File Info |
|:------:|:-------:|:---------:|
| ![Player](https://www.gruppogea.net/genplayer/foto_genplayer/genplayer-flac-player.jpg) | ![Library](https://www.gruppogea.net/genplayer/foto_genplayer/genplayer-music-library.jpg) | ![Info](https://www.gruppogea.net/genplayer/foto_genplayer/genplayer-file-info.jpg) |

| VU Meter Yellow | VU Meter Green | VU Meter Blue |
|:---------------:|:--------------:|:-------------:|
| ![VU Yellow](https://www.gruppogea.net/genplayer/foto_genplayer/genplayer-vu-meter-yellow-1.jpg) | ![VU Green](https://www.gruppogea.net/genplayer/foto_genplayer/genplayer-vu-meter-green.jpg) | ![VU Blue](https://www.gruppogea.net/genplayer/foto_genplayer/genplayer-vu-meter-blue.jpg) |

| DSP Filter | Tube Transformer | Loudness ISO 226 |
|:----------:|:----------------:|:----------------:|
| ![DSP](https://www.gruppogea.net/genplayer/foto_genplayer/genplayer-dsp-filter.jpg) | ![Tube](https://www.gruppogea.net/genplayer/foto_genplayer/genplayer-dsp-transformer.jpg) | ![Loudness](https://www.gruppogea.net/genplayer/foto_genplayer/genplayer-loudness-iso226.jpg) |

---

## Features

### 3-in-1 Player

One app for all your audio sources:

- **Web Radio** — PLS/M3U support, automatic logo/cover retrieval, quality filters (FLAC/HIGH/MEDIUM), connection stability monitor
- **Local Files** — FLAC, WAV, MP3, AAC, OGG with embedded album art, browse by Album/Artist/Folder
- **NAS / Network** — SMB/DLNA support, remote cover art caching, circular buffer for instant seek, streaming without download

### Total Transparency

Every screen shows codec, bitrate, sample rate, bit depth, container format, and streaming protocol. The **Pro** button opens detailed metadata and technical specs for any file or stream. Made for nerd audiophiles who don't settle for "play" — they want to understand what's under the hood.

### Realistic VU Meter

A vintage amplifier visual experience on your smartphone:

- Physical modeling with mass/spring needle dynamics
- Dynamic Sag effect (power supply voltage drop simulation)
- Adjustable lighting (Yellow, Green, Blue themes)
- Precise calibration and audio/video sync
- Cinematic fullscreen landscape mode

### Advanced DSP (Native C++)

Proprietary filters inspired by professional audio:

**Signal Hygiene**
- DC Blocker — Always ON, removes sub-20Hz offset
- Soft Clipper — Anti-clipping protection without ruining dynamics

**Color Box (Analog Harmonic Simulation)**
- T1 - Tube Warm — Classic warmth, soft symmetrical harmonics
- T2 - Triode Vintage — Asymmetric Class A saturation, 70s style
- T3 - Transformer — Fat Bass + Crystal Highs

**Psychoacoustics**
- ISO 226 Loudness — Fletcher-Munson compensation (Auto + Manual)
- Crossfeed — Reduces headphone fatigue with natural stereo image
- Dynamic Sag — Simulates an amplifier that "breathes" on bass transients

---

## Why Free?

Simply because I wanted a player like this. A player that tells me: What WiFi protocol did the stream connect with? How is USB-C transmitting? What codec does this radio use? What's the real bitrate I'm listening to?

I searched. Couldn't find anything that gave me all this information.

So we decided: **let's make it!**

No ads, no tracking, no data selling. Just an app for those who, like me, want to know everything.

---

## Roadmap

### Completed

- Hybrid Architecture (Kotlin + C++ JNI)
- Deep Analysis (Real-time Bitrate, Codec, Transport)
- 3-in-1 Player (Web Radio, Local Files, NAS)
- Realistic VU Meter with physical modeling
- Advanced DSP Controls (Loudness, Tube Sim, Crossfeed)
- Android Notification/Lockscreen controls
- Hub Mode shortcuts for external apps

### Upcoming

- **Bit-Perfect Mode** — Direct audio output bypassing Android mixer
- **Parametric EQ** — Professional multiband equalizer
- **DSP for External Apps** (Android 9+) — Apply GenPlayer DSP effects to Spotify, YouTube Music, Apple Music, Prime Music
  - Loudness ISO 226
  - Multiband Compressor
  - Post-Processing Limiter
  - Clean Bass Enhancement

---

## Tech Stack

- **Android** (Kotlin)
- **Native C++** via JNI for DSP processing
- **100% Offline** — No registration, no cloud dependency

---

## License 

Freeware, Copyright © 2025 GeCo gruppogea. All Rights Reserved. 

---

**Website**: https://www.gruppogea.net/genplayer/

**Contact**: gecogea@gmail.com

---

*GenPlayer - Music the way it should sound.*
