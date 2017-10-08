using System;
using System.IO;
using System.IO.Ports;
using System.Collections;
using System.Windows.Forms; 

namespace TerraControlPanel
{
    
    public class Settings // Settings for terrarium control
    {
        
        public class Port // Port Name
        {
            public static string PortName = "COM1";
            
        }
        public class Light // Values for RGB LED strip
        {
            public class LD //Daylight Led RGB values
            {
                public static byte R = 255;
                public static byte G = 254;
                public static byte B = 240;
                public static string  RGB = ByteToString(R,G,B);
                public static void ParseRGB()
                {
                    R = byte.Parse(RGB.Substring(0, 3));
                    G = byte.Parse(RGB.Substring(4, 3));
                    B = byte.Parse(RGB.Substring(8, 3));
                }
                public static void UpdateRGB()
                {
                    RGB = ByteToString(R, G, B);
                }
            }
            public class LN // Night/Moonlight Led RGB values
            {
                public static byte R = 14;
                public static byte G = 16;
                public static byte B = 20;
                public static string RGB = ByteToString(R, G, B);
                public static void ParseRGB()
                {
                    R = byte.Parse(RGB.Substring(0, 3));
                    G = byte.Parse(RGB.Substring(4, 3));
                    B = byte.Parse(RGB.Substring(8, 3));
                }
                public static void UpdateRGB()
                {
                    RGB = ByteToString(R, G, B);
                }
            }
            public class LS // Dawn/Sunrise Led RGB values
            {
                public static byte R = 141;
                public static byte G = 78;
                public static byte B = 46;
                public static string RGB = ByteToString(R, G, B);
                public static void ParseRGB()
                {
                    R = byte.Parse(RGB.Substring(0, 3));
                    G = byte.Parse(RGB.Substring(4, 3));
                    B = byte.Parse(RGB.Substring(8, 3));
                }
                public static void UpdateRGB()
                {
                    RGB = ByteToString(R, G, B);
                }
            }
            public class LZ // Twilight/Sunset Led RGB values
            {
                public static byte R = 98;
                public static byte G = 42;
                public static byte B = 32;
                public static string RGB = ByteToString(R, G, B);
                public static void ParseRGB()
                {
                    R = byte.Parse(RGB.Substring(0, 3));
                    G = byte.Parse(RGB.Substring(4, 3));
                    B = byte.Parse(RGB.Substring(8, 3));
                }
                public static void UpdateRGB()
                {
                    RGB = ByteToString(R, G, B);
                }
            }
        }
        public class Temperature // Values for temperature control
        {
            public class TD // Daytime Temperature setting
            {
                public static byte I = 22; // Integer byte of temperature setting (Stored this way due to way nRF module handles sending data to terrarium control module)
                public static byte F = 0; // Fractional byte of temperature setting
                public static string Temp = ByteToString(I, F);
                public static void ParseTemp()
                {
                    I = byte.Parse(Temp.Substring(0, 3));
                    F = byte.Parse(Temp.Substring(4, 3));   
                }
                public static void UpdateTemp()
                {
                    Temp = ByteToString(I, F);
                }
            }
            public class TN // Nightime Temperature setting
            {
                public static byte I = 20; // Integer byte of temperature setting (Stored this way due to way nRF module handles sending data to terrarium control module)
                public static byte F = 0; // Fractional byte of temperature setting
                public static string Temp = ByteToString(I, F);
                public static void ParseTemp()
                {
                    I = byte.Parse(Temp.Substring(0, 3));
                    F = byte.Parse(Temp.Substring(4, 3));
                }
                public static void UpdateTemp()
                {
                    Temp = ByteToString(I, F);
                }
            }

        }
        public class Times // Start times for different setting modes
        {
            public class CD // Start time of day setting
            {
                public static byte HH = 8; 
                public static byte MM = 30; 
                public static string Time = ByteToString(HH, MM);
                public static void ParseTime()
                {
                    HH = byte.Parse(Time.Substring(0, 3));
                    MM = byte.Parse(Time.Substring(4, 3));
                }
                public static void UpdateTime()
                {
                    Time = ByteToString(HH, MM);
                }
            }
            public class CN // Start time of night setting
            {
                public static byte HH = 20;
                public static byte MM = 30;
                public static string Time = ByteToString(HH, MM);
                public static void ParseTime()
                {
                    HH = byte.Parse(Time.Substring(0, 3));
                    MM = byte.Parse(Time.Substring(4, 3));
                }
                public static void UpdateTime()
                {
                    Time = ByteToString(HH, MM);
                }
            }
            public class CS // Start time of dawn setting
            {
                public static byte HH = 6;
                public static byte MM = 30;
                public static string Time = ByteToString(HH, MM);
                public static void ParseTime()
                {
                    HH = byte.Parse(Time.Substring(0, 3));
                    MM = byte.Parse(Time.Substring(4, 3));
                }
                public static void UpdateTime()
                {
                    Time = ByteToString(HH, MM);
                }
            }
            public class CZ // Start time of twilight setting
            {
                public static byte HH = 18;
                public static byte MM = 30;
                public static string Time = ByteToString(HH, MM);
                public static void ParseTime()
                {
                    HH = byte.Parse(Time.Substring(0, 3));
                    MM = byte.Parse(Time.Substring(4, 3));
                }
                public static void UpdateTime()
                {
                    Time = ByteToString(HH, MM);
                }
            }
            public class CO // Start time of off setting ( no lights/dark night)
            {
                public static byte HH = 23;
                public static byte MM = 59;
                public static string Time = ByteToString(HH, MM);
                public static void ParseTime()
                {
                    HH = byte.Parse(Time.Substring(0, 3));
                    MM = byte.Parse(Time.Substring(4, 3));
                }
                public static void UpdateTime()
                {
                    Time = ByteToString(HH, MM);
                }
            }

        }
        public class Water // Values for humidity control
        {
            public class WP // Value for air humidity
            {
                public static byte I = 32; // Integer byte of humidity setting (Stored this way due to way nRF module handles sending data to terrarium control module)
                public static byte F = 0; // Fractional byte of humidity setting
                public static string Hum = ByteToString(I, F);
                public static void ParseHum()
                {
                    I = byte.Parse(Hum.Substring(0, 3));
                    F = byte.Parse(Hum.Substring(4, 3));
                }
                public static void UpdateHum()
                {
                    Hum = ByteToString(I, F);
                }
            }
            public class WG // Value for ground humidity
            {
                public static byte I = 32; // Integer byte of humidity setting (Stored this way due to way nRF module handles sending data to terrarium control module)
                public static byte F = 0; // Fractional byte of humidity setting
                public static string Hum = ByteToString(I, F);
                public static void ParseHum()
                {
                    I = byte.Parse(Hum.Substring(0, 3));
                    F = byte.Parse(Hum.Substring(4, 3));
                }
                public static void UpdateHum()
                {
                    Hum = ByteToString(I, F);
                }
            }

        }
        public class Panel // Settings for Application
        {
            public static bool SetTerraClock = false;
            public static bool UseOffLED = true;
            public static bool DebugMode = false;
            public static bool smallRefresh = true;
        }

        public static string ByteToString(byte R, byte G, byte B) // Conversion of 3 byte variables into parsable string
        {
            string str;
            str = R + "." + G + "." + B;

            if (R < 10)
                str = str.Insert(0, "00");
            else
                if (R < 100)
                str = str.Insert(0, "0");

            if (G < 10)
                str = str.Insert(4, "00");
            else
                if (G < 100)
                str = str.Insert(4, "0");

            if (B < 10)
                str = str.Insert(8, "00");
            else
                if (B < 100)
                str = str.Insert(8, "0");

            return str;
        }
        public static string ByteToString(byte A, byte B) // Conversion of 2 byte variables into parsable string
        {
            string str;
            str = A + "." + B;

            if (A < 10)
                str = str.Insert(0, "00");
            else
                if (A < 100)
                str = str.Insert(0, "0");

            if (B < 10)
                str = str.Insert(4, "00");
            else
                if (B < 100)
                str = str.Insert(4, "0");

            return str;
        }

        public static void ParseAll() // Updating all bytes from string values
        {
            Light.LD.ParseRGB();
            Light.LN.ParseRGB();
            Light.LS.ParseRGB();
            Light.LZ.ParseRGB();
            Temperature.TD.ParseTemp();
            Temperature.TN.ParseTemp();
            Times.CD.ParseTime();
            Times.CS.ParseTime();
            Times.CZ.ParseTime();
            Times.CN.ParseTime();
            Times.CO.ParseTime();
            Water.WP.ParseHum();
            Water.WG.ParseHum();
        }
        public static void UpdateAll() // Updating all strings from byte values
        {
            Light.LD.UpdateRGB();
            Light.LN.UpdateRGB();
            Light.LS.UpdateRGB();
            Light.LZ.UpdateRGB();
            Temperature.TD.UpdateTemp();
            Temperature.TN.UpdateTemp();
            Times.CD.UpdateTime();
            Times.CS.UpdateTime();
            Times.CZ.UpdateTime();
            Times.CN.UpdateTime();
            Times.CO.UpdateTime();
            Water.WP.UpdateHum();
            Water.WG.UpdateHum();
        }

        public static void Read() // Read the settings from disk
        {
            IniFile ini = new IniFile(Application.StartupPath + "\\TerraConfig.ini");
            Port.PortName = ini.ReadValue("Port", "PortName", Port.PortName);
            
            Light.LD.RGB  = ini.ReadValue("Lights", "LD", Light.LD.RGB);
            Light.LN.RGB = ini.ReadValue("Lights", "LN", Light.LN.RGB);
            Light.LS.RGB = ini.ReadValue("Lights", "LS", Light.LS.RGB);
            Light.LZ.RGB = ini.ReadValue("Lights", "LZ", Light.LZ.RGB);

            Temperature.TD.Temp = ini.ReadValue("Temperature","TD", Temperature.TD.Temp);
            Temperature.TN.Temp = ini.ReadValue("Temperature", "TN", Temperature.TN.Temp);
            
            Times.CD.Time = ini.ReadValue("Times", "CD", Times.CD.Time);
            Times.CS.Time = ini.ReadValue("Times", "CS", Times.CS.Time);
            Times.CZ.Time = ini.ReadValue("Times", "CZ", Times.CZ.Time);
            Times.CN.Time = ini.ReadValue("Times", "CN", Times.CN.Time);
            Times.CO.Time = ini.ReadValue("Times", "CO", Times.CO.Time);
            
            Water.WP.Hum = ini.ReadValue("Water", "WP", Water.WP.Hum);
            Water.WG.Hum = ini.ReadValue("Water", "WG", Water.WG.Hum);

            ParseAll();

        }     
        public static void Write() // Write the settings to disk
        {
            UpdateAll();
            IniFile ini = new IniFile(Application.StartupPath + "\\TerraConfig.ini");
            ini.WriteValue("Port", "PortName", Port.PortName);

            ini.WriteValue("Lights", "LD", Light.LD.RGB);
            ini.WriteValue("Lights", "LN", Light.LN.RGB);
            ini.WriteValue("Lights", "LS", Light.LS.RGB);
            ini.WriteValue("Lights", "LZ", Light.LZ.RGB);

            ini.WriteValue("Temperature", "TD", Temperature.TD.Temp);
            ini.WriteValue("Temperature", "TN", Temperature.TN.Temp);

            ini.WriteValue("Times", "CD", Times.CD.Time);
            ini.WriteValue("Times", "CS", Times.CS.Time);
            ini.WriteValue("Times", "CZ", Times.CZ.Time);
            ini.WriteValue("Times", "CN", Times.CN.Time);
            ini.WriteValue("Times", "CO", Times.CO.Time);

            ini.WriteValue("Water", "WP", Water.WP.Hum);
            ini.WriteValue("Water", "WG", Water.WG.Hum);

        }
	}
}
