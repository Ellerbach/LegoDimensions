#include "xsm3_debug_log.h"

#include <string.h>

#include "pico/critical_section.h"
#include "pico/stdio/driver.h"

#define LOG_BUF_SIZE 512

static char log_buf[LOG_BUF_SIZE];
static size_t log_len;
static critical_section_t log_lock;

// Appends to the ring buffer, dropping the oldest bytes once full so the
// most recent diagnostics (the ones relevant to the last handshake attempt)
// always survive.
static void log_out_chars(const char *buf, int len) {
    critical_section_enter_blocking(&log_lock);
    for (int i = 0; i < len; i++) {
        if (log_len >= sizeof(log_buf) - 1) {
            size_t drop = sizeof(log_buf) / 4;
            memmove(log_buf, log_buf + drop, log_len - drop);
            log_len -= drop;
        }
        log_buf[log_len++] = buf[i];
    }
    critical_section_exit(&log_lock);
}

static stdio_driver_t xsm3_log_driver = {
    .out_chars = log_out_chars,
};

void xsm3_debug_log_init(void) {
    critical_section_init(&log_lock);
    log_len = 0;
    stdio_set_driver_enabled(&xsm3_log_driver, true);
}

size_t xsm3_debug_log_get(char *out, size_t max_len) {
    if (max_len == 0) return 0;
    critical_section_enter_blocking(&log_lock);
    size_t n = log_len < max_len - 1 ? log_len : max_len - 1;
    memcpy(out, log_buf, n);
    critical_section_exit(&log_lock);
    out[n] = '\0';
    return n;
}
