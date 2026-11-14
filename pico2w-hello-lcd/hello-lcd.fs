#include ../lib/lcd.fs

: hello ( -- )
  init-lcd
  0 0 s" hello world,"  lcd-print
  1 0 s" zeptoforth!"   lcd-print
;
