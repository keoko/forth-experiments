# Button IRQ — counting presses with a GPIO interrupt

Counts button presses like [pico2w-button-counter](../pico2w-button-counter/),
but nothing polls the pin. The button press itself runs the code: the RP2350
raises an **interrupt** on each edge of GP14, a short handler counts it, and a
display task that sleeps the rest of the time wakes up to update the LCD.

The LCD shows two numbers:

```
presses: 7
edges:   9
```

- **presses**: real button presses, after debouncing
- **edges**: every falling edge the hardware detected, contact bounce included

When `edges` grows faster than `presses`, you are seeing the bounce that the
polling version hid by sampling every 20 ms.

## Circuit

Same as [pico2w-button-counter](../pico2w-button-counter/README.md#circuit):
button between GP14 (pin 19) and GND, LCD on GP16–GP21.

## Files

- `button-irq.fs` — interrupt handler, display task, start/stop words
- [`../lib/lcd.fs`](../lib/lcd.fs) — the shared HD44780 driver
- [`../lib/button.fs`](../lib/button.fs) — only `button-pin` and `init-button`
  are used here; the polling words are not

## Run

If another experiment has run since the Pico last booted, especially
[pico2w-dimmer-task](../pico2w-dimmer-task/), type `reboot` in the REPL first.
A leftover background task will fight the display task over the LCD.

From the repository root:

```bash
make load LOAD_FILE=pico2w-button-irq/button-irq.fs
make repl
```

At the REPL:

```forth
start-irq-counter
```

The prompt comes straight back; press the button and watch the LCD. The counts
are ordinary variables, so you can read them from the REPL too:

```forth
presses @ .  edges @ .
```

To stop:

```forth
stop-irq-counter
```

`reboot` also stops everything.

## How it works

### Polling vs. interrupts

| | pico2w-button-counter (polling) | this experiment (interrupt) |
|---|---|---|
| Who notices the press | a loop reading the pin every 20 ms | the GPIO hardware, instantly |
| CPU while idle | busy checking the pin | asleep; the display task is blocked |
| Bounces | invisible between samples | every one triggers the handler |

### Setting up the interrupt

`start-irq-counter` does it in four steps:

```forth
['] button-handler io-irq-bank0-vector vector!   \ 1. install the handler
button-pin INTR_GPIO_EDGE_LOW!                   \ 2. clear stale edges
true button-pin PROC0_INTE_GPIO_EDGE_LOW!        \ 3. GP14 falling edge → interrupt
IO_IRQ_BANK0 NVIC_ISER_SETENA!                   \ 4. let the CPU take that interrupt
```

1. All GPIOs in bank 0 share one interrupt line, `IO_IRQ_BANK0` (IRQ 21 on the
   RP2350). `vector!` puts our handler in the CPU's vector table. The table's
   first 16 entries are for CPU exceptions, so IRQ *n* is entry *n* + 16.
2. The GPIO block latches edges even while interrupts are off. Clearing them
   first means an old edge can't fire the moment we enable.
3. `PROC0_INTE_...` enables the event for core 0. It's enabled for both
   directions: falling (press) and rising (release).
4. The NVIC is the ARM interrupt controller; `NVIC_ISER_SETENA!` unmasks IRQ 21.

### The handler

`button-handler` runs *in the middle of whatever the CPU was doing*, so it has
to be quick and must not wait. That rules out `ms` and the LCD (each character
takes milliseconds). It only:

1. checks which event happened (`PROC0_INTS_GPIO_EDGE_LOW@` / `..._HIGH@`),
2. acknowledges it (`INTR_GPIO_EDGE_LOW!`), otherwise it would fire again
   immediately and the CPU would never leave the handler,
3. updates the counters,
4. wakes the display task with `notify`.

### Debouncing with timestamps

Polling debounced by sampling slowly. With interrupts, every bounce runs the
handler, so instead it compares times: `systick-counter` counts in 100 µs ticks,
and `last-edge` remembers the time of the latest edge.

A falling edge counts as a press only if the line has been **quiet** for 50 ms
before it:

| Time | Edge | Quiet for 50 ms before? | Counted as |
|------|------|-------------------------|------------|
| 0 ms | falling (press) | yes | **press**, edge |
| 1 ms | rising (bounce) | — | — |
| 2 ms | falling (bounce) | no, 1 ms | edge only |
| 800 ms | rising (release) | — | — |
| 801 ms | falling (release bounce) | no, 1 ms | edge only |
| 802 ms | rising (bounce) | — | — |

That's why the rising edges are watched too. Without them, the bounce on
*release* comes after a long quiet period (the button was held down) and would
count as a second press.

`systick-counter last-edge @ -` stays correct when the counter wraps around,
because unsigned subtraction wraps the same way.

### The display task and notifications

The LCD is updated by `display-loop`, a task that spends almost all its time in
`wait-notify-indefinite`: blocked, using no CPU. The handler wakes it with
`notify`; it redraws both rows and goes back to sleep.

Notifications don't queue up. If several edges arrive while the task is still
redrawing, they collapse into one wake-up, and the next redraw shows the latest
counts. Nothing is lost because the numbers live in the variables, not in the
notification.

`config-notify` gives the task its one mailbox (`mailbox`); it must run before
the first `notify`, which is why the interrupt is enabled last.
