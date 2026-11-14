#include ../lib/lcd.fs
#include ../lib/button.fs

variable presses

: drain-pressed-keys ( -- )
  begin key? while key drop repeat
;

: show-presses ( -- )
  1 lcd-clear-row
  1 0 presses @ 0 <# #s #> lcd-print
;

\ Short press: count it.  Long press: reset to 0
: on-press ( -- )
  long-press? if 0 else presses @ 1+ then presses !
  show-presses
;

: counter ( -- )
  init-lcd
  init-button
  drain-pressed-keys
  0 presses !
  0 0 s" presses:" lcd-print
  show-presses
  begin
    debounced-pressed? if on-press then
  key? until
;
