\ HD44780 / LCD1602A driver, 4-bit parallel mode
\ Wiring: see pico2w-hello-lcd/README.md

pin import

16 constant rs-pin   \ GP16, physical pin 21 — Register Select
17 constant en-pin   \ GP17, physical pin 22 — Enable
18 constant d4-pin   \ GP18, physical pin 24 — Data bit 4
19 constant d5-pin   \ GP19, physical pin 25 — Data bit 5
20 constant d6-pin   \ GP20, physical pin 26 — Data bit 6
21 constant d7-pin   \ GP21, physical pin 27 — Data bit 7

: init-pins ( -- )
  rs-pin output-pin  en-pin output-pin
  d4-pin output-pin  d5-pin output-pin
  d6-pin output-pin  d7-pin output-pin
  low en-pin pin!
  low rs-pin pin!
;

\ Write the lower 4 bits of n to data pins D4-D7
: nibble! ( n -- )
  dup 1 and d4-pin pin!
  dup 2 and d5-pin pin!
  dup 4 and d6-pin pin!
      8 and d7-pin pin!
;

\ Strobe Enable to latch a nibble
: pulse-en ( -- )
  high en-pin pin!  1 ms
  low  en-pin pin!  1 ms
;

\ Send one byte in 4-bit mode.  rs: 0=command, non-zero=data
: send-byte ( byte rs -- )
  rs-pin pin!
  dup $f0 and 4 rshift nibble! pulse-en
      $0f and         nibble! pulse-en
;

: cmd!  ( b -- ) 0 send-byte ;
: data! ( b -- ) 1 send-byte ;

\ Clear the display and return the cursor home
: lcd-clear ( -- )
  $01 cmd!  2 ms              \ clear display (needs >1.52 ms)
;

\ HD44780 power-on initialisation for 4-bit mode
: init-lcd ( -- )
  init-pins
  40 ms                       \ wait >40 ms after power-on
  $3 nibble! pulse-en  5 ms  \ 8-bit reset 1 (>4.1 ms)
  $3 nibble! pulse-en  1 ms  \ 8-bit reset 2
  $3 nibble! pulse-en  1 ms  \ 8-bit reset 3
  $2 nibble! pulse-en  1 ms  \ switch to 4-bit mode
  $28 cmd!                    \ function set: 4-bit, 2 lines, 5×8
  $08 cmd!                    \ display off
  lcd-clear
  $06 cmd!                    \ entry mode: left-to-right, no shift
  $0C cmd!                    \ display on, cursor off, blink off
;

\ Move cursor to row (0-1) and col (0-15)
: goto ( row col -- )
  swap 0= if $80 or else $C0 or then cmd!
;

\ Print a string at the given row and column
: lcd-print ( row col c-addr u -- )
  { row col c-addr u }
  row col goto
  u 0 ?do c-addr i + c@ data! loop
;

\ Blank out a whole row (0-1)
: lcd-clear-row ( row -- ) 0 s"                 " lcd-print ;
