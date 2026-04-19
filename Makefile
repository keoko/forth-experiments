# Default Forth file to load onto the Pico. Override with: make load LOAD_FILE=<path>
LOAD_FILE ?= pico2w-hello-lcd/hello-lcd.fs

.PHONY: tty load repl

# Print the /dev/ttyACM* path of the connected Pico (detected by zeptoforth USB ID).
tty:
	@link=$$(ls /dev/serial/by-id/ 2>/dev/null | grep -i zeptoforth | head -1); \
	if [ -n "$$link" ]; then \
		readlink -f /dev/serial/by-id/"$$link"; \
	else \
		echo "No Pico found. Is it plugged in?" >&2; \
		exit 1; \
	fi

# Load a Forth source file onto the Pico via serial (requires sudo).
#   make load                                         # loads $(LOAD_FILE)
#   make load LOAD_FILE=pico2w-hello-led/hello-led.fs
load:
	sudo deps/zeptoforth/utils/codeload3.sh -B 115200 -p $$($(MAKE) -s --no-print-directory tty) serial $(LOAD_FILE)

# Open the zeptoforth REPL over serial (requires sudo). Exit with Ctrl-A \.
repl:
	sudo screen $$($(MAKE) -s --no-print-directory tty) 115200
