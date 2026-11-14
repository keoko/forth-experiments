\ Debounced push button with short/long press detection
\ Wiring: button between GP14 and GND, see pico2w-button-counter/README.md

pin import

14 constant button-pin       \ GP14, physical pin 19 — button to GND
20 constant debounce-ms      \ two equal samples this far apart = stable
1000 constant long-press-ms  \ hold at least this long for a long press

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

\ Call after debounced-pressed? returns true: waits for release
: long-press? ( -- flag ) held-ms long-press-ms >= ;
