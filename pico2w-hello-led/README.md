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

## Dependencies

- zeptoforth 1.17.2 (submodule at `../deps/zeptoforth/`), flashed as described in
  the [root README](../README.md#flash-zeptoforth)
- No CYW43 driver needed — GP15 is a plain GPIO pin

## Run

From the repository root:

```bash
make load LOAD_FILE=pico2w-hello-led/hello-led.fs
make repl
```

At the REPL:

```forth
blink
```

Press any key to stop (the key is checked once per on/off cycle, so it can take up to 4 s).

## How it works

| MicroPython | zeptoforth |
|---|---|
| `machine.Pin(15, machine.Pin.OUT)` | `15 output-pin` |
| `led.value(1)` | `high 15 pin!` |
| `led.value(0)` | `low 15 pin!` |
| `utime.sleep(2)` | `2000 ms` |

`pin!` takes `( state pin -- )`. `high` and `low` are constants (`true`/`false`)
exported by the `pin` module.

`drain-pressed-keys` (`begin key? while key drop repeat`) runs before the loop and drains any buffered
keypress (the Enter used to invoke `blink`) so the loop does not exit immediately.

The setup word is called `init-led` rather than `init`: in zeptoforth, `init` is
the boot hook that the system runs at startup, and redefining it would break boot
if the file were ever compiled to flash.
