﻿namespace FestivalInstrumentMapper
{
    internal enum HidApiDeviceType
    {
        Unknown,
        Santroller_RB,
        Santroller_GH,
        Wii_RB,
        PS3_RB,
        PS3_GH,
        PS4_RB_PDP,
        PS4_RB_MadCatz,
        PS5_RB_PDP,
        Raphnet_GH,
    }

    internal class HidApiDevice : InstrumentMapperDevice
    {
        private readonly HidDeviceStream _stream;

        public HidApiDeviceType Type { get; }

        public HidApiDevice(HidDeviceStream stream)
        {
            _stream = stream;
            Type = GetDeviceType(stream);
        }

        private static HidApiDeviceType GetDeviceType(HidDeviceStream stream)
        {
            // Santroller
            if (stream.VendorId == 0x1209 && stream.ProductId == 0x2882)
            {
                // Santroller encodes the device type into the top byte of the revision
                byte deviceType = (byte)(stream.Revision >> 8);

                // Rock Band guitar mode
                if (deviceType == 0x04)
                    return HidApiDeviceType.Santroller_RB;
                // Guitar Hero guitar mode
                if (deviceType == 0x03)
                    return HidApiDeviceType.Santroller_GH;
            }

            // PS3 instruments
            if (stream.VendorId == 0x12BA)
            {
                // Rock Band guitars
                if (stream.ProductId == 0x0200)
                    return HidApiDeviceType.PS3_RB;
                // Guitar Hero guitars
                if (stream.ProductId == 0x0100)
                    return HidApiDeviceType.PS3_GH;
            }

            // MadCatz (older VID)
            if (stream.VendorId == 0x1BAD)
            {
                // Wii RB1 guitars
                if (stream.ProductId == 0x0004)
                    return HidApiDeviceType.Wii_RB;
                // Wii RB2/3 guitars
                if (stream.ProductId == 0x3010)
                    return HidApiDeviceType.Wii_RB;
            }

            // MadCatz (newer VID)
            if (stream.VendorId == 0x0738)
            {
                // PS4 RB4 Stratocasters
                if (stream.ProductId == 0x8261)
                    return HidApiDeviceType.PS4_RB_MadCatz;
            }

            // PDP
            if (stream.VendorId == 0x0E6F)
            {
                // PS4 RB4 Jaguars
                if (stream.ProductId == 0x0173)
                    return HidApiDeviceType.PS4_RB_PDP;
                // PS4 Riffmaster guitars
                if (stream.ProductId == 0x024A)
                    return HidApiDeviceType.PS4_RB_PDP;
                // PS5 Riffmaster guitars
                if (stream.ProductId == 0x0249)
                    return HidApiDeviceType.PS5_RB_PDP;
            }

            // Raphnet
            if (stream.VendorId == 0x289B)
            {
                if (stream.ProductId == 0x0080)
                    return HidApiDeviceType.Raphnet_GH;
            }

            return HidApiDeviceType.Unknown;
        }

        public override string ToString()
        {
            string device_name = Type switch
            {
                HidApiDeviceType.Wii_RB => "Wii Rock Band Guitar",
                HidApiDeviceType.PS3_RB => "PS3 Rock Band Guitar",
                HidApiDeviceType.PS3_GH => "PS3 Guitar Hero Guitar",
                HidApiDeviceType.PS4_RB_MadCatz => "PS4 Stratocaster",
                HidApiDeviceType.PS4_RB_PDP => "PS4 Jaguar/Riffmaster",
                HidApiDeviceType.PS5_RB_PDP => "PS5 Riffmaster",
                HidApiDeviceType.Santroller_RB or
                HidApiDeviceType.Santroller_GH => "Santroller Guitar",
                HidApiDeviceType.Raphnet_GH => "Raphnet Wii Adapter",
                _ => $"Unknown - {_stream.VendorId:X4}:{_stream.ProductId:X4}:{_stream.Revision:X4}",
            };
            return $"{device_name}";
        }

        public override bool Exists() => true;

        public override void Open()
        {
            if (Type == HidApiDeviceType.Unknown)
                throw new Exception("That device is unknown?!");

            if (!_stream.Open(exclusive: false))
                throw new Exception("Failed to open HID device stream");
        }

        public override void Close()
        {
            // Stream can be re-opened after disposing
            _stream.Dispose();
        }

        public override void Read(Span<byte> buffer)
        {
            if (!_stream.Read(buffer))
                throw new Exception("Failed to read HID device report");
        }

        public override int GetReadLength()
        {
            int expected = Type switch
            {
                HidApiDeviceType.Wii_RB or
                HidApiDeviceType.PS3_RB => 28,
                HidApiDeviceType.PS3_GH => 28,
                HidApiDeviceType.PS4_RB_PDP or
                HidApiDeviceType.PS4_RB_MadCatz => 64,
                HidApiDeviceType.PS5_RB_PDP => 64,
                HidApiDeviceType.Santroller_RB => 7,
                HidApiDeviceType.Santroller_GH => 7,
                HidApiDeviceType.Raphnet_GH => 15,
                _ => throw new Exception($"Unhandled device type {Type}")
            };

            if (_stream.InputLength < expected)
                throw new Exception($"Device read length ({_stream.InputLength}) is less than expected ({expected})");

            return _stream.InputLength;
        }

        public override ToGipAction GetGipConverter()
        {
            return Type switch
            {
                HidApiDeviceType.Wii_RB or
                HidApiDeviceType.PS3_RB => ToGip.PS3Wii_RB,
                HidApiDeviceType.PS3_GH => ToGip.PS3_GH,
                HidApiDeviceType.PS4_RB_PDP or
                HidApiDeviceType.PS4_RB_MadCatz => ToGip.PS4_RB,
                HidApiDeviceType.PS5_RB_PDP => ToGip.PS5_RB,
                HidApiDeviceType.Santroller_RB => ToGip.Santroller_RB,
                HidApiDeviceType.Santroller_GH => ToGip.Santroller_GH,
                HidApiDeviceType.Raphnet_GH => ToGip.Raphnet_GH,
                _ => throw new Exception($"Unhandled device type {Type}")
            };
        }
    }
}
