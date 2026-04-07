using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace BlueFood.Api.Infrastructure;

public static class PublicTraceUrlBuilder
{
    public static string Build(string qrToken)
    {
        var configuredBaseUrl = Environment.GetEnvironmentVariable("BLUEFOOD_PUBLIC_BASE_URL");
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            return Combine(configuredBaseUrl, qrToken);
        }

        var hostAddress = GetLocalLanIpAddress();
        return $"http://{hostAddress}:5085/t/{WebUtility.UrlEncode(qrToken)}";
    }

    private static string Combine(string baseUrl, string qrToken)
    {
        var normalizedBaseUrl = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
        return normalizedBaseUrl + WebUtility.UrlEncode(qrToken);
    }

    private static string GetLocalLanIpAddress()
    {
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(networkInterface => networkInterface.OperationalStatus == OperationalStatus.Up)
            .SelectMany(networkInterface => networkInterface.GetIPProperties().UnicastAddresses)
            .Select(addressInfo => addressInfo.Address)
            .Where(address => address.AddressFamily == AddressFamily.InterNetwork)
            .Where(address => !IPAddress.IsLoopback(address))
            .Where(address => !address.ToString().StartsWith("169.254."))
            .ToList();

        var preferred = networkInterfaces.FirstOrDefault(address => address.ToString().StartsWith("192.168."))
            ?? networkInterfaces.FirstOrDefault(address => address.ToString().StartsWith("10."))
            ?? networkInterfaces.FirstOrDefault(address => address.ToString().StartsWith("172."))
            ?? networkInterfaces.FirstOrDefault();

        return preferred?.ToString() ?? "localhost";
    }
}
