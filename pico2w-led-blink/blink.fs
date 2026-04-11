oo import
cyw43-control import
<cyw43-control> class-size buffer: my-ctrl
default-mac-addr
cyw43-clm::data cyw43-clm::size
cyw43-fw::data cyw43-fw::size
23 29 24 25 -1 -1
<cyw43-control> my-ctrl init-object
my-ctrl init-cyw43
: blink
  begin key? while key drop repeat
  begin
    true 0 my-ctrl cyw43-gpio!
    500 ms
    false 0 my-ctrl cyw43-gpio!
    500 ms
  key? until
;
