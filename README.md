# forth-experiments

A collection of experiments for learning the Forth programming language, targeting embedded hardware with [zeptoforth](https://github.com/tabemann/zeptoforth).

## Repository layout

```
forth-experiments/
├── deps/
│   ├── zeptoforth/       ← git submodule (Forth implementation for RP2040/RP2350)
│   └── cyw43-firmware/   ← git submodule (CYW43439 WiFi chip firmware blobs)
├── lib/
│   ├── lcd.fs            ← shared HD44780 / LCD1602A driver (4-bit parallel)
│   └── button.fs         ← shared debounced button with short/long press
├── pico2w-led-blink/     ← blink the onboard LED on a Raspberry Pi Pico 2W
├── pico2w-hello-led/     ← blink an external LED on GP15
├── pico2w-hello-lcd/     ← display text on a 16×2 LCD1602A (4-bit parallel)
├── pico2w-button-counter/ ← count debounced button presses on the LCD
└── pico2w-led-dimmer/    ← set LED brightness with PWM from the button
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
the 1.17.2 release archive, extract it, then hold BOOTSEL while plugging in the
Pico 2W and copy the UF2 from the extracted archive:

```bash
udisksctl mount -b /dev/disk/by-label/RP2350   # skip if your desktop already mounted it
cp zeptoforth-1.17.2/bin/1.17.2/rp2350/zeptoforth_full_usb-1.17.2.uf2 \
   /run/media/$USER/RP2350/
```

If `cp` says `/run/media/$USER/RP2350/: No such file or directory`, the drive
isn't mounted — run the `udisksctl` line. `lsblk -o NAME,LABEL` shows it as
`RP2350` while the Pico is in BOOTSEL mode. The Pico reboots into zeptoforth by
itself once the copy finishes.

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

The Pico is detected automatically by its zeptoforth USB serial ID — no need to hardcode `/dev/ttyACMx`. None of the targets need `sudo` once you are in the `uucp` group (see above).

Code loaded this way lives in RAM, so reload it after every power cycle.

### Working with the REPL

`make repl` opens the REPL in `screen`. Only one program can use the serial
port at a time, so **`make load` fails while the REPL is open**:

```
Error: TTY device /dev/ttyACM0 invalid
```

The usual cycle is: load, open the REPL, try things, close the REPL, edit, load again.

```bash
make load LOAD_FILE=pico2w-button-counter/button-counter.fs
make repl
```

**Closing the REPL properly:**

1. If a program is running (e.g. `counter`), press any key to stop it and get
   the `ok` prompt back.
2. Optional: type `reboot` to clear RAM and reset the hardware (LEDs off) before
   loading something else.
3. Press `Ctrl-A`, then `\`, and answer `y` to "Really quit and kill all your windows?".

Don't close the terminal window instead, and don't use `Ctrl-A d`: both leave
`screen` running in the background, still holding the port. If `make load`
reports the port as invalid and no REPL is visible, find and close the leftover
session:

```bash
screen -ls                    # lists sessions, e.g. 5278.pts-1.host (Detached)
screen -r 5278                # reattach, then quit with Ctrl-A \ as above
screen -X -S 5278 quit        # or close it without reattaching
```

`fuser -v /dev/ttyACM*` shows which process has the port open.

**Don't type multi-line code into the REPL.** The terminal wraps long lines and
can split words in two (`my-ctrl` becomes `my-ct rl`). Put the code in a `.fs`
file and load it with `make load`; use the REPL for calling words and quick
one-liners such as `25 brightness!`.

## Experiments

| Directory | Board | Description |
|-----------|-------|-------------|
| [pico2w-led-blink](pico2w-led-blink/) | Raspberry Pi Pico 2W | Blink the onboard LED via the CYW43439 WiFi chip |
| [pico2w-hello-led](pico2w-hello-led/) | Raspberry Pi Pico 2W | Blink an external LED on GP15 via direct GPIO |
| [pico2w-hello-lcd](pico2w-hello-lcd/) | Raspberry Pi Pico 2W | Display "hello world, / zeptoforth!" on a 16×2 LCD1602A (4-bit parallel) |
| [pico2w-button-counter](pico2w-button-counter/) | Raspberry Pi Pico 2W | Count debounced button presses on GP14 and show them on the LCD |
| [pico2w-led-dimmer](pico2w-led-dimmer/) | Raspberry Pi Pico 2W | Set the GP15 LED's brightness with PWM, stepped by the button, shown on the LCD |

## Hardware

Experiments currently target the **Raspberry Pi Pico 2W** (RP2350). The Forth implementation used is [zeptoforth](https://github.com/tabemann/zeptoforth).

## Dependencies

- **zeptoforth** — Forth implementation for RP2040/RP2350 boards (`deps/zeptoforth/`)
- **cyw43-firmware** — binary firmware blobs for the CYW43439 chip on the Pico W / Pico 2W (`deps/cyw43-firmware/`)
