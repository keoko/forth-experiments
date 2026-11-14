# Thermometer — the RP2350's internal temperature sensor on the LCD

Reads the temperature sensor built into the RP2350 through its ADC and shows it
on the LCD, updated every second by a background task:

```
temp: 16.8°C
adc:  898
```

No new parts: the sensor is inside the chip.

## Circuit

Only the LCD, wired as in [pico2w-hello-lcd](../pico2w-hello-lcd/README.md#circuit).

## Files

- `thermometer.fs` — ADC reading, conversion to °C, display task
- [`../lib/lcd.fs`](../lib/lcd.fs) — the shared HD44780 driver

## Run

If another experiment with a background task has run since the last boot, or
the standalone app is installed, reboot or `make unflash` first. Then, from the
repository root:

```bash
make load LOAD_FILE=pico2w-thermometer/thermometer.fs
make repl
```

At the REPL:

```forth
start-thermometer
```

The prompt comes straight back. Put a finger on the Pico's chip (the large
square in the middle) and watch the temperature rise within a few seconds.

From the REPL, or without opening it:

```forth
tenths @ .     \ e.g. 246 = 24.6 °C
raw @ .        \ the averaged ADC reading
```

```bash
make send CMD='tenths @ .'
```

## How it works

### The ADC

The ADC (analog-to-digital converter) measures a voltage and returns a 12-bit
number: 0 for 0 V up to 4095 for 3.3 V. It has several *channels*: GP26–GP29 on
the board's pins, plus channel 4, wired inside the chip to the temperature sensor.

```forth
temp-adc-chan default-adc adc@   ( -- u )   \ channel 4 on ADC 0
```

zeptoforth switches the temperature sensor on at boot, so there's no setup.
`read-raw` reads it 16 times and averages, because single readings jitter by a
few counts.

### From counts to °C

Two steps, straight from the RP2350 datasheet:

```
V = raw × 3.3 / 4096                 raw>volts
T = 27 − (V − 0.706) / 0.001721      volts>celsius
```

The sensor reads 0.706 V at 27 °C, and the voltage *drops* by 1.721 mV per
degree, hence the minus. For example raw 898 → 0.7235 V → 16.8 °C.

The sensor isn't calibrated: expect it to be a few degrees off a room
thermometer, and a little warmer than the air, because it measures the chip.

### Fixed-point numbers

Forth on a microcontroller usually has no floating point. zeptoforth has
**fixed-point** numbers instead (S31.32): two cells, 32 bits for the integer
part and 32 for the fraction. You write them with a comma:

```forth
3,3        \ 3.3
0,001721   \ 0.001721
27,0       \ 27.0 — the ,0 makes it fixed-point, not the integer 27
```

| Word | Stack | Meaning |
|------|-------|---------|
| `s>f` | ( n -- f ) | integer to fixed-point |
| `f*` `f/` | ( f1 f2 -- f3 ) | multiply, divide |
| `d+` `d-` | ( f1 f2 -- f3 ) | add, subtract (the double-cell words work as is) |
| `2swap` | ( f1 f2 -- f2 f1 ) | swap two fixed-point values |
| `round-half-even` | ( f -- n ) | round to the nearest integer |

Each fixed-point value takes two cells, so stack comments here write it as `f`,
and stack-shuffling uses the `2`-words (`2swap`, `2dup`, ...).

The temperature is kept as an integer number of **tenths**: 24.6 °C becomes 246.
Integers are easier to store, compare and print.

### Printing `24.6°C`

Pictured numeric output builds the string from right to left:

```forth
dup abs 0 <# [char] C hold $DF hold # [char] . hold #s rot sign #>
```

| Step | String so far |
|------|---------------|
| `[char] C hold` | `C` |
| `$DF hold` | `°C` |
| `#` (one digit: the tenths) | `6°C` |
| `[char] . hold` | `.6°C` |
| `#s` (the rest) | `24.6°C` |
| `rot sign` (a `-` if negative) | `24.6°C` |

`$DF` is the degree sign in the HD44780's built-in character table; it isn't
ASCII, so a terminal shows something else if you `type` it in the REPL.

### The update task

`thermometer-loop` reads, displays, then waits a second with `ms`, which lets the
REPL run in the meantime. It's the same pattern as
[pico2w-dimmer-task](../pico2w-dimmer-task/): one task owns the LCD, and the
REPL only reads the variables.
