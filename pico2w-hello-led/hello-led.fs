pin import

15 constant led-pin
2000 constant blink-delay

: drain-pressed-keys ( -- )
  begin key? while key drop repeat
;

: init-led ( -- )
  led-pin output-pin
;

: blink ( -- )
  init-led
  drain-pressed-keys
  begin
    high led-pin pin!
    blink-delay ms
    low led-pin pin!
    blink-delay ms
  key? until
;
