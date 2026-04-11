# forth-experiments

A collection of experiments for learning the Forth programming language, targeting embedded hardware with [zeptoforth](https://github.com/tabemann/zeptoforth).

## Repository layout

```
forth-experiments/
├── deps/
│   ├── zeptoforth/       ← git submodule (Forth implementation for RP2040/RP2350)
│   └── cyw43-firmware/   ← git submodule (CYW43439 WiFi chip firmware blobs)
└── pico2w-led-blink/     ← blink the onboard LED on a Raspberry Pi Pico 2W
```

## Getting started

Clone with submodules:

```bash
git clone --recurse-submodules <repo-url>
```

If you already cloned without submodules:

```bash
git submodule update --init --recursive
```

## Experiments

| Directory | Board | Description |
|-----------|-------|-------------|
| [pico2w-led-blink](pico2w-led-blink/) | Raspberry Pi Pico 2W | Blink the onboard LED via the CYW43439 WiFi chip |

## Hardware

Experiments currently target the **Raspberry Pi Pico 2W** (RP2350). The Forth implementation used is [zeptoforth](https://github.com/tabemann/zeptoforth).

## Dependencies

- **zeptoforth** — Forth implementation for RP2040/RP2350 boards (`deps/zeptoforth/`)
- **cyw43-firmware** — binary firmware blobs for the CYW43439 chip on the Pico W / Pico 2W (`deps/cyw43-firmware/`)
