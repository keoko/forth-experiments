# Default Forth file to load onto the Pico. Override with: make load LOAD_FILE=<path>
LOAD_FILE ?= pico2w-hello-lcd/hello-lcd.fs

ZEPTOFORTH = deps/zeptoforth
# Relative to $(ZEPTOFORTH): its #include lines expect to be loaded from there
CYW43_DRIVER = extra/rp_common/cyw43/cyw43_all.fs
CYW43_FW_DIR = deps/cyw43-firmware/cyw43439-firmware
# CYW43 driver + zeptoIP, compiled to flash by make net (relative to $(ZEPTOFORTH))
NET_STACK = extra/rp_common/pico_w_net_ipv4_all.fs

# App that `make flash` compiles into flash to run at every boot
FLASH_FILE = pico2w-standalone/standalone.fs

.PHONY: tty load cyw43-fw net unnet cyw43 repl send reboot flash unflash wait-for-pico

# Print the /dev/ttyACM* path of the connected Pico (detected by zeptoforth USB ID).
tty:
	@link=$$(ls /dev/serial/by-id/ 2>/dev/null | grep -i zeptoforth | head -1); \
	if [ -n "$$link" ]; then \
		readlink -f /dev/serial/by-id/"$$link"; \
	else \
		echo "No Pico found. Is it plugged in?" >&2; \
		exit 1; \
	fi

# Load a Forth source file onto the Pico via serial.
#   make load                                         # loads $(LOAD_FILE)
#   make load LOAD_FILE=pico2w-hello-led/hello-led.fs
load:
	@port=$$($(MAKE) -s --no-print-directory tty) || exit 1; \
	$(ZEPTOFORTH)/utils/codeload3.sh -B 115200 -p "$$port" serial $(LOAD_FILE)

# Upload the CYW43439 firmware blobs to flash (one-time; redo after reflashing zeptoforth).
cyw43-fw:
	@port=$$($(MAKE) -s --no-print-directory tty) || exit 1; \
	cd $(ZEPTOFORTH) && utils/load_cyw43_fw.sh "$$port" \
		$(CURDIR)/$(CYW43_FW_DIR)/43439A0.bin \
		$(CURDIR)/$(CYW43_FW_DIR)/43439A0_clm.bin

# Compile the CYW43 driver and zeptoIP (IPv4) into flash (one-time, after make cyw43-fw).
# Takes a while. Undo with make unnet. Once installed, make cyw43 is not needed.
net: reboot
	@port=$$($(MAKE) -s --no-print-directory tty) || exit 1; \
	cd $(ZEPTOFORTH) && utils/codeload3.sh -B 115200 -p "$$port" serial $(NET_STACK)
	@$(MAKE) -s --no-print-directory reboot

# Erase the CYW43 driver and zeptoIP installed by make net (runs its erase-net marker).
unnet:
	@$(MAKE) -s --no-print-directory send CMD=erase-net
	@$(MAKE) -s --no-print-directory wait-for-pico

# Load the CYW43 driver (needed by pico2w-led-blink; RAM only, repeat after each power cycle).
cyw43:
	@port=$$($(MAKE) -s --no-print-directory tty) || exit 1; \
	cd $(ZEPTOFORTH) && utils/codeload3.sh -B 115200 -p "$$port" serial $(CYW43_DRIVER)

# Open the zeptoforth REPL over serial. Exit with Ctrl-A \.
repl:
	@port=$$($(MAKE) -s --no-print-directory tty) || exit 1; \
	screen "$$port" 115200

# Send one line to the REPL without opening screen and print the reply:
#   make send CMD='presses @ .'
# The USB console drops input that arrives right as the port opens or closes,
# so keep it open (fd 3), wait 0.5 s (discarding old output such as the boot
# banner), send, and read the reply for 1.5 s.
send:
	@port=$$($(MAKE) -s --no-print-directory tty) || exit 1; \
	exec 3<>"$$port"; \
	stty -F "$$port" 115200 raw -echo; \
	timeout 0.5 cat <&3 >/dev/null; \
	printf '%s\r' "$(CMD)" >&3; \
	timeout 1.5 cat <&3 | tr -d '\006'; \
	echo

# Reboot the Pico and wait until its serial port is back. Sends Ctrl-C rather than
# the word reboot: zeptoforth reboots on Ctrl-C even while busy, e.g. stuck in init-wifi.
reboot:
	@$(MAKE) -s --no-print-directory send CMD="$$(printf '\003')"
	@$(MAKE) -s --no-print-directory wait-for-pico

# Compile $(FLASH_FILE) into flash, then reboot so it starts. Undo with make unflash.
flash: reboot
	@$(MAKE) -s --no-print-directory load LOAD_FILE=$(FLASH_FILE)
	@$(MAKE) -s --no-print-directory reboot

# Erase the app installed by make flash (runs its forget-app marker, which reboots).
unflash:
	@$(MAKE) -s --no-print-directory send CMD=forget-app
	@$(MAKE) -s --no-print-directory wait-for-pico

wait-for-pico:
	@sleep 2; \
	for i in $$(seq 1 20); do \
		$(MAKE) -s --no-print-directory tty >/dev/null 2>&1 && exit 0; \
		sleep 1; \
	done; \
	echo "Pico did not come back after rebooting" >&2; exit 1
