﻿using System;
using System.Threading;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT;


namespace HomeStation.TempHumid
{
    public class DHTSensorScan
    {
        TimeCounter timeCounter = new TimeCounter();
        //Thread gather_input;

        TimeSpan elapsed = TimeSpan.Zero;
        TimeSpan elapsed1 = TimeSpan.Zero;

        //AnalogInput analogInput;
        Double temperature = 0;
        Double humidity = 0;

        Double[] temp_history = new Double[10];
        Double[] humid_history = new Double[10];

        Int32 history_index = 0;

        DhtSensor _sensor;

        public DHTSensorScan(DhtSensor sensor)
        {
            _sensor = sensor;
            
            //Create scan task uncommenting lines below
            //Otherwise leave commnents and run GatherInput loop after init

            //gather_input = new Thread(GatherInput);
            //gather_input.Priority = ThreadPriority.Lowest;
            //Thread.Sleep(2000);
            //gather_input.Start();
        }


        public void GatherInput()
        {
            while (true)
            {
                timeCounter.Start();
                {
                    elapsed += timeCounter.Elapsed;
                    elapsed1 += timeCounter.Elapsed;

                    if (elapsed.Milliseconds >= 500)
                    {
                        if (_sensor.Read())
                        {
                            temp_history[history_index] = _sensor.Temperature;
                            humid_history[history_index] = _sensor.Humidity;
                        }

                        history_index++;

                        if (history_index >= temp_history.Length)
                            history_index = 0;

                        elapsed = TimeSpan.Zero;
                    }

                    if (elapsed1.Seconds >= 2)
                    {
                        Double tmp = 0.0f;
                        for (Byte i = 0; i < temp_history.Length; i++) tmp += temp_history[i];
                        temperature = (tmp / temp_history.Length);

                        tmp = 0.0f;
                        for (Byte i = 0; i < humid_history.Length; i++) tmp += humid_history[i];
                        humidity = (tmp / humid_history.Length);

                        elapsed1 = TimeSpan.Zero;

                        //string log = "DHT Sensor: RH = " + humidity.ToString("F1") +
                        //                    "%  Temp = " + temperature.ToString("F1") + "°C ";
                        //Debug.Print(log);                            
                    }
                }
                timeCounter.Stop();
            }
        }

        public Double Temperature
        {
            get
            {
                return temperature;
            }
        }
        public Double Humidity
        {
            get
            {
                return humidity;
            }
        }
    }
}