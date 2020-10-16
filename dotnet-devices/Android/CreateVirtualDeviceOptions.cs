namespace DotNetDevices.Android
{
    public class CreateVirtualDeviceOptions
    {
        public string? Device { get; set; }

        public bool Overwrite { get; set; }

        public string? Path { get; set; }

        public string? SharedSdCardPath { get; set; }

        public string? NewSdCardSize { get; set; }
    }
}
