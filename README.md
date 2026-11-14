# forth-experiments

A collection of experiments for learning the Forth programming language, targeting embedded hardware with [zeptoforth](https://github.com/tabemann/zeptoforth).

## Repository layout

```
forth-experiments/
├── deps/
│   ├── zeptoforth/       ← git submodule (Forth implementation for RP2040/RP2350)
│   └── cyw43-firmware/   ← git submodule (CYW43439 WiFi chip firmware blobs)
├── lib/
│   └── lcd.fs            ← shared HD44780 / LCD1602A driver (4-bit parallel)
├── pico2w-led-blink/     ← blink the onboard LED on a Raspberry Pi Pico 2W
├── pico2w-hello-led/     ← blink an external LED on GP15
└── pico2w-hello-lcd/     ← display text on a 16×2 LCD1602A (4-bit parallel)
```

Experiments pull in shared code from `lib/` with `#include ../lib/<file>.fs`.
`codeload3.sh` resolves the path relative to the including file and uploads it inline.

## Getting started

Clone with submodules:

```bash
git clone --recurse-submodules <repo-url>
```

If you already cloned without submodules:

```bash
git submodule update --init --recursive
```

System dependencies (Arch Linux):

```bash
sudo pacman -S python-pyserial screen
```

Serial access: on Arch, `/dev/ttyACM*` belongs to the `uucp` group. Add yourself
once so the Makefile targets work without `sudo`, then log out and back in
(`id` should list `uucp`):

```bash
sudo usermod -aG uucp $USER
```

### Flash zeptoforth

The firmware images are not in the git repository; they ship with each
[zeptoforth release](https://github.com/tabemann/zeptoforth/releases). Download
the 1.16.1.1 release archive, extract it, then hold BOOTSEL while plugging in the
Pico 2W and copy the UF2 from the extracted archive:

```bash
cp zeptoforth-1.16.1.1/bin/1.16.1.1/rp2350/zeptoforth_full_usb-1.16.1.1.uf2 \
   /run/media/$USER/RP2350/
```

## Makefile

The root `Makefile` provides these targets for working with a connected Pico over USB serial.

| Target | Description |
|--------|-------------|
| `make tty` | Print the auto-detected `/dev/ttyACM*` path of the connected Pico |
| `make load` | Load a Forth source file onto the Pico (defaults to `pico2w-hello-lcd/hello-lcd.fs`) |
| `make cyw43` | Load the CYW43 WiFi-chip driver (needed by `pico2w-led-blink`) |
| `make repl` | Open the zeptoforth REPL over serial |

Loading a specific file:

```bash
make load LOAD_FILE=pico2w-hello-led/hello-led.fs
```

The Pico is detected automatically by its zeptoforth USB serial ID — no need to hardcode `/dev/ttyACMx`. None of the targets need `sudo` once you are in the `uucp` group (see above). Exit `screen` with `Ctrl-A \`.

Code loaded this way lives in RAM, so reload it after every power cycle.

## Experiments

| Directory | Board | Description |
|-----------|-------|-------------|
| [pico2w-led-blink](pico2w-led-blink/) | Raspberry Pi Pico 2W | Blink the onboard LED via the CYW43439 WiFi chip |
| [pico2w-hello-led](pico2w-hello-led/) | Raspberry Pi Pico 2W | Blink an external LED on GP15 via direct GPIO |
| [pico2w-hello-lcd](pico2w-hello-lcd/) | Raspberry Pi Pico 2W | Display "hello world, / zeptoforth!" on a 16×2 LCD1602A (4-bit parallel) |

## Hardware

Experiments currently target the **Raspberry Pi Pico 2W** (RP2350). The Forth implementation used is [zeptoforth](https://github.com/tabemann/zeptoforth).

## Dependencies

- **zeptoforth** — Forth implementation for RP2040/RP2350 boards (`deps/zeptoforth/`)
- **cyw43-firmware** — binary firmware blobs for the CYW43439 chip on the Pico W / Pico 2W (`deps/cyw43-firmware/`)
