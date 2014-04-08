using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace BrianBot
{
    class Program
    {
        static void Main(string[] args)
        {
            if (CheckArgs(args))
                return;

            //UpdateTfs();
            //GetConfigurationClass gcc = ReadConfigFile();
            ReadFileToInsert();

            Console.ReadLine();
        }

        static GetConfigurationClass ReadConfigFile()
        {
            GetConfigurationClass g = new GetConfigurationClass();
            g.ReadConfig();
            return g;
        }

        /// <summary>
        /// Get the files and insert them into the database.
        /// </summary>
        static void ReadFileToInsert()
        {
            // string should be folder location got from config info //
            FileConvertClass fcc = new FileConvertClass();
            fcc.ReadFilesToInsert();
        }

        /// <summary>
        /// Check the args given by the user.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static bool CheckArgs(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg == "newxml")
                {
                    XmlClass xClass = new XmlClass();
                    xClass.CreateDefault();
                    return true;
                }
                else if (arg == "testdb")
                {
                    Console.WriteLine("User ID: ");
                    string UserId = Console.ReadLine();
                    Console.WriteLine("Password: ");
                    string Password = Console.ReadLine();
                    Console.WriteLine("Privilege: ");
                    string Privilege = Console.ReadLine();
                    TestDbConnection(UserId, Password, Privilege);
                    return true;
                }
                else if (arg == "help")
                {
                    ShowHelp();
                    return true;
                }
                else
                {
                    Console.WriteLine("Invalid Entry!");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Run the batch file to update TFS with latest.
        /// </summary>
        static void UpdateTfs()
        {
            Console.WriteLine("Update TFS...");
            Process p = Process.Start("getlatest_tfs.bat");
            p.WaitForExit();
            Console.WriteLine("TFS has been updated!");
        }

        static void OpenSqlPlus()
        {
            Process p = new Process();
            p.StartInfo.FileName = @"C:\app\SScribner\product\11.2.0\client_1\BIN\sqlplus.exe";
            p.StartInfo.Arguments = "lynx_app@SVR1/f3line";
            p.Start();
            p.WaitForExit();
        }

        /// <summary>
        /// Test if the xml file is setup correctly for a database connection.
        /// </summary>
        static void TestDbConnection(string UserId, string Password, string Privilege)
        {
            Console.WriteLine("Testing Database Connection...");
            try
            {
                OracleClass oc = new OracleClass();
                oc.Connect(UserId, Password, Privilege);
                oc.conn.Close();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Show Help File.
        /// </summary>
        static void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine("*** Help File ***");
            Console.WriteLine();
            Console.WriteLine("$ help - Show help, but you new this already.");
            Console.WriteLine("$ newxml - Create a new default xml file \"XmlDefault.xml\".");
            Console.WriteLine("  This file can be modified for the correct database parameters.");
            Console.WriteLine("$ testdb - Test the database connection to make sure the xml file is setup correctly.");
            Console.WriteLine();
            Console.WriteLine("*** End of Help File ***");
        }

        static void OldTestStuff()
        {
            // sql plus test //
            // lynx_app@SVR1/f3line

            // impdp and expdp tools for oracle
            // this location should be in the config file?
            // C:\app\SScribner\product\11.2.0\client_1\BIN
            // impdp lynx/dang3r DUMPFILE=%1.dmp LOGFILE=%1.log TABLE_EXISTS_ACTION=REPLACE

            // Collect all the scripts from TFS
            // Should be able to point to directory and read each file and run script
            // insert into RT_UPGRADE_COMMAND
            // Do that from Oracle call in here?
            // Then start a periodic read and update the console with the progress?
            // expdp... => UPG_E06_E07.DMP

            //Console.WriteLine("Do you want to do Oracle Read Test? (Y/N)");
            //test = Console.ReadLine();
            //if (test.ToUpper() != "Y")
            //    return;

            // Oracle Reader Test //
            //try
            //{
            //    OracleClass oc = new OracleClass();
            //    oc.ReadTimerSetup();
            //    oc.Connect();
            //    oc.Reader();
            //    //oc.conn.Dispose();
            //}
            //catch
            //{
            //}
        }
    }
}
