#ifndef DHCP_SERVER_H
#define DHCP_SERVER_H

#include "lwip/ip_addr.h"

struct netif;
struct udp_pcb;

typedef struct {
    struct udp_pcb *udp;
    struct netif *network;
    ip_addr_t address;
    ip_addr_t netmask;
} dhcp_server_t;

void dhcp_server_init(dhcp_server_t *server, struct netif *network,
    const ip_addr_t *address, const ip_addr_t *netmask);
void dhcp_server_deinit(dhcp_server_t *server);

#endif
