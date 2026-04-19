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
> (RS, E, D4–D7) remain at 3.3 V and connect directly to the Pico GPIOs.
>
> **Contrast (V0):** With VDD at 5 V, connecting V0 directly to GND gives
> maximum contrast and is the easiest starting point. For fine-tuning, wire a
> 10 kΩ potentiometer between VDD and GND and connect its wiper to V0.
>
> **Blank display?** The most common causes are (1) VDD at 3.3 V instead of
> 5 V — switch to VBUS; (2) contrast — adjust V0.

## Repository structure

```
forth-experiments/
├── deps/
│   └── zeptoforth/          ← git submodule
└── pico2w-hello-lcd/
    ├── README.md            ← this file
    └── hello-lcd.fs
```

## Dependencies

### zeptoforth

- Repository: https://github.com/tabemann/zeptoforth
- Tested version: 1.16.1.1
- Available as a git submodule at `../deps/zeptoforth/`
- No extra driver needed — `hello-lcd.fs` is self-contained

### System dependencies (Arch Linux)

```bash
sudo pacman -S python-pyserial screen
```

## Setup

### 1. Flash zeptoforth firmware

Hold BOOTSEL while plugging in the Pico 2W. Copy the UF2:

```bash
cp ../deps/zeptoforth/bin/1.16.1.1/rp2350/zeptoforth_full_usb-1.16.1.1.uf2 \
   /run/media/$USER/RP2350/
```

### 2. Load the program

```bash
cd ../deps/zeptoforth
sudo utils/codeload3.sh -B 115200 -p /dev/ttyACM0 serial \
  ../../pico2w-hello-lcd/hello-lcd.fs
```

### 3. Open the REPL and run

```bash
screen /dev/ttyACM0 115200
```

```forth
hello
```

The display should show:

```
hello world,
zeptoforth!
```

## Validating the circuit with MicroPython

Before running the zeptoforth code, wiring can be verified with a MicroPython
script. Flash the MicroPython UF2 for RP2350, then run a script using the same
GP16–GP21 pin mapping:

```python
import machine, utime

rs = machine.Pin(16, machine.Pin.OUT)
e  = machine.Pin(17, machine.Pin.OUT)
d4 = machine.Pin(18, machine.Pin.OUT)
d5 = machine.Pin(19, machine.Pin.OUT)
d6 = machine.Pin(20, machine.Pin.OUT)
d7 = machine.Pin(21, machine.Pin.OUT)
```

If text appears under MicroPython, the circuit is correct and the zeptoforth
code will work after reflashing zeptoforth firmware.

## How it works

The HD44780 controller inside the LCD1602A accepts 8-bit commands and data.
In **4-bit mode** only pins D4–D7 are used; each byte is sent as two nibbles
(high nibble first), each latched by a pulse on Enable.

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

Initialisation follows the HD44780 datasheet's "4-bit mode" sequence: three
8-bit reset pulses using only the upper nibble, then the switch to 4-bit, then
function-set / display / entry-mode commands.
