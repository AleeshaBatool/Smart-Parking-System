using System.ComponentModel.DataAnnotations;

namespace SmartParkingSystem.Models
{
    public class SetCamera
    {
        [Range(-2, 2)]
        public int Brightness { get; set; }

        [Range(-2, 2)]
        public int Contrast { get; set; }

        [Range(-2, 2)]
        public int Saturation { get; set; }

        [Range(0, 6)]
        public int SpecialEffect { get; set; }

        public bool WhiteBalance { get; set; }
        public bool AwbGain { get; set; }

        [Range(0, 4)]
        public int WbMode { get; set; }

        public bool ExposureCtrl { get; set; }
        public bool Aec2 { get; set; }

        [Range(-2, 2)]
        public int AeLevel { get; set; }

        [Range(0, 1200)]
        public int AecValue { get; set; }

        public bool GainCtrl { get; set; }

        [Range(0, 30)]
        public int AgcGain { get; set; }

        [Range(0, 6)]
        public int Gainceiling { get; set; }

        public bool Bpc { get; set; }
        public bool Wpc { get; set; }
        public bool RawGma { get; set; }
        public bool Lenc { get; set; }
        public bool Hmirror { get; set; }
        public bool Vflip { get; set; }
        public bool Dcw { get; set; }
        public bool Colorbar { get; set; }
    }
}
