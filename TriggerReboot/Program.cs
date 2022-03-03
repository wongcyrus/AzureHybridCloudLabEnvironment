using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Common.Model;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");


string? localIP;
using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
{
    socket.Connect("8.8.8.8", 65530);
    IPEndPoint? endPoint = socket.LocalEndPoint as IPEndPoint;
    localIP = endPoint?.Address.ToString();
    Console.WriteLine(localIP);

}
