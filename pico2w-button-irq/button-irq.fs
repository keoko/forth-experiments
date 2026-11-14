#include ../lib/lcd.fs
#include ../lib/button.fs

interrupt import
gpio import
task import
systick import

IO_IRQ_BANK0 16 + constant io-irq-bank0-vector  \ vector table: IRQ n is entry n + 16
500 constant quiet-ticks     \ 50 ms in 100 µs SysTick ticks

variable edges               \ every falling edge the hardware saw, bounces included
variable presses             \ falling edges after 50 ms of quiet: real presses
variable last-edge           \ SysTick count of the latest edge, either direction
variable display-task
variable mailbox             \ notification mailbox for display-task

: quiet? ( -- flag ) systick-counter last-edge @ - quiet-ticks u> ;

\ Interrupt handler: keep it short.  No ms, no LCD: just count and wake the task
: button-handler ( -- )
  button-pin PROC0_INTS_GPIO_EDGE_LOW@ if
    button-pin INTR_GPIO_EDGE_LOW!   \ acknowledge, or it fires again at once
    1 edges +!
    quiet? if 1 presses +! then
    systick-counter last-edge !
    0 display-task @ notify
  then
  button-pin PROC0_INTS_GPIO_EDGE_HIGH@ if
    button-pin INTR_GPIO_EDGE_HIGH!
    systick-counter last-edge !      \ release bounce must not look like a press
  then
;

: show-count ( row c-addr u n -- )
  { row c-addr u n }
  row lcd-clear-row
  row 0 c-addr u lcd-print
  row 9 n 0 <# #s #> lcd-print
;

\ Sleeps until the handler notifies it; never polls
: display-loop ( -- )
  begin
    0 s" presses:" presses @ show-count
    1 s" edges:"   edges @   show-count
    0 wait-notify-indefinite drop
  again
;

: start-irq-counter ( -- )
  init-lcd
  init-button
  0 edges !
  0 presses !
  systick-counter quiet-ticks 1+ - last-edge !  \ so the first press counts
  0 ['] display-loop 256 256 512 spawn display-task !
  mailbox 1 display-task @ config-notify
  display-task @ run
  ['] button-handler io-irq-bank0-vector vector!
  button-pin INTR_GPIO_EDGE_LOW!     \ drop any edge latched before now
  button-pin INTR_GPIO_EDGE_HIGH!
  true button-pin PROC0_INTE_GPIO_EDGE_LOW!
  true button-pin PROC0_INTE_GPIO_EDGE_HIGH!
  IO_IRQ_BANK0 NVIC_ISER_SETENA!
;

: stop-irq-counter ( -- )
  false button-pin PROC0_INTE_GPIO_EDGE_LOW!
  false button-pin PROC0_INTE_GPIO_EDGE_HIGH!
  display-task @ kill
;
