cmake_minimum_required(VERSION 3.13)

if (NOT DEFINED PICO_SDK_PATH)
    set(PICO_SDK_PATH $ENV{PICO_SDK_PATH})
endif()
if (NOT PICO_SDK_PATH)
    message(FATAL_ERROR "Set PICO_SDK_PATH to a Pico SDK 2.3.0 or newer checkout")
endif()
include(${PICO_SDK_PATH}/external/pico_sdk_import.cmake)

project(pico_portal_xsm3_sidecar C CXX ASM)
set(CMAKE_C_STANDARD 11)
set(CMAKE_CXX_STANDARD 17)

set(PICO_RP2350_A2_SUPPORTED 0)
pico_sdk_init()

add_executable(pico_portal_xsm3_sidecar
    main.c
    ../pico_portal_simulator/uart_bridge.c
)

target_include_directories(pico_portal_xsm3_sidecar PRIVATE
    ${CMAKE_CURRENT_LIST_DIR}
    ${CMAKE_CURRENT_LIST_DIR}/../pico_portal_simulator
)

pico_enable_stdio_uart(pico_portal_xsm3_sidecar 0)
pico_enable_stdio_usb(pico_portal_xsm3_sidecar 0)

target_link_libraries(pico_portal_xsm3_sidecar PRIVATE
    pico_stdlib
    hardware_uart
    tinyusb_host
)

pico_add_extra_outputs(pico_portal_xsm3_sidecar)