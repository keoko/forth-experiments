#include ../lib/lcd.fs

adc import
task import

16 constant samples          \ readings averaged per update, to smooth out noise
1000 constant update-ms

variable raw                 \ latest averaged ADC reading, 0-4095
variable tenths              \ latest temperature in tenths of °C: 246 = 24.6 °C
variable thermometer-task

\ Average several readings of the internal temperature sensor
: read-raw ( -- u )
  0  samples 0 ?do temp-adc-chan default-adc adc@ + loop  samples /
;

\ Fixed-point (S31.32) from here: 3,3 is 3.3, and each value takes two cells

\ ADC counts to volts: 0-4095 spans 0-3.3 V
: raw>volts ( u -- f ) s>f 3,3 f* 4096,0 f/ ;

\ RP2350 datasheet: T = 27 - (V - 0.706) / 0.001721
: volts>celsius ( f -- f ) 0,706 d- 0,001721 f/ 27,0 2swap d- ;

: celsius>tenths ( f -- n ) 10,0 f* round-half-even ;

\ Inside <# ... #>: the digits of a tenths value, e.g. 246 -> "24.6"
: #tenths ( ud -- ud ) # [char] . hold #s ;

\ 246 -> "24.6°C"; $DF is the degree sign in the HD44780 character set
: tenths>string ( n -- c-addr u )
  dup abs 0 <# [char] C hold $DF hold #tenths rot sign #>
;

: update ( -- )
  read-raw dup raw !
  raw>volts volts>celsius celsius>tenths tenths !
;

: show-temp ( -- )
  0 lcd-clear-row
  0 0 s" temp:" lcd-print
  0 6 tenths @ tenths>string lcd-print
  1 lcd-clear-row
  1 0 s" adc:" lcd-print
  1 6 raw @ 0 <# #s #> lcd-print
;

: thermometer-loop ( -- )
  begin
    update
    show-temp
    update-ms ms
  again
;

: start-thermometer ( -- )
  init-lcd
  0 ['] thermometer-loop 256 256 512 spawn
  dup thermometer-task !
  run
;
