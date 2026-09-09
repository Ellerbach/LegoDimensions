#ifndef XSM3_DEBUG_LOG_H
#define XSM3_DEBUG_LOG_H

#include <stddef.h>

#ifdef __cplusplus
extern "C" {
#endif

// Captures libxsm3's own printf() diagnostics (e.g. "Checksum failed when
// validating challenge init!", "MAC failed...") into a buffer we can expose
// over the web API, instead of requiring a wired UART capture to see them.
void xsm3_debug_log_init(void);

// Copies the captured log text (nul-terminated) into out, up to max_len
// bytes including the terminator. Returns the number of bytes written,
// excluding the terminator.
size_t xsm3_debug_log_get(char *out, size_t max_len);

#ifdef __cplusplus
}
#endif

#endif
