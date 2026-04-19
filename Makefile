LOAD_FILE ?= pico2w-hello-lcd/hello-lcd.fs

.PHONY: tty load repl

tty:
	@link=$$(ls /dev/serial/by-id/ 2>/dev/null | grep -i zeptoforth | head -1); \
	if [ -n "$$link" ]; then \
		readlink -f /dev/serial/by-id/"$$link"; \
	else \
		echo "No Pico found. Is it plugged in?" >&2; \
		exit 1; \
	fi

load:
	sudo deps/zeptoforth/utils/codeload3.sh -B 115200 -p $$($(MAKE) -s --no-print-directory tty) serial $(LOAD_FILE)

repl:
	sudo screen $$($(MAKE) -s --no-print-directory tty) 115200
