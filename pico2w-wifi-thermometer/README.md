# WiFi thermometer — the temperature on a web page

The Pico joins your WiFi network and serves a web page with the temperature
from [pico2w-thermometer](../pico2w-thermometer/). Open `http://pico.local/` on
any device on the same network:

```
16.8 °C
ADC reading: 898
```

The page refreshes itself every 5 seconds. The LCD keeps showing the
temperature too.

> **Known limitation:** tested working on a phone hotspot. On a TP-Link mesh
> network (two nodes, same SSID on 2.4 and 5 GHz) the Pico joins but never
> receives a DHCP offer, with power saving on or off (`wifi-power-mode`). The
> DHCP log shows `Sending DHCPDISCOVER` / `Timeout, retrying DHCPDISCOVER`
> forever. If that happens to you, use a hotspot or a simpler access point.

## Circuit

Only the LCD, as in [pico2w-hello-lcd](../pico2w-hello-lcd/README.md#circuit).
WiFi uses the Pico 2W's onboard CYW43439 chip.

## One-time setup

From the repository root:

```bash
make cyw43-fw   # CYW43439 firmware blobs into flash (skip if already done)
make net        # CYW43 driver + zeptoIP into flash; takes several minutes
```

`make net` compiles zeptoforth's network stack
(`extra/rp_common/pico_w_net_ipv4_all.fs`) into flash and reboots. It's large,
which is why it goes to flash once instead of RAM at every boot. Remove it with
`make unnet`.

Then create your WiFi credentials file:

```bash
cp pico2w-wifi-thermometer/wifi-secrets.example.fs pico2w-wifi-thermometer/wifi-secrets.fs
```

and put your network name and password in `wifi-secrets.fs`:

```forth
: wifi-ssid ( -- c-addr u ) s" my-network" ;
: wifi-pass ( -- c-addr u ) s" my-password" ;
```

`wifi-secrets.fs` is in `.gitignore`, so it never gets committed. Note that
`make load` prints every line it sends, password included, to your terminal.

## Run

After every boot:

```bash
make load LOAD_FILE=pico2w-wifi-thermometer/wifi-thermometer.fs
make repl
```

At the REPL:

```forth
start-wifi-thermometer
```

Joining can take a few seconds. When it works it prints the Pico's address:

```
Joining WiFi...
Discovering IPv4 address...
IPv4 address: 192.168.1.42
...
Open http://pico.local/ or the IPv4 address above
```

If `pico.local` doesn't resolve on your computer (it needs mDNS: Avahi on Linux,
built into macOS and recent Windows), use the IPv4 address instead.

If it never gets past `Joining WiFi...`, the SSID or password is wrong, or the
signal is too weak. `init-wifi` retries forever and the REPL stops answering
meanwhile, so `make load` fails with `could not upload file`. Press `Ctrl-C` in
`screen` (or run `make reboot`) to reboot the Pico, fix `wifi-secrets.fs`, and
load again.

To see what DHCP is doing when it hangs at `Discovering IPv4 address...`, turn
on zeptoIP's DHCP log before starting:

```forth
debug-dhcp
start-wifi-thermometer
```

`debug-dhcp` does two things: it sets `net-config::dhcp-log?`, and it points
`net-config::debug-hook` at the USB console. By default zeptoIP prints its log
to the hardware UART on GP0/GP1, so with only USB connected, setting
`dhcp-log?` alone shows nothing. The CYW43439 only does
2.4 GHz WiFi, not 5 GHz.

## Files

- `wifi-thermometer.fs` — the page handler and `start-wifi-thermometer`
- `wifi-secrets.example.fs` — template for `wifi-secrets.fs`
- [`../pico2w-thermometer/thermometer.fs`](../pico2w-thermometer/thermometer.fs) — ADC, conversion, LCD task
- From zeptoforth, included directly:
  - `extra/rp_common/net_tools/pico_w_ipv4_base_no_handler.fs` — `init-wifi`: join, get an address by DHCP, mDNS
  - `extra/rp_common/net_tools/http_server.fs` — a small HTTP server

## How it works

### The layers

| Layer | Where it lives | What it does |
|-------|----------------|--------------|
| CYW43439 firmware | flash (`make cyw43-fw`) | runs on the WiFi chip itself |
| CYW43 driver + zeptoIP | flash (`make net`) | talks to the chip; IP, DHCP, TCP, mDNS |
| `init-wifi`, HTTP server | RAM (`make load`) | joins your network, answers browsers |
| `serve-temp` | RAM (`make load`) | the page itself |

zeptoIP runs on the RP2350's second core (core 1). Your code, the REPL and the
thermometer task stay on core 0.

### Serving a page

The HTTP server maps paths to words. `serve-temp` is registered for `/`:

```forth
['] serve-temp s" /" net-http::register-fixed-uri
```

When a browser asks for `/`, the server runs `serve-temp` with its output
redirected to the network connection. That's why the page is written with the
same `."` used to print in the REPL:

```forth
s" text/html" net-http::http-ok.          \ status line and headers
." <body><h1>" tenths @ .tenths ." &deg;C</h1>"
```

`&deg;` is HTML for the degree sign. The LCD uses its own code (`$DF`) for the
same symbol, which is why `.tenths` only prints the digits and each display adds
its own unit: both share `#tenths` from `thermometer.fs`.

### Power saving: `wifi-power-mode`

`start-wifi-thermometer` doesn't call zeptoforth's `pico-w-net::init-wifi`
directly but `wifi-join::init-wifi-pm`, a copy of it that takes the CYW43439's
power-management mode as a parameter instead of hardcoding `PM_AGGRESSIVE`.
`wifi-power-mode` defaults to `PM_NONE` (radio always on), the most reliable
setting for a USB-powered device. To try another mode, before starting:

```forth
cyw43-consts::PM_PERFORMANCE to wifi-power-mode
```

### Module prefixes

`init-wifi` is in the `pico-w-net` module and the HTTP words are in `net-http`,
so they're written with their module name: `pico-w-net::init-wifi`,
`net-http::start-server`. Importing whole modules instead (`net-http import`)
would also work, but with a network stack this large the prefixes make it clear
where each word comes from, and keep its names from clashing with ours.
