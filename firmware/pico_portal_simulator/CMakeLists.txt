cmake_minimum_required(VERSION 3.13)

if (NOT DEFINED PICO_SDK_PATH)
    set(PICO_SDK_PATH $ENV{PICO_SDK_PATH})
endif()
if (NOT PICO_SDK_PATH)
    message(FATAL_ERROR "Set PICO_SDK_PATH to a Pico SDK 2.3.0 or newer checkout")
endif()
include(${PICO_SDK_PATH}/external/pico_sdk_import.cmake)

project(pico_portal_simulator C CXX ASM)
set(CMAKE_C_STANDARD 11)
set(CMAKE_CXX_STANDARD 17)

set(PICO_RP2350_A2_SUPPORTED 0)
pico_sdk_init()

add_executable(pico_portal_simulator
    dhcp_server.c
    main.c
    portal_protocol.c
    portal_state.c
    uart_bridge.c
    usb_descriptors.c
    usb_transport.c
    web_server.c
    wifi_settings.c
    xsm3_relay.c
)

pico_configure_ip4_address(pico_portal_simulator PRIVATE
    CYW43_DEFAULT_IP_AP_ADDRESS 192.168.4.1
)

set_source_files_properties(web_server.c PROPERTIES LANGUAGE CXX)

target_include_directories(pico_portal_simulator PRIVATE ${CMAKE_CURRENT_LIST_DIR})

pico_enable_stdio_uart(pico_portal_simulator 1)
pico_enable_stdio_usb(pico_portal_simulator 0)

target_link_libraries(pico_portal_simulator PRIVATE
    pico_stdlib
    pico_rand
    pico_flash
    hardware_flash
    pico_cyw43_arch_lwip_threadsafe_background
    pico_lwip_http
    pico_lwip_mdns
    tinyusb_device
    hardware_uart
)

pico_add_extra_outputs(pico_portal_simulator)
