#include ../lib/lcd.fs
#include ../lib/button.fs

pwm import

15 constant led-pin
7 constant led-slice     \ GP15 is PWM slice 7, output B
999 constant led-top     \ counter runs 0-999: 1000 steps per period
100 constant led-div     \ 150 MHz / 100 / 1000 = 1.5 kHz, too fast to see flicker
10 constant step-percent

variable brightness      \ 0-100 %

: drain-pressed-keys ( -- )
  begin key? while key drop repeat
;

: init-pwm-led ( -- )
  led-slice bit disable-pwm
  led-pin pwm-pin
  0 led-slice pwm-counter!
  led-top led-slice pwm-top!
  0 led-div led-slice pwm-clock-div!
  led-slice free-running-pwm
  0 led-slice pwm-counter-compare-b!
  led-slice bit enable-pwm
;

\ Output is high while counter < compare, so compare = top+1 is fully on
: brightness! ( percent -- )
  dup brightness !
  led-top 1+ * 100 / led-slice pwm-counter-compare-b!
;

: show-brightness ( -- )
  1 lcd-clear-row
  1 0 brightness @ 0 <# [char] % hold #s #> lcd-print
;

\ Short press: +10 %, wrapping 100 -> 0.  Long press: off
: on-press ( -- )
  long-press? if
    0
  else
    brightness @ step-percent +  dup 100 > if drop 0 then
  then
  brightness!
  show-brightness
;

: dimmer ( -- )
  init-lcd
  init-button
  init-pwm-led
  drain-pressed-keys
  0 brightness!
  0 0 s" brightness:" lcd-print
  show-brightness
  begin
    debounced-pressed? if on-press then
  key? until
;
