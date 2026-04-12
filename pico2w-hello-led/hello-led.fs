pin import

15 output-pin

: blink
  begin key? while key drop repeat
  begin
    high 15 pin!
    2000 ms
    low 15 pin!
    2000 ms
  key? until
;
