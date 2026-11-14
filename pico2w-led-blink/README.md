# Blinking the LED on Raspberry Pi Pico 2W with zeptoforth

The Pico 2W (RP2350) onboard LED is connected through the CYW43439 WiFi chip,
not directly to a GPIO pin. This requires the CYW43 driver to be loaded before
the LED can be controlled.

## Dependencies

### zeptoforth

The Forth implementation for RP2040/RP2350 boards.

- Tested version: 1.17.2, available as a git submodule at `../deps/zeptoforth/`
- Flash it as described in the [root README](../README.md#flash-zeptoforth)

### CYW43439 firmware

Binary firmware blobs for the CYW43439 WiFi/BT chip on the Pico W / Pico 2W.
Not included in zeptoforth — provided as a git submodule at `../deps/cyw43-firmware/`.

- Repository: https://github.com/tabemann/cyw43-firmware
- Files needed: `cyw43439-firmware/43439A0.bin` and `cyw43439-firmware/43439A0_clm.bin`

## One-time setup: upload the CYW43439 firmware blobs

The blobs are stored in flash, so this survives reboots. From the repository root:

```bash
deps/zeptoforth/utils/load_cyw43_fw.sh \
  $(make -s tty) \
  deps/cyw43-firmware/cyw43439-firmware/43439A0.bin \
  deps/cyw43-firmware/cyw43439-firmware/43439A0_clm.bin
```

This uploads the firmware as `cyw43-fw` and `cyw43-clm` modules to flash.
If you see "module already defined", the firmware is already there — skip this step.

## Every boot

The CYW43 driver and `blink.fs` load to RAM, so repeat this after each power
cycle. From the repository root:

```bash
make cyw43                                   # CYW43 driver
make load LOAD_FILE=pico2w-led-blink/blink.fs
make repl
```

If you see "module already defined" errors from `make cyw43`, the driver is
already loaded.

At the REPL:

```forth
blink
```

Press any key to stop blinking.

## Notes

### Do not type multi-line code in the REPL directly

The serial terminal wraps long lines and splits tokens mid-word (e.g. `my-ctrl`
becomes `my-ct rl`). Always load `.fs` files with `make load`.

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
