#include "portal_state.h"

#include <string.h>

#include "pico/critical_section.h"
#include "pico/rand.h"
#include "pico/time.h"

static portal_snapshot_t state;
static portal_tag_event_t events[PORTAL_EVENT_QUEUE_SIZE];
static uint8_t event_head;
static uint8_t event_tail;
static critical_section_t state_lock;

typedef enum {
    EFFECT_NONE,
    EFFECT_FADE,
    EFFECT_FLASH,
} effect_type_t;

typedef struct {
    effect_type_t type;
    uint8_t from[3];
    uint8_t to[3];
    uint8_t remaining;
    bool forever;
    bool showing_color;
    uint32_t phase_started_ms;
    uint32_t on_ms;
    uint32_t off_ms;
} portal_effect_t;

static portal_effect_t effects[PORTAL_PAD_COUNT];

static uint32_t rotate_right(uint32_t value, unsigned count) {
    return (value >> count) | (value << (32u - count));
}

static uint32_t read_u32_le(const uint8_t *data) {
    return (uint32_t)data[0] | ((uint32_t)data[1] << 8) |
        ((uint32_t)data[2] << 16) | ((uint32_t)data[3] << 24);
}

static void write_u32_le(uint8_t *data, uint32_t value) {
    data[0] = (uint8_t)value;
    data[1] = (uint8_t)(value >> 8);
    data[2] = (uint8_t)(value >> 16);
    data[3] = (uint8_t)(value >> 24);
}

static uint32_t mix_uid(uint32_t value) {
    value ^= value >> 16;
    value *= 0x7feb352du;
    value ^= value >> 15;
    value *= 0x846ca68bu;
    return value ^ (value >> 16);
}

static void generate_uid(tag_kind_t kind, uint16_t id, uint8_t uid[7]) {
    uint32_t seed = 0x4c445447u ^ id ^ ((uint32_t)kind << 24);
    uint32_t first = mix_uid(seed);
    uint32_t second = mix_uid(seed ^ 0x9e3779b9u);
    uid[0] = 0x04;
    uid[1] = (uint8_t)first;
    uid[2] = (uint8_t)(first >> 8);
    uid[3] = (uint8_t)(first >> 16);
    uid[4] = (uint8_t)(first >> 24);
    uid[5] = (uint8_t)second;
    uid[6] = (uint8_t)(0x80 | ((second >> 8) & 0x0f));
}

static uint32_t scramble(const uint8_t uid[7], uint8_t count) {
    uint8_t data[24] = {
        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xb7,
        0xd5, 0xd7, 0xe6, 0xe7, 0xba, 0x3c, 0xa8, 0xd8,
        0x75, 0x47, 0x68, 0xcf, 0x23, 0xe9, 0xfe, 0xaa,
    };
    memcpy(data, uid, 7);
    data[count * 4 - 1] = 0xaa;

    uint32_t value = 0;
    for (uint8_t i = 0; i < count; i++) {
        uint32_t word = read_u32_le(&data[i * 4]);
        value = word + rotate_right(value, 25) + rotate_right(value, 10) - value;
    }
    return value;
}

static void encrypt_character(const uint8_t uid[7], uint16_t id, uint8_t output[8]) {
    uint32_t key[4] = {
        scramble(uid, 3), scramble(uid, 4), scramble(uid, 5), scramble(uid, 6),
    };
    uint32_t v0 = id;
    uint32_t v1 = id;
    uint32_t sum = 0;
    for (int i = 0; i < 32; i++) {
        sum += 0x9e3779b9u;
        v0 += ((v1 << 4) + key[0]) ^ (v1 + sum) ^ ((v1 >> 5) + key[1]);
        v1 += ((v0 << 4) + key[2]) ^ (v0 + sum) ^ ((v0 >> 5) + key[3]);
    }
    write_u32_le(output, v0);
    write_u32_le(output + 4, v1);
}

static void queue_event(const portal_tag_t *tag, bool removed) {
    portal_tag_event_t *event = &events[event_head];
    event->pad = tag->pad;
    event->type = 0;
    event->index = tag->index;
    event->removed = removed;
    memcpy(event->uid, tag->uid, sizeof(event->uid));

    uint8_t next = (uint8_t)((event_head + 1) % PORTAL_EVENT_QUEUE_SIZE);
    if (next == event_tail) {
        event_tail = (uint8_t)((event_tail + 1) % PORTAL_EVENT_QUEUE_SIZE);
    }
    event_head = next;
}

static void initialize_tag(portal_tag_t *tag, uint8_t pad, uint8_t position,
    tag_kind_t kind, uint16_t id) {
    memset(tag, 0, sizeof(*tag));
    tag->present = true;
    tag->pad = pad;
    tag->position = position;
    tag->kind = kind;
    tag->id = id;

    generate_uid(kind, id, tag->uid);

    memcpy(tag->pages[0], tag->uid, 3);
    tag->pages[0][3] = 0x88 ^ tag->uid[0] ^ tag->uid[1] ^ tag->uid[2];
    memcpy(tag->pages[1], &tag->uid[3], 4);
    tag->pages[2][0] = tag->uid[3] ^ tag->uid[4] ^ tag->uid[5] ^ tag->uid[6];

    if (kind == TAG_VEHICLE) {
        tag->pages[0x24][0] = (uint8_t)id;
        tag->pages[0x24][1] = (uint8_t)(id >> 8);
        tag->pages[0x26][1] = 1;
    } else {
        uint8_t encrypted[8];
        encrypt_character(tag->uid, id, encrypted);
        memcpy(tag->pages[0x24], encrypted, 4);
        memcpy(tag->pages[0x25], encrypted + 4, 4);
    }
}

void portal_state_init(void) {
    memset(&state, 0, sizeof(state));
    memset(effects, 0, sizeof(effects));
    memset(events, 0, sizeof(events));
    event_head = 0;
    event_tail = 0;
    state.nfc_enabled = true;
    critical_section_init(&state_lock);
}

bool portal_state_place(uint8_t pad, uint8_t position, tag_kind_t kind, uint16_t id) {
    bool valid_position = (pad == 1 && position == 0) ||
        (pad == 2 && position >= 1 && position <= 3) ||
        (pad == 3 && position >= 4 && position <= 6);
    if (!valid_position || (kind != TAG_CHARACTER && kind != TAG_VEHICLE)) {
        return false;
    }

    critical_section_enter_blocking(&state_lock);
    uint8_t slot = position;
    if (state.tags[slot].present) {
        critical_section_exit(&state_lock);
        return false;
    }
    initialize_tag(&state.tags[slot], pad, position, kind, id);
    state.tags[slot].index = slot;
    if (state.nfc_enabled) {
        queue_event(&state.tags[slot], false);
    }
    critical_section_exit(&state_lock);
    return true;
}

bool portal_state_remove(uint8_t index) {
    if (index >= PORTAL_MAX_TAGS) {
        return false;
    }
    critical_section_enter_blocking(&state_lock);
    bool present = state.tags[index].present;
    if (present) {
        if (state.nfc_enabled) {
            queue_event(&state.tags[index], true);
        }
        state.tags[index].present = false;
    }
    critical_section_exit(&state_lock);
    return present;
}

bool portal_state_remove_pad(uint8_t pad) {
    bool removed = false;
    critical_section_enter_blocking(&state_lock);
    for (int i = 0; i < PORTAL_MAX_TAGS; i++) {
        if (state.tags[i].present && state.tags[i].pad == pad) {
            if (state.nfc_enabled) {
                queue_event(&state.tags[i], true);
            }
            state.tags[i].present = false;
            removed = true;
        }
    }
    critical_section_exit(&state_lock);
    return removed;
}

bool portal_state_pop_event(portal_tag_event_t *event) {
    critical_section_enter_blocking(&state_lock);
    bool available = event_tail != event_head;
    if (available) {
        *event = events[event_tail];
        event_tail = (uint8_t)((event_tail + 1) % PORTAL_EVENT_QUEUE_SIZE);
    }
    critical_section_exit(&state_lock);
    return available;
}

bool portal_state_read(uint8_t index, uint8_t page, uint8_t out[16]) {
    critical_section_enter_blocking(&state_lock);
    bool valid = index < PORTAL_MAX_TAGS && state.tags[index].present && page + 3 < PORTAL_TAG_PAGES;
    if (valid) {
        memcpy(out, state.tags[index].pages[page], 16);
    }
    critical_section_exit(&state_lock);
    return valid;
}

bool portal_state_write(uint8_t index, uint8_t page, const uint8_t data[4]) {
    critical_section_enter_blocking(&state_lock);
    bool valid = index < PORTAL_MAX_TAGS && state.tags[index].present && page < PORTAL_TAG_PAGES;
    if (valid) {
        memcpy(state.tags[index].pages[page], data, 4);
    }
    critical_section_exit(&state_lock);
    return valid;
}

bool portal_state_get_tag(uint8_t index, tag_kind_t *kind, uint16_t *id) {
    critical_section_enter_blocking(&state_lock);
    bool valid = index < PORTAL_MAX_TAGS && state.tags[index].present;
    if (valid) {
        *kind = state.tags[index].kind;
        *id = state.tags[index].id;
    }
    critical_section_exit(&state_lock);
    return valid;
}

size_t portal_state_list(uint8_t *out_pairs, size_t max_pairs) {
    size_t count = 0;
    critical_section_enter_blocking(&state_lock);
    if (state.nfc_enabled) {
        for (int i = 0; i < PORTAL_MAX_TAGS && count < max_pairs; i++) {
            if (state.tags[i].present) {
                out_pairs[count * 2] = (uint8_t)((state.tags[i].pad << 4) | state.tags[i].index);
                out_pairs[count * 2 + 1] = 0;
                count++;
            }
        }
    }
    critical_section_exit(&state_lock);
    return count;
}

void portal_state_set_color(uint8_t pad, uint8_t red, uint8_t green, uint8_t blue) {
    critical_section_enter_blocking(&state_lock);
    uint8_t first = pad == 0 ? 0 : (uint8_t)(pad - 1);
    uint8_t last = pad == 0 ? PORTAL_PAD_COUNT : pad;
    if (first < PORTAL_PAD_COUNT && last <= PORTAL_PAD_COUNT) {
        for (uint8_t i = first; i < last; i++) {
            effects[i].type = EFFECT_NONE;
            state.colors[i][0] = red;
            state.colors[i][1] = green;
            state.colors[i][2] = blue;
        }
    }
    critical_section_exit(&state_lock);
}

static void start_fade(uint8_t index, uint8_t tick_time, uint8_t tick_count,
    uint8_t red, uint8_t green, uint8_t blue, bool random_color, uint32_t now) {
    portal_effect_t *effect = &effects[index];
    memcpy(effect->from, state.colors[index], 3);
    if (random_color) {
        uint32_t value = get_rand_32();
        effect->to[0] = (uint8_t)value;
        effect->to[1] = (uint8_t)(value >> 8);
        effect->to[2] = (uint8_t)(value >> 16);
    } else {
        effect->to[0] = red;
        effect->to[1] = green;
        effect->to[2] = blue;
    }
    effect->type = EFFECT_FADE;
    effect->remaining = tick_count;
    effect->forever = tick_count == 0;
    effect->phase_started_ms = now;
    effect->on_ms = (uint32_t)(tick_time ? tick_time : 1) * 10u;
}

void portal_state_start_fade(uint8_t pad, uint8_t tick_time, uint8_t tick_count,
    uint8_t red, uint8_t green, uint8_t blue, bool random_color) {
    uint8_t first = pad == 0 ? 0 : (uint8_t)(pad - 1);
    uint8_t last = pad == 0 ? PORTAL_PAD_COUNT : pad;
    if (first >= PORTAL_PAD_COUNT || last > PORTAL_PAD_COUNT) {
        return;
    }
    uint32_t now = (uint32_t)to_ms_since_boot(get_absolute_time());
    critical_section_enter_blocking(&state_lock);
    for (uint8_t i = first; i < last; i++) {
        start_fade(i, tick_time, tick_count, red, green, blue, random_color, now);
    }
    critical_section_exit(&state_lock);
}

void portal_state_start_flash(uint8_t pad, uint8_t tick_on, uint8_t tick_off,
    uint8_t tick_count, uint8_t red, uint8_t green, uint8_t blue) {
    uint8_t first = pad == 0 ? 0 : (uint8_t)(pad - 1);
    uint8_t last = pad == 0 ? PORTAL_PAD_COUNT : pad;
    if (first >= PORTAL_PAD_COUNT || last > PORTAL_PAD_COUNT) {
        return;
    }
    uint32_t now = (uint32_t)to_ms_since_boot(get_absolute_time());
    critical_section_enter_blocking(&state_lock);
    for (uint8_t i = first; i < last; i++) {
        portal_effect_t *effect = &effects[i];
        memcpy(effect->from, state.colors[i], 3);
        effect->to[0] = red;
        effect->to[1] = green;
        effect->to[2] = blue;
        memcpy(state.colors[i], effect->to, 3);
        effect->type = EFFECT_FLASH;
        effect->remaining = tick_count;
        effect->forever = tick_count == 0xff;
        effect->showing_color = true;
        effect->phase_started_ms = now;
        effect->on_ms = (uint32_t)(tick_on ? tick_on : 1) * 10u;
        effect->off_ms = (uint32_t)(tick_off ? tick_off : 1) * 10u;
    }
    critical_section_exit(&state_lock);
}

void portal_state_task(void) {
    uint32_t now = (uint32_t)to_ms_since_boot(get_absolute_time());
    critical_section_enter_blocking(&state_lock);
    for (uint8_t i = 0; i < PORTAL_PAD_COUNT; i++) {
        portal_effect_t *effect = &effects[i];
        if (effect->type == EFFECT_FADE) {
            uint32_t elapsed = now - effect->phase_started_ms;
            uint32_t duration = effect->on_ms;
            if (elapsed >= duration) {
                memcpy(state.colors[i], effect->to, 3);
                if (!effect->forever && --effect->remaining == 0) {
                    effect->type = EFFECT_NONE;
                    continue;
                }
                uint8_t swap[3];
                memcpy(swap, effect->from, 3);
                memcpy(effect->from, effect->to, 3);
                memcpy(effect->to, swap, 3);
                effect->phase_started_ms = now;
                elapsed = 0;
            }
            for (uint8_t channel = 0; channel < 3; channel++) {
                int32_t difference = (int32_t)effect->to[channel] - effect->from[channel];
                state.colors[i][channel] = (uint8_t)(effect->from[channel] +
                    difference * (int32_t)elapsed / (int32_t)duration);
            }
        } else if (effect->type == EFFECT_FLASH) {
            uint32_t duration = effect->showing_color ? effect->on_ms : effect->off_ms;
            if (now - effect->phase_started_ms < duration) {
                continue;
            }
            effect->showing_color = !effect->showing_color;
            memcpy(state.colors[i], effect->showing_color ? effect->to : effect->from, 3);
            effect->phase_started_ms = now;
            if (!effect->forever && effect->remaining > 0 && --effect->remaining == 0) {
                effect->type = EFFECT_NONE;
            }
        }
    }
    critical_section_exit(&state_lock);
}

void portal_state_get_color(uint8_t pad, uint8_t out[3]) {
    memset(out, 0, 3);
    if (pad < 1 || pad > PORTAL_PAD_COUNT) {
        return;
    }
    critical_section_enter_blocking(&state_lock);
    memcpy(out, state.colors[pad - 1], 3);
    critical_section_exit(&state_lock);
}

void portal_state_set_nfc(bool enabled) {
    critical_section_enter_blocking(&state_lock);
    state.nfc_enabled = enabled;
    critical_section_exit(&state_lock);
}

void portal_state_snapshot(portal_snapshot_t *snapshot) {
    critical_section_enter_blocking(&state_lock);
    *snapshot = state;
    critical_section_exit(&state_lock);
}
