\ Needs the CYW43 driver and zeptoIP in flash: run `make cyw43-fw` and `make net` once
\ Needs wifi-secrets.fs next to this file: copy wifi-secrets.example.fs and edit it

#include ../pico2w-thermometer/thermometer.fs
#include ../deps/zeptoforth/extra/rp_common/net_tools/pico_w_ipv4_base_no_handler.fs
#include ../deps/zeptoforth/extra/rp_common/net_tools/http_server.fs
#include wifi-secrets.fs

: wifi-hostname ( -- c-addr u ) s" pico.local" ;
80 constant http-port

\ WiFi power saving.  zeptoforth's init-wifi uses PM_AGGRESSIVE, which can miss
\ the router's DHCP offer on some networks; PM_PERFORMANCE is a middle ground
cyw43-consts::PM_NONE value wifi-power-mode

\ pico-w-net::init-wifi from zeptoforth's pico_w_ipv4_base_no_handler.fs, with
\ the power-management mode as a parameter.  Same imports as the original, kept
\ inside a module so they don't clash with the thermometer's words.
begin-module wifi-join
  oo import
  cyw43-events import
  cyw43-control import
  cyw43-structs import
  net-misc import
  frame-process import
  net-consts import
  net-config import
  net import
  net-ipv4 import
  endpoint-process import
  simple-cyw43-net-ipv4 import
  pico-w-cyw43-net-ipv4 import
  pico-w-net import

  : rejoin ( -- )
    begin ssid@ pass@ my-cyw43-control @ join-cyw43-wpa2 nip until
  ;

  \ Rejoin whenever the access point drops us
  : watch-disassoc ( -- )
    begin
      1000 ms
      my-event my-cyw43-control @ get-cyw43-event
      my-event evt-event-type @ EVENT_DISASSOC =
      my-event evt-event-type @ EVENT_DISASSOC_IND = or if rejoin then
    again
  ;

  : init-wifi-pm { D: ssid D: pass D: hostname pm -- }
    ssid validate-ssid
    pass validate-pass
    hostname net::validate-dns-name
    ssid saved-ssid cstring!
    pass saved-pass cstring!
    sm-index pio-instance <pico-w-cyw43-net-ipv4> my-cyw43-net init-object
    my-cyw43-net cyw43-control@ my-cyw43-control !
    my-cyw43-net net-interface@ my-interface !
    my-cyw43-net init-cyw43-net-no-handler
    pm my-cyw43-control @ cyw43-power-management!
    rejoin
    my-cyw43-control @ disable-all-cyw43-events
    my-cyw43-control @ clear-cyw43-events
    0 ['] watch-disassoc 512 128 1024 task::spawn
    c" cyw43-event-rx" over task::task-name!
    task::run
    EVENT_DISASSOC my-cyw43-control @ cyw43-control::enable-cyw43-event
    EVENT_DISASSOC_IND my-cyw43-control @ cyw43-control::enable-cyw43-event
    my-cyw43-net run-net-process
    cr ." Discovering IPv4 address..."
    my-interface @ discover-ipv4-addr
    my-interface @ intf-ipv4-addr@ cr ." IPv4 address: " ipv4.
    my-interface @ intf-ipv4-netmask@ cr ." IPv4 netmask: " ipv4.
    my-interface @ gateway-ipv4-addr@ cr ." Gateway IPv4 address: " ipv4.
    my-interface @ dns-server-ipv4-addr@ cr ." DNS server IPv4 address: " ipv4.
    true my-interface @ mdns-enabled!
    hostname my-interface @ mdns-hostname!
    my-cyw43-net toggle-pico-w-led
  ;
end-module

\ 246 -> prints "24.6"
: .tenths ( n -- ) dup abs 0 <# #tenths rot sign #> type ;

\ The page at http://pico.local/ ; ." output goes to the browser here
: serve-temp ( -- )
  s" text/html" net-http::http-ok.
  ." <html><head><title>Pico thermometer</title>" cr
  .\" <meta http-equiv=\"refresh\" content=\"5\"></head>" cr
  ." <body><h1>" tenths @ .tenths ." &deg;C</h1>" cr
  ." <p>ADC reading: " raw @ . ." </p></body></html>" cr
;

\ zeptoIP's log goes to the hardware UART (GP0/GP1) by default; send it to USB
: usb-debug-hook ( xt -- ) [: execute flush-console ;] usb::with-usb-output ;

\ Run before start-wifi-thermometer to watch DHCP in the REPL
: debug-dhcp ( -- )
  ['] usb-debug-hook to net-config::debug-hook
  true to net-config::dhcp-log?
;

: start-wifi-thermometer ( -- )
  start-thermometer
  cr ." Joining WiFi..."
  wifi-ssid wifi-pass wifi-hostname wifi-power-mode wifi-join::init-wifi-pm
  http-port pico-w-net::my-interface @ net-http::start-server
  ['] serve-temp s" /" net-http::register-fixed-uri
  cr ." Open http://pico.local/ or the IPv4 address above"
;
