#ifndef WEB_SERVER_H
#define WEB_SERVER_H

#include "wifi_settings.h"

#ifdef __cplusplus
extern "C" {
#endif

void web_server_init(void);
void web_server_set_setup_mode(int enabled);
void web_server_set_settings(const wifi_settings_t *settings);
void web_server_task(void);

#ifdef __cplusplus
}
#endif

#endif
