#include "dhcp_server.h"

#include <string.h>

#include "lwip/netif.h"
#include "lwip/pbuf.h"
#include "lwip/udp.h"

#define DHCP_SERVER_PORT 67
#define DHCP_CLIENT_PORT 68
#define DHCP_DISCOVER 1
#define DHCP_OFFER 2
#define DHCP_REQUEST 3
#define DHCP_ACK 5

#pragma pack(push, 1)
typedef struct {
    uint8_t op, htype, hlen, hops;
    uint32_t xid;
    uint16_t secs, flags;
    uint8_t ciaddr[4], yiaddr[4], siaddr[4], giaddr[4];
    uint8_t chaddr[16], sname[64], file[128], options[312];
} dhcp_message_t;
#pragma pack(pop)

static uint8_t message_type(const dhcp_message_t *message, size_t length) {
    if (length < 243 || memcmp(message->options, "\x63\x82\x53\x63", 4) != 0) {
        return 0;
    }
    const uint8_t *option = message->options + 4;
    const uint8_t *end = (const uint8_t *)message + length;
    while (option < end && *option != 255) {
        if (*option == 0) {
            option++;
            continue;
        }
        if (option + 2 > end || option + 2 + option[1] > end) {
            return 0;
        }
        if (option[0] == 53 && option[1] == 1) {
            return option[2];
        }
        option += 2 + option[1];
    }
    return 0;
}

static uint8_t *add_option(uint8_t *at, uint8_t code, uint8_t length, const void *value) {
    *at++ = code;
    *at++ = length;
    memcpy(at, value, length);
    return at + length;
}

static void receive(void *argument, struct udp_pcb *pcb, struct pbuf *packet,
    const ip_addr_t *source, u16_t source_port) {
    (void)pcb;
    (void)source;
    (void)source_port;
    dhcp_server_t *server = (dhcp_server_t *)argument;
    dhcp_message_t request;
    size_t length = pbuf_copy_partial(packet, &request, sizeof(request), 0);
    uint8_t requested_type = message_type(&request, length);
    pbuf_free(packet);
    if (requested_type != DHCP_DISCOVER && requested_type != DHCP_REQUEST) {
        return;
    }

    dhcp_message_t reply;
    memset(&reply, 0, sizeof(reply));
    reply.op = 2;
    reply.htype = request.htype;
    reply.hlen = request.hlen;
    reply.xid = request.xid;
    reply.flags = request.flags;
    memcpy(reply.chaddr, request.chaddr, sizeof(reply.chaddr));

    uint32_t offered = PP_HTONL(0xc0a80410u);
    memcpy(reply.yiaddr, &offered, 4);
    memcpy(reply.siaddr, &ip4_addr_get_u32(ip_2_ip4(&server->address)), 4);
    memcpy(reply.options, "\x63\x82\x53\x63", 4);
    uint8_t *option = reply.options + 4;
    uint8_t reply_type = requested_type == DHCP_DISCOVER ? DHCP_OFFER : DHCP_ACK;
    uint32_t lease = PP_HTONL(86400u);
    option = add_option(option, 53, 1, &reply_type);
    option = add_option(option, 54, 4, &ip4_addr_get_u32(ip_2_ip4(&server->address)));
    option = add_option(option, 1, 4, &ip4_addr_get_u32(ip_2_ip4(&server->netmask)));
    option = add_option(option, 3, 4, &ip4_addr_get_u32(ip_2_ip4(&server->address)));
    option = add_option(option, 6, 4, &ip4_addr_get_u32(ip_2_ip4(&server->address)));
    option = add_option(option, 51, 4, &lease);
    *option++ = 255;

    size_t reply_length = (size_t)(option - (uint8_t *)&reply);
    struct pbuf *output = pbuf_alloc(PBUF_TRANSPORT, (u16_t)reply_length, PBUF_RAM);
    if (output == NULL) {
        return;
    }
    pbuf_take(output, &reply, reply_length);
    ip_addr_t broadcast;
    ip_addr_set_ip4_u32(&broadcast, PP_HTONL(0xffffffffu));
    udp_sendto_if(server->udp, output, &broadcast, DHCP_CLIENT_PORT, server->network);
    pbuf_free(output);
}

void dhcp_server_init(dhcp_server_t *server, struct netif *network,
    const ip_addr_t *address, const ip_addr_t *netmask) {
    memset(server, 0, sizeof(*server));
    server->network = network;
    ip_addr_copy(server->address, *address);
    ip_addr_copy(server->netmask, *netmask);
    server->udp = udp_new_ip_type(IPADDR_TYPE_V4);
    if (server->udp != NULL && udp_bind(server->udp, IP_ANY_TYPE, DHCP_SERVER_PORT) == ERR_OK) {
        udp_bind_netif(server->udp, network);
        udp_recv(server->udp, receive, server);
    }
}

void dhcp_server_deinit(dhcp_server_t *server) {
    if (server->udp != NULL) {
        udp_remove(server->udp);
        server->udp = NULL;
    }
}
