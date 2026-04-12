# Hello, LED! — SunFounder experiment 2 ported to zeptoforth

Port of the [SunFounder Pico 2W kit "Hello, LED!"](https://docs.sunfounder.com/projects/pico-2w-kit/en/latest/pyproject/py_led.html)
project to zeptoforth on the Raspberry Pi Pico 2W (RP2350).

## Circuit

```
GP15 (pin 20) ──[220Ω]──[LED anode]──[LED cathode]── GND
```

- Insert one leg of a 220Ω resistor into the same breadboard row as GP15.
- Connect the other leg to the LED anode (longer leg).
- Connect the LED cathode (shorter leg) to GND.

## Repository structure

```
forth-experiments/
├── deps/
│   └── zeptoforth/          ← git submodule
└── pico2w-hello-led/
    ├── README.md            ← this file
    └── hello-led.fs
```

## Dependencies

### zeptoforth

- Repository: https://github.com/tabemann/zeptoforth
- Tested version: 1.16.1.1
- Available as a git submodule at `../deps/zeptoforth/`

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

### 2. Load and run

No CYW43 driver needed — GP15 is a plain GPIO pin.

```bash
cd ../deps/zeptoforth
sudo utils/codeload3.sh -B 115200 -p /dev/ttyACM0 serial \
  ../../pico2w-hello-led/hello-led.fs
```

Open the REPL:

```bash
screen /dev/ttyACM0 115200
```

Run:

```forth
blink
```

Press any key to stop.

## How it works

| MicroPython | zeptoforth |
|---|---|
| `machine.Pin(15, machine.Pin.OUT)` | `15 output-pin` |
| `led.value(1)` | `high 15 pin!` |
| `led.value(0)` | `low 15 pin!` |
| `utime.sleep(2)` | `2000 ms` |

`pin!` takes `( state pin -- )`. `high` and `low` are constants (`true`/`false`)
exported by the `pin` module.

The `begin key? while key drop repeat` before the loop drains any buffered
keypress (the Enter used to invoke `blink`) so the loop does not exit immediately.
