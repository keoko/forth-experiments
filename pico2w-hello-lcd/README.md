# Hello, LCD! — LCD1602A 16-pin parallel display on zeptoforth

Displays "hello world," / "zeptoforth!" on a bare 16-pin LCD1602A connected to a
Raspberry Pi Pico 2W (RP2350) in 4-bit parallel mode.

## The 16 pins explained

The LCD1602A has two rows of 8 solder pads (or a single 16-pin header):

| Pin | Label | Function |
|-----|-------|----------|
| 1   | VSS   | Ground |
| 2   | VDD   | Power (5 V) |
| 3   | V0    | Contrast adjust (see note) |
| 4   | RS    | Register Select: 0 = command, 1 = data |
| 5   | RW    | Read/Write: tie to GND (write-only) |
| 6   | E     | Enable — rising/falling edge latches data |
| 7   | D0    | Data bit 0 — **not connected** in 4-bit mode |
| 8   | D1    | Data bit 1 — **not connected** in 4-bit mode |
| 9   | D2    | Data bit 2 — **not connected** in 4-bit mode |
| 10  | D3    | Data bit 3 — **not connected** in 4-bit mode |
| 11  | D4    | Data bit 4 |
| 12  | D5    | Data bit 5 |
| 13  | D6    | Data bit 6 |
| 14  | D7    | Data bit 7 |
| 15  | A     | Backlight LED anode (+) |
| 16  | K     | Backlight LED cathode (−) |

In 4-bit mode only D4–D7 carry data; D0–D3 are left floating.

## Circuit

```
LCD pin  Label  →  Pico 2W
──────────────────────────────────────────
 1       VSS    →  Pin 38  (GND)
 2       VDD    →  Pin 40  (VBUS, 5 V)
 3       V0     →  GND     (see contrast note)
 4       RS     →  GP16    (pin 21)
 5       RW     →  GND
 6       E      →  GP17    (pin 22)
 7-10    D0-D3  →  not connected
11       D4     →  GP18    (pin 24)
12       D5     →  GP19    (pin 25)
13       D6     →  GP20    (pin 26)
14       D7     →  GP21    (pin 27)
15       A      →  Pin 36  (3V3) via 100 Ω resistor
16       K      →  GND
```

> **Power (VDD):** This LCD1602A module requires 5 V on VDD for sufficient
> contrast. Use VBUS (pin 40) rather than 3V3 OUT (pin 36). The logic signals
> (RS, E, D4–D7) remain at 3.3 V and connect directly to the Pico GPIOs. This is
> safe only because RW is tied to GND: the LCD never drives the data lines, so no
> 5 V signal reaches the Pico.
>
> **Contrast (V0):** With VDD at 5 V, connecting V0 directly to GND gives
> maximum contrast and is the easiest starting point. For fine-tuning, wire a
> 10 kΩ potentiometer between VDD and GND and connect its wiper to V0.
>
> **Blank display?** The most common causes are (1) VDD at 3.3 V instead of
> 5 V — switch to VBUS; (2) contrast — adjust V0.

## Files

- `hello-lcd.fs` — defines `hello`; pulls in the driver with `#include ../lib/lcd.fs`
- [`../lib/lcd.fs`](../lib/lcd.fs) — the reusable HD44780 driver (pins, init, `lcd-print`)

## Dependencies

- zeptoforth 1.17.2 (submodule at `../deps/zeptoforth/`), flashed as described in
  the [root README](../README.md#flash-zeptoforth)
- No extra zeptoforth driver needed — the LCD driver is in this repo

## Run

From the repository root:

```bash
make load    # loads pico2w-hello-lcd/hello-lcd.fs (the default)
make repl
```

At the REPL:

```forth
hello
```

The display should show:

```
hello world,
zeptoforth!
```

### Troubleshooting

| What you see | Likely cause |
|---|---|
| A row of solid blocks on line 1 | Power and contrast are fine, but the LCD hasn't been initialised — run `hello`. If the blocks remain, check E (GP17), RS (GP16), RW tied to GND, and a shared ground |
| Random characters | D4–D7 swapped or loose — check them against GP18–GP21 in order |
| Blank screen, no blocks | Contrast — tie V0 to GND or adjust the potentiometer |

## How it works

The HD44780 controller inside the LCD1602A accepts 8-bit commands and data.
In **4-bit mode** only pins D4–D7 are used; each byte is sent as two nibbles
(high nibble first), each latched by a pulse on Enable.

All of these words live in `../lib/lcd.fs`.

| Step | Word | What it does |
|------|------|--------------|
| Configure GPIO | `init-pins` | Sets GP16-GP21 as outputs |
| Send 4 bits | `nibble!` | Drives D4-D7 from low nibble of n |
| Latch | `pulse-en` | Pulses E high→low with 1 ms hold |
| Send 8 bits | `send-byte` | Sends high nibble then low nibble |
| Command | `cmd!` | Calls `send-byte` with RS = 0 |
| Data | `data!` | Calls `send-byte` with RS = 1 |
| Cursor | `goto` | Sends DDRAM address command (`$80\|addr`) |
| Print | `lcd-print` | Sets cursor then sends each character |
| Clear | `lcd-clear` | Clears the display and homes the cursor |

Initialisation follows the HD44780 datasheet's "4-bit mode" sequence: three
8-bit reset pulses using only the upper nibble, then the switch to 4-bit, then
function-set / display / entry-mode commands.
