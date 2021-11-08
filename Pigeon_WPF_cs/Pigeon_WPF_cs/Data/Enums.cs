namespace Pigeon_WPF_cs.Enums
{
    /// <summary>
    /// Daftar perintah untuk dikirim ke wahana
    /// </summary>
    public enum Command
    {
        /// <summary>
        /// Perintah auto take off
        /// </summary>
        TAKE_OFF = 0xAA,

        /// <summary>
        /// Perintah auto landing
        /// </summary>
        LAND = 0xCC,

        /// <summary>
        /// Perintah membatalkan auto take-off
        /// </summary>
        BATALKAN = 0xBB
    }

    /// <summary>
    /// Daftar identifier protokol komunikasi
    /// </summary>
    public enum BufferHeader
    {
        /// <summary>
        /// Protokol prorietary EFALCON 4.0
        /// </summary>
        EFALCON4 = 'W',

        /// <summary>
        /// Protokol MAVLINK Versi 1.0
        /// </summary>
        MAVLINK1 = 0xFE,

        /// <summary>
        /// Protokol MAVLINK Versi 2.0
        /// </summary>
        MAVLINK2 = 0xFD,

        /// <summary>
        /// Protokol prorietary TRITON
        /// </summary>
        TRITON = 'T'
    }

    /// <summary>
    /// Daftar mode penerbangan
    /// </summary>
    public enum FlightMode
    {
        /// <summary>
        /// Terbang secara manual
        /// </summary>
        MANUAL,

        /// <summary>
        /// Terbang dengan stabilisasi
        /// </summary>
        STABILIZER,

        /// <summary>
        /// Terbang mengitari tempat
        /// </summary>
        LOITER,

        /// <summary>
        /// Terbang secara autopilot
        /// </summary>
        AUTO,

        /// <summary>
        /// Sedang takeoff
        /// </summary>
        TAKEOFF
    }

    /// <summary>
    /// Identifier penggunaan efalcon
    /// </summary>
    public enum TipeDevice
    {
        /// <summary>
        /// Antenna Tracker (Triton)
        /// </summary>
        TRACKER,

        /// <summary>
        /// Wahana UAV
        /// </summary>
        WAHANA
    }

    public enum ConnType
    {
        Internet,
        WIFI,
        SerialPort
    }
}