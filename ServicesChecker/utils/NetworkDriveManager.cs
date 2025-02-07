using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace ServicesChecker.utils
{
    public class NetworkDriveManager
    {
        [DllImport("mpr.dll", CharSet = CharSet.Auto)]
        private static extern int WNetUseConnection(
            IntPtr hwndOwner,
            [MarshalAs(UnmanagedType.Struct)] ref NETRESOURCE lpNetResource,
            string lpPassword,
            string lpUserID,
            uint dwFlags,
            StringBuilder lpAccessName,
            ref int lpBufferSize,
            out uint lpResult
        );

        [DllImport("mpr.dll", CharSet = CharSet.Auto)]
        private static extern int WNetCancelConnection2(string lpName, uint dwFlags, bool fForce);

        [StructLayout(LayoutKind.Sequential)]
        private struct NETRESOURCE
        {
            public uint dwScope;
            public uint dwType;
            public uint dwDisplayType;
            public uint dwUsage;

            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpLocalName;

            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpRemoteName;

            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpComment;

            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpProvider;
        }

        private const uint RESOURCE_CONNECTED = 0x00000001;
        private const uint RESOURCETYPE_DISK = 0x00000001;
        private const uint CONNECT_UPDATE_PROFILE = 0x00000001;

        public static void MapNetworkDrive(
            string driveLetter,
            string networkPath,
            string username,
            string password
        )
        {
            try
            {
                if (string.IsNullOrWhiteSpace(driveLetter))
                    throw new ArgumentException(
                        "Drive letter cannot be null or empty.",
                        nameof(driveLetter)
                    );

                if (string.IsNullOrWhiteSpace(networkPath))
                    throw new ArgumentException(
                        "Network path cannot be null or empty.",
                        nameof(networkPath)
                    );

                var nr = new NETRESOURCE
                {
                    dwType = RESOURCETYPE_DISK,
                    lpLocalName = driveLetter + ":",
                    lpRemoteName = networkPath,
                };

                int bufferSize = 64;
                var sb = new StringBuilder(bufferSize);
                uint resultFlags;

                int error = WNetUseConnection(
                    IntPtr.Zero,
                    ref nr,
                    password,
                    username,
                    CONNECT_UPDATE_PROFILE,
                    sb,
                    ref bufferSize,
                    out resultFlags
                );

                if (error == 0)
                {
                    Console.WriteLine($"Network drive {driveLetter}: mapped to {networkPath}");
                }
                else
                {
                    string message = new Win32Exception(error).Message;
                    Console.WriteLine($"Error mapping network drive: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while mapping network drive: {ex}");
            }
        }

        public static void UnmapNetworkDrive(string driveLetter)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(driveLetter))
                    throw new ArgumentException(
                        "Drive letter cannot be null or empty.",
                        nameof(driveLetter)
                    );

                int error = WNetCancelConnection2(driveLetter + ":", 0, true);

                if (error == 0)
                {
                    Console.WriteLine($"Network drive {driveLetter}: unmapped.");
                }
                else
                {
                    string message = new Win32Exception(error).Message;
                    Console.WriteLine($"Error unmapping network drive: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while unmapping network drive: {ex}");
            }
        }
    }
}
