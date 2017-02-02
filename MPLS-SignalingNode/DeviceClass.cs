﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlPlane;
using System.Threading;

namespace MPLS_SignalingNode
{
    public class DeviceClass
    {
        private SignallingNodeDeviceClass _signallignNode;
        private string _configurationFolderPath;

        #region Private Variables
        private static string fileLogPath;
        private static int logID;
        private string fileConfigurationPath;

        private static ReaderWriterLockSlim _writeLock = new ReaderWriterLockSlim();
        #endregion


        public DeviceClass()
        {
            ReadConfigFilePath();
            InitializeLogLastIdNumber();

            _signallignNode = new SignallingNodeDeviceClass(_configurationFolderPath);
        
        }

        //tutaj zmieniłem ze wczytujemy katalog plików konfiguracyjncyh a nie ściezke do pliku!!
        private void ReadConfigFilePath()
        {
            Console.WriteLine("\nEnter the path of the configuration folder:");
            _configurationFolderPath = Console.ReadLine();
            if (_configurationFolderPath == "")
                fileConfigurationPath = _configurationFolderPath + "/LSR_config.xml";
            Console.WriteLine();

            fileLogPath = _configurationFolderPath + "/logFile.txt";

            //bool fileNotExist = !File.Exists(fileConfigurationPath);
            //while (fileNotExist)
            //{
            //    Console.WriteLine("Cannot find the file. Please enter the right path.");
            //    fileConfigurationPath = Console.ReadLine();
            //    fileNotExist = !File.Exists(fileConfigurationPath);
            //    Console.WriteLine();
            //}
        }


        public void StartWorking()
        {
            MakeLog("INFO - Start working.");
            MakeConsoleLog("INFO - Start working.");
            Console.WriteLine();
            Console.WriteLine("Node is working. Write 'end' to close the program.");
            Console.WriteLine("<------------------------------------------------->");
            string end = null;
            do
            {
                end = Console.ReadLine();
            }
            while (end != "end");

            //LOG
            DeviceClass.MakeLog("INFO - Stop working.");
            DeviceClass.MakeConsoleLog("INFO - Stop working.");
        }

        public void StopWorking(string reason)
        {
            Console.WriteLine();
            Console.WriteLine(reason);
            Console.WriteLine("Click 'enter' to close the application...");
            Console.ReadLine();

            //LOG
            DeviceClass.MakeLog("INFO - Stop working.");
            DeviceClass.MakeConsoleLog("INFO - Stop working.");

            //wyłącz konsolę i zwolnij calą pamięć alokowaną
            Environment.Exit(0);
        }


        #region Logs
        public static void MakeLog(string logDescription)
        {
            string log = "#" + logID + " | " + DateTime.Now.ToString("hh:mm:ss") + " " + logDescription;
            logID++;

            WriteToFileThreadSafe(log, fileLogPath);
        }
        public static void WriteToFileThreadSafe(string text, string path)
        {
            // Set Status to Locked
            _writeLock.EnterWriteLock();
            try
            {
                // Append text to the file
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine(text);
                    sw.Close();
                }
            }
            finally
            {
                // Release lock
                _writeLock.ExitWriteLock();
            }
        }    
        public static void MakeConsoleLog(string logDescription)
        {
            string log;
            log = "#" + logID + " | " + DateTime.Now.ToString("hh:mm:ss") + " " + logDescription;
            Console.WriteLine(log);
        }
        private void InitializeLogLastIdNumber()
        {
            if (File.Exists(fileLogPath))
            {
                string last = File.ReadLines(fileLogPath).Last();
                string[] tmp = last.Split('|');

                string tmp2 = tmp[0].Substring(1);
                
                logID = Int32.Parse(tmp2);
                logID++;
            }
            else
                logID = 1;
        }
        #endregion
    }
}
