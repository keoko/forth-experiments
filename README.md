# forth-experiments

A collection of experiments for learning the Forth programming language, targeting embedded hardware with [zeptoforth](https://github.com/tabemann/zeptoforth).

## Repository layout

```
forth-experiments/
├── deps/
│   ├── zeptoforth/       ← git submodule (Forth implementation for RP2040/RP2350)
│   └── cyw43-firmware/   ← git submodule (CYW43439 WiFi chip firmware blobs)
├── pico2w-led-blink/     ← blink the onboard LED on a Raspberry Pi Pico 2W
├── pico2w-hello-led/     ← blink an external LED on GP15
└── pico2w-hello-lcd/     ← display text on a 16×2 I2C LCD (LCD1602 PCF8574)
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

## Makefile

The root `Makefile` provides three targets for working with a connected Pico over USB serial.

| Target | Description |
|--------|-------------|
| `make tty` | Print the auto-detected `/dev/ttyACM*` path of the connected Pico |
| `make load` | Load a Forth source file onto the Pico (defaults to `pico2w-hello-lcd/hello-lcd.fs`) |
| `make repl` | Open the zeptoforth REPL over serial |

Loading a specific file:

```bash
make load LOAD_FILE=pico2w-hello-led/hello-led.fs
```

The Pico is detected automatically by its zeptoforth USB serial ID — no need to hardcode `/dev/ttyACMx`. Both `load` and `repl` require `sudo`. Exit `screen` with `Ctrl-A \`.

## Experiments

| Directory | Board | Description |
|-----------|-------|-------------|
| [pico2w-led-blink](pico2w-led-blink/) | Raspberry Pi Pico 2W | Blink the onboard LED via the CYW43439 WiFi chip |
| [pico2w-hello-led](pico2w-hello-led/) | Raspberry Pi Pico 2W | Blink an external LED on GP15 via direct GPIO |
| [pico2w-hello-lcd](pico2w-hello-lcd/) | Raspberry Pi Pico 2W | Display "hello world, / zeptoforth!" on a 16×2 I2C LCD |

## Hardware

Experiments currently target the **Raspberry Pi Pico 2W** (RP2350). The Forth implementation used is [zeptoforth](https://github.com/tabemann/zeptoforth).

## Dependencies

- **zeptoforth** — Forth implementation for RP2040/RP2350 boards (`deps/zeptoforth/`)
- **cyw43-firmware** — binary firmware blobs for the CYW43439 chip on the Pico W / Pico 2W (`deps/cyw43-firmware/`)
