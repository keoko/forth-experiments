# Button counter — debounced push button on zeptoforth

Counts presses of a push button and shows the total on the LCD1602A from
[pico2w-hello-lcd](../pico2w-hello-lcd/). A short press adds one; holding the
button for a second or more resets the count to 0.

## Circuit

Keep the LCD wired exactly as in [pico2w-hello-lcd](../pico2w-hello-lcd/README.md#circuit)
and add one push button:

### Schematic

```
                 Pico 2W (inside the RP2350)
                 ┌───────────────────────────┐
                 │   3V3                     │
                 │    │                      │
                 │   [R] internal pull-up    │
                 │    │   (~50 kΩ)           │
 pin 19  GP14 ───┼────┴──► pin@ reads:       │
     │           │         released = high   │
     │           │         pressed  = low    │
     │           └───────────────────────────┘
   ──┴──
   ─────  push button (normally open)
     │
 pin 18  GND
```

When the button is released, the pull-up holds GP14 at 3.3 V. Pressing it
connects GP14 straight to GND, so the pin reads low. That's why `pressed?` is
`button-pin pin@ 0=`. No external resistor is needed: `init-button` turns on the
internal pull-up.

### Where the pins are

Pico 2W seen from above, USB at the top. GP14 and GND are side by side near the
bottom of the left-hand row, right above the LED pin from pico2w-hello-led:

```
              ┌──[USB]──┐
   GP0   1 ●──┤         ├──● 40  VBUS  ← LCD VDD
          …   │         │   …
   GP13 17 ●──┤         ├──● 24  GP18  ← LCD D4
   GND  18 ●──┤ Pico 2W ├──● 23  GND
   GP14 19 ●──┤         ├──● 22  GP17  ← LCD E
   GP15 20 ●──┤         ├──● 21  GP16  ← LCD RS
              └─────────┘
     │    │
     │    └── button leg A
     └─────── button leg B (diagonally opposite A)
```

### On the breadboard

A 4-leg tactile button has its legs in two internally connected pairs. Put the
button across the centre gap and use two **diagonally opposite** legs: those are
always on different pairs, whichever way the button is turned.

```
        column:  e   f        (centre gap between e and f)
                 │ ░ │
   row 10   ─────●─░─●─────
                 │┌─┐│
                 ││ ││  button straddles the gap
                 │└─┘│
   row 12   ─────●─░─●─────
                 │ ░ │

   jumper: Pico pin 19 (GP14) → row 10, column a–e  (top-left leg)
   jumper: Pico pin 18 (GND)  → row 12, column f–j  (bottom-right leg)
```

If the count never changes, the two legs you used are either always connected
or never connected. Rotate the button 90°, or check with a multimeter which legs
close when it's pressed.

## Files

- `button-counter.fs` — the counter
- [`../lib/lcd.fs`](../lib/lcd.fs) — the shared HD44780 driver
- [`../lib/button.fs`](../lib/button.fs) — the shared debounced button: everything
  except `show-presses`, `on-press` and `counter` in the table below

## Run

From the repository root:

```bash
make load LOAD_FILE=pico2w-button-counter/button-counter.fs
make repl
```

At the REPL:

```forth
counter
```

The LCD shows:

```
presses:
0
```

Press the button to count, hold it for a second to reset. Press any key in the
REPL to stop.

## How it works

| Word | What it does |
|------|--------------|
| `init-button` | Sets GP14 as input with the internal pull-up |
| `pressed?` | `true` while the button is down (pin reads low) |
| `debounced-pressed?` | Samples twice, 20 ms apart, until both readings agree |
| `held-ms` | Waits for release and returns roughly how long the button was held |
| `long-press?` | `held-ms` compared with 1 s: `true` for a long press |
| `on-press` | Short press: count + 1. Long press: reset to 0 |
| `show-presses` | Clears row 1 (`lcd-clear-row`) and prints the count |
| `counter` | Main loop: poll the button until a key is pressed in the REPL |

### Debouncing

A mechanical button doesn't switch cleanly: for a few milliseconds the contacts
bounce and the pin flips between high and low. Read naively, one press counts
as several. `debounced-pressed?` only trusts a reading once it has been the
same 20 ms later.

The count changes on **release**, not on press — that is the only moment the
code knows whether it was a short press or a long one.

### Printing numbers

The LCD only takes characters, so the count is turned into a string with Forth's
pictured numeric output:

```forth
presses @ 0 <# #s #>   ( -- c-addr u )
```

`<#` starts building a string, `#s` converts all digits of the double-cell
number `presses @ 0`, and `#>` returns the finished string, ready for `lcd-print`.

### Why `presses` and not `count`

`count` is a standard Forth word (`( c-addr -- c-addr+1 u )`, it reads a counted
string). Redefining it would shadow the built-in for any code loaded after this file.
