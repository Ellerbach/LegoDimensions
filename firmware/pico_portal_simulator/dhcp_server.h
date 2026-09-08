#ifndef DHCP_SERVER_H
#define DHCP_SERVER_H

#include "lwip/ip_addr.h"

struct netif;
struct udp_pcb;

#define DHCP_SERVER_MAX_LEASES 8

typedef struct {
    uint8_t hardware_address[16];
    uint8_t hardware_address_length;
    uint32_t address;
} dhcp_lease_t;

typedef struct {
    struct udp_pcb *udp;
    struct netif *network;
    ip_addr_t address;
    ip_addr_t netmask;
    dhcp_lease_t leases[DHCP_SERVER_MAX_LEASES];
} dhcp_server_t;

void dhcp_server_init(dhcp_server_t *server, struct netif *network,
    const ip_addr_t *address, const ip_addr_t *netmask);
void dhcp_server_deinit(dhcp_server_t *server);

#endif
