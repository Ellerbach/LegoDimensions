// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.


// Dump all devices and descriptor information to console output.
using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using System.Collections.ObjectModel;

using (UsbContext context = new UsbContext())
{
    //var allDevices = context.List();
    //foreach (var usbRegistry in allDevices)
    //{
    //    Console.WriteLine(usbRegistry.Info.ToString());
    //    Console.WriteLine(usbRegistry.ToString());

    //    if (usbRegistry.TryOpen())
    //    {
    //        for (int iConfig = 0; iConfig < usbRegistry.Configs.Count; iConfig++)
    //        {
    //            UsbConfigInfo configInfo = usbRegistry.Configs[iConfig];
    //            Console.WriteLine(configInfo.ToString());

    //            ReadOnlyCollection<UsbInterfaceInfo> interfaceList = configInfo.Interfaces;
    //            for (int iInterface = 0; iInterface < interfaceList.Count; iInterface++)
    //            {
    //                UsbInterfaceInfo interfaceInfo = interfaceList[iInterface];
    //                Console.WriteLine(interfaceInfo.ToString());

    //                ReadOnlyCollection<UsbEndpointInfo> endpointList = interfaceInfo.Endpoints;
    //                for (int iEndpoint = 0; iEndpoint < endpointList.Count; iEndpoint++)
    //                {
    //                    Console.WriteLine(endpointList[iEndpoint].ToString());
    //                }
    //            }
    //        }

    //        usbRegistry.Close();
    //    }
    //}

    Test();
}

//Put your Product Id Here
const int ProductId = 0x0141;

//Put your Vendor Id Here
const int VendorId = 0x0E6F;

void Test()
{
    using (var context = new UsbContext())
    {
        context.SetDebugLevel(LogLevel.Info);

        //Get a list of all connected devices
        var usbDeviceCollection = context.List();

        //Narrow down the device by vendor and pid
        var selectedDevice = usbDeviceCollection.FirstOrDefault(d => d.ProductId == ProductId && d.VendorId == VendorId);

        //Open the device
        selectedDevice.Open();

        //Get the first config number of the interface
        selectedDevice.ClaimInterface(selectedDevice.Configs[0].Interfaces[0].Number);

        //Open up the endpoints
        var writeEndpoint = selectedDevice.OpenEndpointWriter(WriteEndpointID.Ep01);
        var readEnpoint = selectedDevice.OpenEndpointReader(ReadEndpointID.Ep01);

        var readBuffer = new byte[64];

        //Read some data
        readEnpoint.Read(readBuffer, 3000, out var readBytes);
        for (int i = 0; i < readBytes; i++)
        {
            Console.Write($"{readBuffer[i].ToString("X2")} ");
        }

        Console.WriteLine();

        //Create a buffer with some data in it
        var buffer = new byte[32] { 0x02, 0x20, 0x02, 0x0f, 0xb0, 0x01, 0x28, 0x63, 0x29, 0x20, 0x4c, 0x45, 0x47, 0x4f, 0x20, 0x32, 0x30, 0x31, 0x34, 0xf7, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, };

        //Write three bytes
        writeEndpoint.Write(buffer, 3000, out var bytesWritten);

        readEnpoint.Read(readBuffer, 3000, out readBytes);
        for (int i = 0; i < readBytes; i++)
        {
            Console.Write($"{readBuffer[i].ToString("X2")} ");
        }

        Console.WriteLine();

        readEnpoint.Read(readBuffer, 3000, out readBytes);
        for (int i = 0; i < readBytes; i++)
        {
            Console.Write($"{readBuffer[i].ToString("X2")} ");
        }

        Console.WriteLine();
    }
}