using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pigeon_WPF_cs.Enums;

namespace Pigeon_WPF_cs.Data_Classes
{
    public class TipeWahana
    {
        public TipeDevice Tipe;
    }

    public class Inertial
    {
        /// <summary>
        /// Arah horizontal wahana (Heading).<br/>
        /// Nilai dalam satuan <b>Derajat</b> (<i>°</i>)
        /// </summary>
        public float Yaw { get; set; } = 0;

        /// <summary>
        /// Sudut kecuraman wahana (Nose/Tail).<br/>
        /// Nilai dalam satuan <b>Derajat</b> (<i>°</i>)
        /// </summary>
        public float Pitch { get; set; } = 0;

        /// <summary>
        /// Sudut kemiringan sayap wahana.<br/>
        /// Nilai dalam satuan <b>Derajat</b> (<i>°</i>)
        /// </summary>
        public float Roll { get; set; } = 0;
    }

    public class GPSData
    {
        /// <summary>
        /// Apakah data valid?
        /// </summary>
        public bool IsValid { get { if (Latitude != 0 && Longitude != 0) { return true; } else { return false; } } }

        /// <summary>
        /// Koordinat Latitude dalam format Decimal Degrees
        /// </summary>
        public int Latitude { get; set; } = 0;

        /// <summary>
        /// Koordinat Longitude dalam format Decimal Degrees
        /// </summary>
        public int Longitude { get; set; } = 0;
    }

    public class FlightData : TipeWahana
    {
        public FlightMode FlightMode { get; set; } = FlightMode.MANUAL;

        public UInt16 BatteryVolt { get; set; } = 0;
        public UInt16 BatteryCurr { get; set; } = 0;

        public ushort MavlinkMiliVolt { get; set; } = 0;
        public short MavlinkCentiAmp { get; set; } = 0;

        /// <summary>
        /// Kualitas sinyal dari perhitungan paket data yang dibuang.<br/>
        /// Nilai dalam satuan <b>Persen</b> (<i>%</i>) dengan rentang 0-255.
        /// </summary>
        public byte Signal { get; set; } = 0;

        /// <summary>
        /// Data sensor IMU
        /// </summary>
        public Inertial IMU { get; set; } = new Inertial();

        /// <summary>
        /// Ketinggian wahana dari permukaan laut (dpl)/(MSL).<br/>
        /// Nilai dalam satuan <b>Milimeter</b> (<i>mm</i>).
        /// </summary>
        public int Altitude { get; set; } = 0;

        public float AltitudeFloat { get; set; } = 0;

        /// <summary>
        /// Kecepatan wahana terhadap tanah.<br/>
        /// Nilai dalam satuan <b>Meter per Sec</b> (<i>m/s</i>)
        /// </summary>
        public float Speed { get; set; } = 0;

        /// <summary>
        /// Data sensor GPS
        /// </summary>
        public GPSData GPS { get; set; } = new GPSData();
    }

    public class TrackerData : TipeWahana
    {
        public Inertial IMU { get; set; } = new Inertial();

        public float Battery { get; set; } = 0;

        public GPSData GPS { get; set; } = new GPSData();

        /// <summary>
        /// Ketinggian tracker (Meter)
        /// </summary>
        public float Altitude { get; set; } = 0;
    }
}
