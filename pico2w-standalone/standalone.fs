\ Compile the button IRQ counter into flash and start it at every boot.
\ Install with `make flash`, remove with `make unflash`.

compile-to-flash

marker forget-app        \ running forget-app erases everything below and reboots

#include ../pico2w-button-irq/button-irq.fs

\ Safe mode: hold the button while plugging in to boot to a plain REPL
: start-app ( -- )
  init-button
  10 ms                  \ let the pull-up settle before reading
  pressed? if exit then
  start-irq-counter
;

initializer start-app    \ runs start-app at every boot, after zeptoforth's own init

compile-to-ram
