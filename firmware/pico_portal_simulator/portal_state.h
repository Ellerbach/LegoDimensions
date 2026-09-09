#ifndef PORTAL_STATE_H
#define PORTAL_STATE_H

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

#define PORTAL_PAD_COUNT 3
#define PORTAL_MAX_TAGS 7
#define PORTAL_TAG_PAGES 45
#define PORTAL_EVENT_QUEUE_SIZE 16

typedef enum {
    TAG_CHARACTER = 0,
    TAG_VEHICLE = 1,
} tag_kind_t;

typedef struct {
    bool present;
    uint8_t pad;
    uint8_t position;
    uint8_t index;
    uint8_t uid[7];
    tag_kind_t kind;
    uint16_t id;
    uint8_t pages[PORTAL_TAG_PAGES][4];
} portal_tag_t;

typedef struct {
    uint8_t pad;
    uint8_t type;
    uint8_t index;
    bool removed;
    uint8_t uid[7];
} portal_tag_event_t;

typedef struct {
    uint8_t colors[PORTAL_PAD_COUNT][3];
    bool nfc_enabled;
    portal_tag_t tags[PORTAL_MAX_TAGS];
} portal_snapshot_t;

void portal_state_init(void);
bool portal_state_place(uint8_t pad, uint8_t position, tag_kind_t kind, uint16_t id);
bool portal_state_remove(uint8_t index);
bool portal_state_remove_pad(uint8_t pad);
bool portal_state_pop_event(portal_tag_event_t *event);
bool portal_state_read(uint8_t index, uint8_t page, uint8_t out[16]);
bool portal_state_write(uint8_t index, uint8_t page, const uint8_t data[4]);
bool portal_state_get_tag(uint8_t index, tag_kind_t *kind, uint16_t *id);
size_t portal_state_list(uint8_t *out_pairs, size_t max_pairs);
void portal_state_set_color(uint8_t pad, uint8_t red, uint8_t green, uint8_t blue);
void portal_state_get_color(uint8_t pad, uint8_t out[3]);
void portal_state_start_fade(uint8_t pad, uint8_t tick_time, uint8_t tick_count,
    uint8_t red, uint8_t green, uint8_t blue, bool random_color);
void portal_state_start_flash(uint8_t pad, uint8_t tick_on, uint8_t tick_off,
    uint8_t tick_count, uint8_t red, uint8_t green, uint8_t blue);
void portal_state_task(void);
void portal_state_set_nfc(bool enabled);
void portal_state_snapshot(portal_snapshot_t *snapshot);

#ifdef __cplusplus
}
#endif

#endif
