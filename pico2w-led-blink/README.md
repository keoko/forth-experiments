# Blinking the LED on Raspberry Pi Pico 2W with zeptoforth

The Pico 2W (RP2350) onboard LED is connected through the CYW43439 WiFi chip,
not directly to a GPIO pin. This requires the CYW43 driver to be loaded before
the LED can be controlled.

## Repository structure

This experiment assumes the following layout (see parent repo for setup):

```
forth-experiments/
├── deps/
│   ├── zeptoforth/          ← git submodule
│   └── cyw43-firmware/      ← git submodule
└── pico2w-led-blink/
    ├── README.md            ← this file
    └── blink.fs
```

All commands below use paths relative to this structure.

## Dependencies

### zeptoforth

The Forth implementation for RP2040/RP2350 boards.

- Repository: https://github.com/tabemann/zeptoforth
- Tested version: 1.16.1.1
- Available as a git submodule at `../deps/zeptoforth/`

### CYW43439 firmware

Binary firmware blobs for the CYW43439 WiFi/BT chip on the Pico W / Pico 2W.
Not included in zeptoforth — provided as a git submodule at `../deps/cyw43-firmware/`.

- Repository: https://github.com/tabemann/cyw43-firmware
- Files needed: `cyw43439-firmware/43439A0.bin` and `cyw43439-firmware/43439A0_clm.bin`

### System dependencies (Arch Linux)

```bash
sudo pacman -S python-pyserial screen
```

- `python-pyserial` — required by the `utils/codeload3.sh` upload script
- `screen` — serial terminal to interact with the zeptoforth REPL

## One-time setup (survives reboots)

### 1. Flash zeptoforth firmware

Hold BOOTSEL while plugging in the Pico 2W. It appears as a USB mass storage
device. Copy the UF2:

```bash
cp ../deps/zeptoforth/bin/1.16.1.1/rp2350/zeptoforth_full_usb-1.16.1.1.uf2 \
   /run/media/$USER/RP2350/
```

### 2. Upload CYW43439 firmware blobs

Run from the `pico2w-led-blink/` directory:

```bash
sudo ../deps/zeptoforth/utils/load_cyw43_fw.sh \
  /dev/ttyACM0 \
  ../deps/cyw43-firmware/cyw43439-firmware/43439A0.bin \
  ../deps/cyw43-firmware/cyw43439-firmware/43439A0_clm.bin
```

This uploads the firmware as `cyw43-fw` and `cyw43-clm` modules to flash.
If you see "module already defined", the firmware is already there — skip this step.

### 3. Upload the CYW43 driver

```bash
cd ../deps/zeptoforth
sudo utils/codeload3.sh -B 115200 -p /dev/ttyACM0 serial \
  extra/rp_common/cyw43/cyw43_all.fs
```

Note: this loads to RAM by default and must be repeated after each power cycle.
If you see "module already defined" errors, the driver is already loaded.

## Every boot

### 1. Reload the CYW43 driver

```bash
cd ../deps/zeptoforth
sudo utils/codeload3.sh -B 115200 -p /dev/ttyACM0 serial \
  extra/rp_common/cyw43/cyw43_all.fs
```

### 2. Load and run the blink code

From the `pico2w-led-blink/` directory:

```bash
cd ../deps/zeptoforth
sudo utils/codeload3.sh -B 115200 -p /dev/ttyACM0 serial \
  ../../pico2w-led-blink/blink.fs
```

Then open the REPL:

```bash
screen /dev/ttyACM0 115200
```

And run:

```forth
blink
```

Press any key to stop blinking.

## Notes

### tty device name

The device may appear as `/dev/ttyACM0`, `/dev/ttyACM1`, `/dev/ttyACM7`, etc.
Check with:

```bash
ls /dev/ttyACM*
```

### Do not type multi-line code in the REPL directly

The serial terminal wraps long lines and splits tokens mid-word (e.g. `my-ctrl`
becomes `my-ct rl`). Always use `codeload3.sh` to load `.fs` files.

### CYW43 pin mapping (Pico W / Pico 2W)

| Signal | GPIO |
|--------|------|
| PWR    | 23   |
| CLK    | 29   |
| DIO    | 24   |
| CS     | 25   |

### Why not use deps/zeptoforth/test/rp2040/pico_w_blinky.fs?

That file passes an extra `pio-addr` argument that is incompatible with the
current cyw43 driver API, causing a "PIO out of range" error. Use `blink.fs`
instead, which passes `-1 -1` to let the driver auto-allocate PIO resources.

### LED GPIO index

The Pico 2W LED is on CYW43 GPIO index `0`. The `cyw43-gpio!` word takes
`( val index self -- )`:

```forth
true  0 my-ctrl cyw43-gpio!   \ LED on
false 0 my-ctrl cyw43-gpio!   \ LED off
```

### Drain input buffer before blinking

Without `begin key? while key drop repeat` before the blink loop, the Enter
keypress used to invoke `blink` is still in the input buffer, causing `key?`
to return true immediately and the loop to exit after one cycle.
