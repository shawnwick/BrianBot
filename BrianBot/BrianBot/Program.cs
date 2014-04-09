using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;

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
            ReadSqlFilesToInsert();
            ExecuteClrDll();

            Console.WriteLine("** Brian Bot Finished, press enter to exit! **");
            Console.ReadLine();
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
                else if (arg == "version")
                {
                    Console.WriteLine("Version - " + Assembly.GetEntryAssembly().GetName().Version.ToString());
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
        /// Run Procedure lynx.store_clr_dlls.
        /// </summary>
        static void ExecuteClrDll()
        {
            Console.WriteLine("Running lynx.store_clr_dlls ...");
            
            OracleClass oc = new OracleClass();
            oc.Connect();
            oc.tran = oc.conn.BeginTransaction();

            ProcedureInputClass pic = new ProcedureInputClass();
            pic.numberOfParameters = 0;
            pic.procedureName = "lynx.store_clr_dlls";
            oc.NonQueryProcedure(pic);
            
            oc.tran.Commit();
            oc.conn.Close();

            Console.WriteLine("Finished lynx.store_clr_dlls.");
        }

        /// <summary>
        /// Get the files and insert them into the database.
        /// </summary>
        static void ReadSqlFilesToInsert()
        {
            try
            {
                FileConvertClass fcc = new FileConvertClass();
                fcc.ReadFilesToInsert("Structure");
                fcc.ReadFilesToInsert("Code");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Make sure XmlDefault.xml is setup correctly.");
            }
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
            Console.WriteLine("$ newxml - Create a new default xml file \"XmlDefault.xml\".");
            Console.WriteLine("  This file can be modified for the correct database parameters.");
            Console.WriteLine("$ testdb - Test a database connection.");
            Console.WriteLine("$ version - Show the current software version.");
            Console.WriteLine();
            Console.WriteLine("*** End of Help File ***");
        }

        /*******************************************************************************************************/
        /*******************************************************************************************************/
        /********************************************************************************************************
        Put stuff here that is test code that may or may not be in the final code.
        ********************************************************************************************************/
        /*******************************************************************************************************/
        /*******************************************************************************************************/

        static GetConfigurationClass ReadConfigFile()
        {
            GetConfigurationClass g = new GetConfigurationClass();
            g.ReadConfig();
            return g;
        }
        
        static void OpenSqlPlus()
        {
            Process p = new Process();
            p.StartInfo.FileName = @"C:\app\SScribner\product\11.2.0\client_1\BIN\sqlplus.exe";
            p.StartInfo.Arguments = "lynx_app@SVR1/f3line";
            p.Start();
            p.WaitForExit();
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
