# Dimmer task — the LED dimmer as a background task

The same dimmer as [pico2w-led-dimmer](../pico2w-led-dimmer/) (button steps the
LED brightness, LCD shows it), but running in its own zeptoforth **task**. The
REPL stays free while it runs, so you can inspect and change the brightness
from the keyboard while the button keeps working.

## Circuit

Exactly as in [pico2w-led-dimmer](../pico2w-led-dimmer/README.md#circuit): LED on
GP15, button on GP14, LCD on GP16–GP21.

## Files

- `dimmer-task.fs` — the task; builds on the dimmer with
  `#include ../pico2w-led-dimmer/led-dimmer.fs`
- [`../pico2w-led-dimmer/led-dimmer.fs`](../pico2w-led-dimmer/led-dimmer.fs) — PWM setup,
  `brightness!`, `show-brightness`, `next-brightness`
- [`../lib/lcd.fs`](../lib/lcd.fs), [`../lib/button.fs`](../lib/button.fs) — shared drivers

## Run

From the repository root:

```bash
make load LOAD_FILE=pico2w-dimmer-task/dimmer-task.fs
make repl
```

At the REPL:

```forth
start-dimmer
```

You get the `ok` prompt straight back, and the button works. Now try:

```forth
brightness @ .         \ current level, e.g. 30
75 brightness !        \ set it from the REPL: LED and LCD follow
dimmer-task @ stop     \ pause the task: the button stops responding
dimmer-task @ run      \ resume it
dimmer-task @ kill     \ end it for good (start-dimmer starts a new one)
```

`reboot` also ends it, along with everything else in RAM. **Reboot before
loading another experiment**: the task keeps running otherwise, and keeps
writing to the LCD and reacting to the button.

## How it works

### Tasks

zeptoforth runs the REPL as the *main task*. `spawn` creates another one:

```forth
0 ['] dimmer-loop 256 256 512 spawn   ( -- task )
```

| Argument | Meaning |
|----------|---------|
| `0` | number of values to pass to the task (none) |
| `['] dimmer-loop` | the word the task runs |
| `256 256 512` | dictionary, data stack and return stack sizes in bytes |

`spawn` only creates the task; `run` starts it. The task header is kept in
`dimmer-task` so you can `stop`, `run` or `kill` it later. `stop` and `run`
nest: two `stop`s need two `run`s.

The scheduler is **preemptive**: it can switch tasks at any moment. But a task
that waits should give the CPU away, and `ms` does exactly that. `dimmer-loop`
calls `ms` every 20 ms inside `debounced-pressed?`, so the REPL stays responsive.

Compare with [pico2w-led-dimmer](../pico2w-led-dimmer/): there, `dimmer` runs in
the main task, so it has to poll `key?` to let you stop it, and the REPL is
blocked until you do.

### One owner for the hardware

Two tasks are now running: the REPL and the dimmer. If both wrote to the LCD,
a task switch could land in the middle of a character (between the two nibbles
of `send-byte`), and the display would show garbage.

So the design is: **only the dimmer task touches the PWM and the LCD**.
Everyone else communicates through the `brightness` variable:

```
 REPL task                     dimmer task (loops every ~20 ms)
 ─────────                     ───────────
 75 brightness !  ──────────►  brightness ≠ shown?
                               yes → brightness! (PWM), show-brightness (LCD)
                                     shown = brightness
```

`apply-brightness` compares `brightness` with `shown` (the last value it put on
the hardware) and only redraws when they differ. A single `!` of one cell is
atomic, so the REPL can never leave `brightness` half-written. The worst case is
pressing the button at the same moment as you type a new value: one of the two
wins, and the display shows which.

For cases where two tasks really must share a device, zeptoforth has locks (the
`lock` module), but "one owner, communicate through variables" is simpler and
can't deadlock.

### `!` versus `brightness!`

| Word | What it does | Safe from the REPL? |
|------|--------------|---------------------|
| `brightness !` | stores into the variable | yes — the task applies it |
| `brightness!` | stores **and** writes the PWM register directly | works, but bypasses the owner rule |

Use `brightness !` from the REPL.
