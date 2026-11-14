#include ../pico2w-led-dimmer/led-dimmer.fs

task import

variable shown           \ brightness last applied to the PWM and LCD
variable dimmer-task     \ task header, for stop / run / kill

\ Only the dimmer task touches the PWM and LCD.  Anyone else (e.g. the REPL)
\ just stores a new value: 75 brightness !
: apply-brightness ( -- )
  brightness @ dup shown @ <> if
    dup shown !
    brightness!
    show-brightness
  else
    drop
  then
;

\ Runs forever in its own task; ms inside debounced-pressed? lets other tasks run
: dimmer-loop ( -- )
  begin
    debounced-pressed? if long-press? next-brightness brightness ! then
    apply-brightness
  again
;

: start-dimmer ( -- )
  init-lcd
  init-button
  init-pwm-led
  0 0 s" brightness:" lcd-print
  -1 shown !             \ force the first apply-brightness to draw
  0 brightness !
  0 ['] dimmer-loop 256 256 512 spawn
  dup dimmer-task !
  run
;
