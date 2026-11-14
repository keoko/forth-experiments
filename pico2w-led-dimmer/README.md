# LED dimmer — PWM brightness control on zeptoforth

The push button sets the brightness of the LED on GP15, and the LCD shows the
level. A short press adds 10 % (wrapping from 100 % back to 0 %), and a long
press (1 s or more) switches the LED off.

```
brightness:
40%
```

## Circuit

No new parts. Everything is wired as in the earlier experiments:

| Part | Wiring | From |
|------|--------|------|
| LED | GP15 (pin 20) → 220 Ω → LED anode, cathode → GND | [pico2w-hello-led](../pico2w-hello-led/README.md#circuit) |
| LCD1602A | GP16–GP21, 5 V, GND | [pico2w-hello-lcd](../pico2w-hello-lcd/README.md#circuit) |
| Button | GP14 (pin 19) → button → GND | [pico2w-button-counter](../pico2w-button-counter/README.md#circuit) |

## Files

- `led-dimmer.fs` — the dimmer
- [`../lib/lcd.fs`](../lib/lcd.fs) — the shared HD44780 driver
- [`../lib/button.fs`](../lib/button.fs) — the shared debounced button (short/long press)

## Run

From the repository root:

```bash
make load LOAD_FILE=pico2w-led-dimmer/led-dimmer.fs
make repl
```

At the REPL:

```forth
dimmer
```

Press any key in the REPL to stop. The LED stays at its last brightness; type
`0 brightness!` to switch it off.

You can also set the brightness directly from the REPL, without the button:

```forth
25 brightness!
```

## How it works

### PWM in one paragraph

An LED can only be on or off. PWM (Pulse Width Modulation) switches it on and
off so fast — here 1500 times a second — that your eye only sees the average.
On 40 % of the time looks like 40 % brightness. The fraction of time the pin is
high is the **duty cycle**.

### The PWM hardware

The RP2350 has PWM *slices*: hardware counters that drive pins with no help
from the CPU once they're set up. Each slice drives two outputs, A and B.
GP15 belongs to **slice 7, output B** (slice = (GPIO / 2) mod 8; even GPIOs are
A, odd are B).

Each slice counts up from 0 to a **top** value, then wraps to 0. While the
counter is below the **compare** value, the pin is high; from compare to top,
it's low.

```
counter   0 ─────────────────► 999, wrap
pin       ████████░░░░░░░░░░░░   compare = 400 → 40 % duty
```

| Setting | Value | Why |
|---------|-------|-----|
| top | 999 | 1000 steps, so 1 % = 10 counts |
| clock divider | 100 | 150 MHz system clock / 100 = 1.5 MHz count rate |
| frequency | 1.5 kHz | 1.5 MHz / 1000 steps; well above visible flicker |
| compare | percent × 10 | 0 = off, 1000 (top + 1) = fully on |

| Word | What it does |
|------|--------------|
| `init-pwm-led` | Hands GP15 to the PWM hardware and configures slice 7 |
| `brightness!` | Sets the duty cycle from a percentage |
| `show-brightness` | Prints e.g. `40%` on row 1 |
| `on-press` | Short press: +10 %. Long press: off |
| `dimmer` | Main loop: poll the button until a key is pressed in the REPL |

`pwm-pin` switches GP15 from plain GPIO (what `output-pin` did in
pico2w-hello-led) to the PWM function. After that, `pin!` no longer controls it.

### Printing `40%`

Pictured numeric output builds the string from right to left, so the `%` is
added first:

```forth
brightness @ 0 <# [char] % hold #s #>   ( -- c-addr u )
```

### Shared button code

The debounce and short/long press logic came from pico2w-button-counter and
now lives in [`../lib/button.fs`](../lib/button.fs), so both experiments use
the same words:

| Word | What it does |
|------|--------------|
| `init-button` | GP14 as input with the internal pull-up |
| `debounced-pressed?` | `true` once two readings 20 ms apart say "pressed" |
| `long-press?` | Waits for release; `true` if held for 1 s or more |
