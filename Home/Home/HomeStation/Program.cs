﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

using System.IO;
using HttpLibrary;

using HomeStation.InfraRed.Encoder;
using HomeStation.InfraRed.Decoder;
using HomeStation.TempHumid;
//using HomeStation.WebServer;

namespace Home
{
    public class Program
    {
        static HttpServer Server;//server object
        static Credential ServerCredential;//server security
        static Configuration ServerConfiguration;//configuration settings
        static DhtSensor Sensor;//humidity and temperature sensor
        static InfraredTransmitter InfraredTransmitter;
        static InfraredCodecNEC InfraredCodec;

        static Double temperature = 0;  // graus Celsius
        static Double humidity = 0;     // %

        public static void Main()
        {
            TimeCounter timeCounter = new TimeCounter();
            TimeSpan elapsed = TimeSpan.Zero;
            int i = 0;

            // Try to get clock at system start
            try
            {
                var time = NtpClient.GetNetworkTime();
                Utility.SetLocalTime(time);
            }
            catch (Exception ex)
            {
                // Don't depend on time
                Debug.Print("Error setting clock: " + ex.Message);
            }
            
            // On board led
            OutputPort onBoardLed = new OutputPort(Pins.ONBOARD_LED, false);

            // Humidity and Temperature
            Sensor = new Dht22Sensor(Pins.GPIO_PIN_D0, Pins.GPIO_PIN_D1, PullUpResistor.Internal);
            
            //IRRX: Infrared Decoder
            NecProtocolDecoder decoder = new NecProtocolDecoder(Pins.GPIO_PIN_D7);
            NecProtocolDecoder.OnIRCommandReceived += NecDecoder_OnIRCommandReceived;

            //IRTX: Infrared Encoder
            InfraredTransmitter = new InfraredTransmitter(Pins.GPIO_PIN_D8);
            InfraredCodec = new InfraredCodecNEC(InfraredTransmitter);

            Thread.Sleep(1000);

            // Web Server
            ServerConfiguration = new Configuration(80);
            ServerCredential = new Credential("Administrator", "admin", "admin");
            Server = new HttpServer(ServerConfiguration, ServerCredential, @"\SD\");
            Server.OnServerError += new OnServerErrorDelegate(Server_OnServerError);
            Server.OnRequestReceived += new OnRequestReceivedDelegate(Server_OnRequestReceived);
            Server.Start();

            while (true)
            {
                timeCounter.Start();
                {
                    elapsed += timeCounter.Elapsed;
                    if (elapsed.Seconds >= 2)
                    {
                        if (Sensor.Read())
                        {
                            temperature = Sensor.Temperature;
                            humidity = Sensor.Humidity;
                        }
                        elapsed = TimeSpan.Zero;
                        onBoardLed.Write((i++ & 0x01) == 0); // blink on board led

                        //string log = "DHT Sensor: RH = " + humidity.ToString("F1") +
                        //             "%  Temp = " + temperature.ToString("F1") + "°C ";
                        //Debug.Print(log);
                    }
                }
                timeCounter.Stop();
            }
        }

        static void Server_OnRequestReceived(HttpRequest Request, HttpResponse Response)
        {
            int cmd = 0;

            if (Request.RequestedCommand != null)
            {
                switch (Request.RequestedCommand.ToLower())
                {
                    case "on":
                        cmd = 0x0D;         // ON
                        break;
                    case "off":
                        cmd = 0x1F;         // OFF
                        break;
                    case "white":
                        cmd = 0x15;         // WHITE
                        break;
                    case "green":
                        cmd = 0x1B;         // GREEN
                        break;
                    case "red":
                        cmd = 0x19;         // RED
                        break;
                    case "blue":
                        cmd = 0x11;         // BLUE
                        break;
                    default:                // NONE
                        break;
                }

                if (cmd != 0) InfraredCodec.Send(0xFF, cmd);    // Address is ignored by current led stripes

                Response.WriteFilesList("Comando " + Request.RequestedCommand.ToLower() + ": Addr=0xFF Cmd=" + IntToHexString(cmd));
            }
            else if (Request.RequestedFile != null)
            {
                string FullFileName = Request.FilesPath + Request.RequestedFile;
                if (File.Exists(FullFileName))
                {
                    Response.WriteFile(FullFileName);
                }
                else
                {
                    Response.WriteNotFound();
                }
            }
            else
            {
                string status = "DHT Sensor: RH = " + humidity.ToString("F1") + "%  Temp = " + temperature.ToString("F1") + "°C ";

                Response.WriteFilesList(status);

                //Response.WriteFile(Request.FilesPath + "home.html"); // TODO: produto
            }
        }

        static void Server_OnServerError(ErrorEventArgs e)
        {
            Debug.Print(e.EventMessage);
        }

        static void NecDecoder_OnIRCommandReceived(UInt32 irData)
        {
            Debug.Print("Ir Command Received: " + UlongToHexString((ulong)irData));
        }

        // Hex to string. Codes make more sense in hex as they are really four bytes
        public static string UlongToHexString(ulong val)
        {
            string s = "";
            for (int i = 0; i < 8; i++)
            {
                ulong x = val & (ulong)0x0000000F;
                val = val >> 4;
                switch (x)
                {
                    case 0: s = '0' + s; break;
                    case 1: s = '1' + s; break;
                    case 2: s = '2' + s; break;
                    case 3: s = '3' + s; break;
                    case 4: s = '4' + s; break;
                    case 5: s = '5' + s; break;
                    case 6: s = '6' + s; break;
                    case 7: s = '7' + s; break;
                    case 8: s = '8' + s; break;
                    case 9: s = '9' + s; break;
                    case 10: s = 'A' + s; break;
                    case 11: s = 'B' + s; break;
                    case 12: s = 'C' + s; break;
                    case 13: s = 'D' + s; break;
                    case 14: s = 'E' + s; break;
                    case 15: s = 'F' + s; break;
                }
            }
            return s;
        }
        public static string IntToHexString(int val)
        {
            string s = "";
            for (int i = 0; i < 2; i++)
            {
                int x = val & 0x0F;
                val = val >> 4;
                switch (x)
                {
                    case 0: s = '0' + s; break;
                    case 1: s = '1' + s; break;
                    case 2: s = '2' + s; break;
                    case 3: s = '3' + s; break;
                    case 4: s = '4' + s; break;
                    case 5: s = '5' + s; break;
                    case 6: s = '6' + s; break;
                    case 7: s = '7' + s; break;
                    case 8: s = '8' + s; break;
                    case 9: s = '9' + s; break;
                    case 10: s = 'A' + s; break;
                    case 11: s = 'B' + s; break;
                    case 12: s = 'C' + s; break;
                    case 13: s = 'D' + s; break;
                    case 14: s = 'E' + s; break;
                    case 15: s = 'F' + s; break;
                }
            }
            return s;
        }
    }
}