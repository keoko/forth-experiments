# Standalone — run an experiment from flash at every boot

Until now, everything was loaded into RAM, which is why every power cycle
needed a `make load`. This experiment compiles
[pico2w-button-irq](../pico2w-button-irq/) into **flash** and starts it
automatically at boot. Plug the Pico into a USB charger, without the PC, and the
button counter just works.

## Circuit

Same as [pico2w-button-irq](../pico2w-button-irq/README.md#circuit): button on
GP14, LCD on GP16–GP21.

## Install, use, remove

From the repository root, with the REPL closed:

```bash
make flash      # reboot, compile standalone.fs into flash, reboot again
```

After the second reboot the LCD shows `presses: 0` / `edges: 0` and the button
counts, all without typing anything. Unplug it, plug it into any USB power, and
it starts again.

The REPL still works while the app runs (`make repl`, then e.g. `presses @ .`),
because the app runs in a background task and an interrupt.

To remove the app and get a plain zeptoforth back:

```bash
make unflash    # runs forget-app, which erases the app from flash and reboots
```

**Run `make unflash` before `make flash` again**, and before using the other
experiments: otherwise the app starts at every boot and fights them for the
LCD and the button (see [Working with the REPL](../README.md#working-with-the-repl)).

## Safe mode

If the app ever misbehaves at boot, **hold the button while plugging in the
Pico**. `start-app` sees it pressed and returns without starting anything, so
you get a plain REPL and can run `make unflash`.

Last resort, if even that fails: flash the zeptoforth UF2 again (see the
[root README](../README.md#flash-zeptoforth)). That erases everything in flash,
including the CYW43 firmware blobs, so redo `make cyw43-fw` afterwards if you
use pico2w-led-blink.

## Files

- `standalone.fs` — compiles the button IRQ counter to flash and registers it
  to run at boot
- [`../pico2w-button-irq/button-irq.fs`](../pico2w-button-irq/button-irq.fs) — the app itself, unchanged

## How it works

### RAM vs. flash

| | RAM (`make load`) | flash (`make flash`) |
|---|---|---|
| After a power cycle | gone | still there |
| To remove | `reboot` | `make unflash` (or reflash zeptoforth) |
| Good for | trying things | a finished program |

zeptoforth compiles new words into RAM by default. `compile-to-flash` switches
to flash, so every definition after it is written there; `compile-to-ram`
switches back. Variables defined in flash still live in RAM (flash can't be
written byte by byte at runtime); only their definitions are in flash.

### Running at boot: `initializer`

At every boot zeptoforth runs its `init` chain (USB console, tasks, ...), then
shows the `ok` prompt. `initializer start-app` adds `start-app` to the end of
that chain.

The older way to do this is redefining `init` yourself:

```forth
: init init start-app ;   \ call the previous init first, then ours
```

Forget the inner `init` and zeptoforth's own startup never runs: no USB console,
no REPL. That's why [pico2w-hello-led](../pico2w-hello-led/) calls its setup word
`init-led` and not `init`. `initializer` does the chaining for you.

### Removing it again: `marker`

```forth
marker forget-app
```

creates a word `forget-app` that remembers the state of the dictionary at that
point. Running it erases everything defined after it, including itself, and
because this is flash, reboots. Everything defined *before* the marker survives,
which matters here: the CYW43 firmware blobs from `make cyw43-fw` are in flash
too, and `make unflash` leaves them alone.

### The Makefile targets

| Target | What it does |
|--------|--------------|
| `make flash` | `make reboot`, then loads `standalone.fs`, then `make reboot` |
| `make unflash` | sends `forget-app` to the REPL and waits for the reboot |
| `make reboot` | sends `reboot` and waits until the serial port is back |
| `make send CMD='...'` | sends one line to the REPL without opening `screen` |

The first reboot matters: flash code must not use words that only exist in RAM,
and rebooting clears anything left over from earlier `make load`s.
