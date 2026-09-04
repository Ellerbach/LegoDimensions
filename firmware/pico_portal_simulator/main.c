#include <stdio.h>

#include "pico/cyw43_arch.h"
#include "pico/stdlib.h"
#include "tusb.h"
#include "lwip/apps/mdns.h"
#include "lwip/ip4_addr.h"
#include "lwip/netif.h"

#include "dhcp_server.h"
#include "portal_protocol.h"
#include "portal_state.h"
#include "usb_transport.h"
#include "web_server.h"
#include "wifi_config.h"
#include "wifi_settings.h"

static wifi_settings_t wifi_settings;
static dhcp_server_t setup_dhcp;

#define SETUP_AP_SSID "Dimension-Toypad-Setup"
#define MDNS_HOSTNAME "dimensions"

static void start_mdns(struct netif *network) {
    cyw43_arch_lwip_begin();
    netif_set_hostname(network, MDNS_HOSTNAME);
    mdns_resp_init();
    err_t result = mdns_resp_add_netif(network, MDNS_HOSTNAME);
    if (result == ERR_OK) {
        s8_t service = mdns_resp_add_service(network, "Dimensions Portal", "_http",
            DNSSD_PROTO_TCP, 80, NULL, NULL);
        if (service < 0) {
            printf("WARNING: Failed to advertise the mDNS HTTP service (%d).\n", service);
        }
    }
    cyw43_arch_lwip_end();

    if (result == ERR_OK) {
        printf("Portal hostname: http://%s.local/\n", MDNS_HOSTNAME);
    } else {
        printf("WARNING: Failed to start mDNS responder (%d).\n", result);
    }
}

static const char *link_status_text(int status) {
    switch (status) {
        case CYW43_LINK_DOWN: return "link down";
        case CYW43_LINK_JOIN: return "joined; waiting for DHCP";
        case CYW43_LINK_NOIP: return "connected; no IP address";
        case CYW43_LINK_UP: return "connected with IP address";
        case CYW43_LINK_FAIL: return "connection failed";
        case CYW43_LINK_NONET: return "SSID not found";
        case CYW43_LINK_BADAUTH: return "authentication failed";
        default: return "unknown";
    }
}

static bool start_wifi_radio(void) {
    for (int attempt = 1; attempt <= 3; attempt++) {
        printf("Starting CYW43 radio (attempt %d/3)...\n", attempt);
        cyw43_arch_enable_sta_mode();
        if (cyw43_state.itf_state & (1u << CYW43_ITF_STA)) {
            printf("CYW43 radio started.\n");
            return true;
        }
        printf("CYW43 radio did not start; retrying after hardware power cycle.\n");
        sleep_ms(500);
    }
    return false;
}

static bool wait_for_dhcp(uint32_t timeout_ms) {
    absolute_time_t deadline = make_timeout_time_ms(timeout_ms);
    printf("Wi-Fi associated; waiting up to %lu seconds for a DHCP address",
        (unsigned long)(timeout_ms / 1000));
    while (!time_reached(deadline)) {
        int status = cyw43_tcpip_link_status(&cyw43_state, CYW43_ITF_STA);
        if (status == CYW43_LINK_UP) {
            printf(" done.\n");
            return true;
        }
        if (status < 0 || status == CYW43_LINK_DOWN) {
            printf("\nDHCP wait stopped: %s (%d).\n", link_status_text(status), status);
            return false;
        }
        printf(".");
        sleep_ms(1000);
    }
    printf(" timed out.\n");
    return false;
}

static void start_setup_access_point(void) {
    cyw43_arch_disable_sta_mode();
    cyw43_arch_enable_ap_mode(SETUP_AP_SSID, NULL, CYW43_AUTH_OPEN);

    ip_addr_t address;
    ip_addr_t netmask;
    ip_addr_set_ip4_u32(&address, PP_HTONL(CYW43_DEFAULT_IP_AP_ADDRESS));
    ip_addr_set_ip4_u32(&netmask, PP_HTONL(CYW43_DEFAULT_IP_MASK));
    cyw43_arch_lwip_begin();
    dhcp_server_init(&setup_dhcp, &cyw43_state.netif[CYW43_ITF_AP], &address, &netmask);
    cyw43_arch_lwip_end();

    start_mdns(&cyw43_state.netif[CYW43_ITF_AP]);
    web_server_set_setup_mode(1);
    web_server_init();
    printf("Setup access point started.\n");
    printf("SSID: %s\n", SETUP_AP_SSID);
    printf("Password: none\n");
    printf("Wi-Fi setup: http://192.168.4.1/\n");
}

static void print_network_status(void) {
    ip4_addr_t address = {0};
    ip4_addr_t netmask = {0};
    ip4_addr_t gateway = {0};
    bool interface_up = false;
    bool link_up = false;

    cyw43_arch_lwip_begin();
    struct netif *network = netif_default;
    if (network != NULL) {
        ip4_addr_copy(address, *netif_ip4_addr(network));
        ip4_addr_copy(netmask, *netif_ip4_netmask(network));
        ip4_addr_copy(gateway, *netif_ip4_gw(network));
        interface_up = netif_is_up(network);
        link_up = netif_is_link_up(network);
    }
    cyw43_arch_lwip_end();

    uint8_t mac[6] = {0};
    cyw43_wifi_get_mac(&cyw43_state, CYW43_ITF_STA, mac);

    char address_text[IP4ADDR_STRLEN_MAX];
    char netmask_text[IP4ADDR_STRLEN_MAX];
    char gateway_text[IP4ADDR_STRLEN_MAX];
    ip4addr_ntoa_r(&address, address_text, sizeof(address_text));
    ip4addr_ntoa_r(&netmask, netmask_text, sizeof(netmask_text));
    ip4addr_ntoa_r(&gateway, gateway_text, sizeof(gateway_text));

    printf("\n--- Portal network status ---\n");
    printf("SSID: %s\n", wifi_settings.ssid);
    printf("Wi-Fi MAC: %02X:%02X:%02X:%02X:%02X:%02X\n",
        mac[0], mac[1], mac[2], mac[3], mac[4], mac[5]);
    printf("Interface: %s, link: %s\n",
        interface_up ? "up" : "down", link_up ? "up" : "down");
    printf("IPv4: %s\n", address_text);
    printf("Netmask: %s\n", netmask_text);
    printf("Gateway: %s\n", gateway_text);
    printf("Portal UI: http://%s/\n", address_text);
    printf("-----------------------------\n");
}

int main(void) {
    stdio_init_all();
    portal_state_init();
    portal_protocol_init();

    printf("\nPico Portal Simulator starting\n");
    if (wifi_settings_load(&wifi_settings)) {
        printf("Loaded saved Wi-Fi settings.\n");
    } else {
        snprintf(wifi_settings.ssid, sizeof(wifi_settings.ssid), "%s", WIFI_SSID);
        snprintf(wifi_settings.password, sizeof(wifi_settings.password), "%s", WIFI_PASSWORD);
        wifi_settings.portal_variant = PORTAL_USB_XBOX_360;
        wifi_settings.state_verbosity = STATE_VERBOSITY_NONE;
        printf("No saved Wi-Fi settings; using the firmware bootstrap settings.\n");
    }
    web_server_set_settings(&wifi_settings);
    if (cyw43_arch_init()) {
        printf("ERROR: CYW43 Wi-Fi initialization failed.\n");
        return 1;
    }
    bool wifi_connected = false;
    bool web_server_started = false;
    if (!start_wifi_radio()) {
        printf("ERROR: CYW43 radio startup failed after 3 attempts.\n");
        printf("Check that the board is a Pico 2 W, then fully remove and restore power.\n");
        printf("USB simulation remains available.\n");
    } else {
        printf("Connecting to %s (Pico W requires a 2.4 GHz network)...\n", wifi_settings.ssid);
        uint32_t authentication = wifi_settings.password[0] == '\0' ?
            CYW43_AUTH_OPEN : CYW43_AUTH_WPA2_AES_PSK;
        int wifi_result = cyw43_arch_wifi_connect_timeout_ms(wifi_settings.ssid, wifi_settings.password,
            authentication, WIFI_CONNECT_TIMEOUT_MS);
        wifi_connected = wifi_result == 0;
        int link_status = cyw43_tcpip_link_status(&cyw43_state, CYW43_ITF_STA);
        if (!wifi_connected &&
                (link_status == CYW43_LINK_JOIN || link_status == CYW43_LINK_NOIP)) {
            wifi_connected = wait_for_dhcp(60000);
            link_status = cyw43_tcpip_link_status(&cyw43_state, CYW43_ITF_STA);
        }
        if (!wifi_connected) {
            printf("ERROR: Wi-Fi connection failed (%d): %s (%d).\n",
                wifi_result, link_status_text(link_status), link_status);
            if (link_status == CYW43_LINK_JOIN || link_status == CYW43_LINK_NOIP) {
                printf("The radio joined successfully, but the router did not issue an IPv4 address.\n");
                printf("Check the router's DHCP server, address pool, and MAC filtering.\n");
            } else {
                printf("Verify 2.4 GHz is enabled and security is WPA2-PSK/AES.\n");
            }
            printf("USB simulation remains available.\n");
        } else {
            start_mdns(&cyw43_state.netif[CYW43_ITF_STA]);
            web_server_set_setup_mode(0);
            web_server_init();
            web_server_started = true;
            print_network_status();
        }
    }

    if (!wifi_connected && !web_server_started) {
        start_setup_access_point();
    }

    printf("USB portal type: %s\n", usb_transport_variant_name(wifi_settings.portal_variant));
    usb_transport_init(wifi_settings.portal_variant);

    absolute_time_t next_status = make_timeout_time_ms(15000);
    while (true) {
        usb_transport_task();
        portal_state_task();
        web_server_task();
        if (wifi_connected && time_reached(next_status)) {
            print_network_status();
            next_status = make_timeout_time_ms(15000);
        }
        tight_loop_contents();
    }
}
