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
            ReadFileToInsert();

            Console.ReadLine();
        }

        static void ReadFileToInsert()
        {
            FileConvertClass fcc = new FileConvertClass();
            fcc.ReadFileToInsert("C:\\Work\\PL_SQL_Stuff\\test_upgrade\\E03toE04.pdc");
        }

        static void OldTestStuff()
        {
            // sql plus test //
            // lynx_app@SVR1/f3line

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

        /// <summary>
        /// Check the args given by the user.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static bool CheckArgs(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg == "new_xml")
                {
                    XmlClass xClass = new XmlClass();
                    xClass.CreateDefault();
                    return true;
                }
                else if (arg == "test_db_conn")
                {
                    TestDbConnection();
                    return true;
                }
                else if (arg == "-h")
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

        /// <summary>
        /// Test if the xml file is setup correctly for a database connection.
        /// </summary>
        static void TestDbConnection()
        {
            Console.WriteLine("Testing Database Connection...");
            try
            {
                OracleClass oc = new OracleClass();
                oc.Connect();
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
            Console.WriteLine("$ \"-h\" - Show help, but you new this already.");
            Console.WriteLine("$ \"new_xml\" - Create a new default xml file \"XmlDefault.xml\".");
            Console.WriteLine("  This file can be modified for the correct database parameters.");
            Console.WriteLine("$ \"test_db_conn\" - Test the database connection to make sure the xml file is setup correctly.");
            Console.WriteLine();
            Console.WriteLine("*** End of Help File ***");
        }
    }
}
