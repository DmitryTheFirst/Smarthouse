using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Smarthouse.Modules.Hardware
{
    public static class WiringPi
    {
        #region Setup
        [DllImport("libwiringPi.so", EntryPoint = "wiringPiSetup")]
        public static extern void Setup();
        #endregion
        #region digital
        [DllImport("libwiringPi.so", EntryPoint = "pinMode")]
        public static extern void pinMode(int pin, int value);

        [DllImport("libwiringPi.so", EntryPoint = "digitalWrite")]
        public static extern void digitalWrite(int pin, int state);

        [DllImport("libwiringPi.so", EntryPoint = "pullUpDnControl")]
        public static extern void pullUpDnControl(int pin, int pud);

        [DllImport("libwiringPi.so", EntryPoint = "digitalRead")]
        public static extern int digitalRead(int pin);
        #endregion
        #region Servo
        [DllImport("libwiringPi.so", EntryPoint = "softServoWrite")]
        public static extern void softServoWrite(int pin, int value);

        [DllImport("libwiringPi.so", EntryPoint = "softServoSetup")]
        public static extern int softServoSetup(int p0, int p1, int p2, int p3, int p4, int p5, int p6, int p7);
        #endregion
        #region PWM
        [DllImport("libwiringPi.so", EntryPoint = "softPwmCreate")]
        public static extern int softPwmCreate(int pin, int initialValue, int pwmRange);

        [DllImport("libwiringPi.so", EntryPoint = "softPwmWrite")]
        public static extern void softPwmWrite(int pin, int value);
        #endregion
        #region IR reciever
        #region Structs
        [StructLayout(LayoutKind.Sequential)]
        public class lirc_config
        {
            public string current_mode;
            public lirc_config_entry next;
            public lirc_config_entry first;

            public int sockfd;

        }
        [StructLayout(LayoutKind.Sequential)]
        public class lirc_config_entry
        {
            public string prog;
            public lirc_code code;
            public uint rep_delay;
            public uint rep;
            public lirc_list config;
            public string change_mode;
            public uint flags;

            public string mode;
            public lirc_list next_config;
            public lirc_code next_code;

            public lirc_config_entry next;
        };
        [StructLayout(LayoutKind.Sequential)]
        public class lirc_code
        {
            public string remote;
            public string button;
            public lirc_code next;
        };
        [StructLayout(LayoutKind.Sequential)]
        public class lirc_list
        {
            public string str;
            public lirc_list next;
        };

        #endregion
        [DllImport("liblirc_client.so", EntryPoint = "lirc_init")]
        public static extern int lirc_init(string name, int verbose);

        [DllImport("liblirc_client.so", EntryPoint = "lirc_readconfig")]
        public static extern int lirc_readconfig(string path, ref lirc_config config, string callback);

        [DllImport("liblirc_client.so", EntryPoint = "lirc_nextcode")]
        public static extern int lirc_nextcode(ref string code);
        #endregion

        public enum PinMode
        {
            INPUT,
            OUTPUT,
            PWM_OUTPUT,
            GPIO_CLOCK,
            SOFT_PWM_OUTPUT,
            SOFT_TONE_OUTPUT,
            PWM_TONE_OUTPUT
        }
        public enum PinSignal    //0-HIGH, 1- LOW. I don't know why it works like this. In WiringPI there is reversed enum. 
        {
            HIGH,
            LOW
        }

        public enum PullResistor
        {
            PUD_OFF,
            PUD_DOWN,
            PUD_UP
        }


    }
}
