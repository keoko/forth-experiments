#include ../lib/lcd.fs

14 constant button-pin       \ GP14, physical pin 19 — button to GND
20 constant debounce-ms      \ two equal samples this far apart = stable
1000 constant long-press-ms  \ hold at least this long to reset

variable presses

: drain-pressed-keys ( -- )
  begin key? while key drop repeat
;

\ Input with internal pull-up: reads high when released, low when pressed
: init-button ( -- )
  button-pin input-pin
  button-pin pull-up-pin
;

: pressed? ( -- flag ) button-pin pin@ 0= ;

\ Sample the button until two readings debounce-ms apart agree
: debounced-pressed? ( -- flag )
  begin
    pressed?  debounce-ms ms  pressed?
    2dup = if drop true else 2drop false then
  until
;

\ Wait for release; return roughly how long the button was held
: held-ms ( -- ms )
  0 begin debounced-pressed? while debounce-ms + repeat
;

: clear-row ( row -- ) 0 s"                 " lcd-print ;

: show-presses ( -- )
  1 clear-row
  1 0 presses @ 0 <# #s #> lcd-print
;

\ Short press: count it.  Long press: reset to 0
: on-press ( -- )
  held-ms long-press-ms < if presses @ 1+ else 0 then presses !
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
