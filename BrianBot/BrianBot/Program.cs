using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Speech.Synthesis;
using System.Speech.AudioFormat;
using System.IO;

namespace BrianBot
{
    class Program
    {
        public static string CurrentEdition;
        public static string NextEdition;
        public static string StartLocation;
        public static string OracleLocation;

        static void Main(string[] args)
        {
            if (CheckArgs(args))
                return;

            FunSoundsAndColor();
            GetLocations();
            
            // Create Upgrade Package //
            //UpdateTfs();
            ReadSqlFilesToInsert();
            ExecuteClrDll();
            ExportUpgradePackage();

            // Upgrade But Not Finalized //
            //RevertVmToCurrentEdition();
            //CopyDumpToVm();
            //RunDropPriorUpgrade();
            //ImportDumpFile();
            //ExecuteDeployClrDll();
            //RunDoGrantsOnNew();
            //RunQualifyDatabaseAndUnlock();
            //RunUpgrade();

            FunSoundsEnd();
            Console.WriteLine("** Brian Bot Finished, press enter to exit! **");
            Console.ReadLine();
        }

        #region Functionality Section

        /***********************************************************************/
        // Functionality Section 
        /***********************************************************************/
        
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

        /// <summary>
        /// Fun sounds for start of the program.
        /// </summary>
        static void FunSoundsAndColor()
        {
            Console.Title = "Brian Bot";
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Green;

            SpeechSynthesizer synth = new SpeechSynthesizer();
            synth.Rate = -2;
            synth.SetOutputToDefaultAudioDevice();
            synth.Speak("Brian bot is alive");

            Console.Beep(400, 150);
            Console.Beep(600, 150);
            Console.Beep(500, 150);
        }

        /// <summary>
        /// Fun sounds for end of the program.
        /// </summary>
        static void FunSoundsEnd()
        {
            SpeechSynthesizer synth = new SpeechSynthesizer();
            synth.Rate = -2;
            synth.SetOutputToDefaultAudioDevice();
            synth.Speak("Brian bot will go to sleep now");

            Console.Beep(300, 500);
            Console.Beep(200, 500);
        }

        /// <summary>
        /// Get the locations from the xml file.
        /// </summary>
        static void GetLocations()
        {
            XmlClass xClass = new XmlClass();
            xClass.ReadFile();
            StartLocation = xClass.XmlValues["StartLocation"];
            OracleLocation = xClass.XmlValues["OracleLocation"];

            using (var sr = new StreamReader(StartLocation + "\\DB Scripts\\Current Edition.txt"))
            {
                CurrentEdition = sr.ReadLine();
                NextEdition = sr.ReadLine();
            }
        }

        #endregion

        #region Create Upgrade Package Section

        /***********************************************************************/
        // Create Upgrade Package Section 
        /***********************************************************************/

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
        /// Get the files and insert them into the database.
        /// </summary>
        static void ReadSqlFilesToInsert()
        {
            try
            {
                ConvertUpgradeFilesClass fcc = new ConvertUpgradeFilesClass();
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
        /// Create the exported dump and log file.
        /// </summary>
        static void ExportUpgradePackage()
        {
            Console.WriteLine("Create an Export dmp file...");

            string fileName = "upg_" + CurrentEdition + "_" + NextEdition;
            Process p = new Process();
            p.StartInfo.FileName = "expdp.exe";
            p.StartInfo.WorkingDirectory = OracleLocation;
            p.StartInfo.Arguments = "lynx@beta/dang3r DUMPFILE=" + fileName + ".dmp LOGFILE=" + fileName + ".log REUSE_DUMPFILES=YES";
            p.Start();
            p.WaitForExit();

            // Need to move dmp file after it is created to repository:
            //string savelocation = StartLocation + "\\DB_Upgrades\\E07_to_E08";

            Console.WriteLine("Export dmp file has been created!");
        }

        #endregion

        #region Upgrade But Not Finalized Section

        /***********************************************************************/
        // Upgrade But Not Finalized Section 
        /***********************************************************************/

        static void RevertVmToCurrentEdition()
        {
            // https://www.vmware.com/support/ws55/doc/ws_learning_cli_vmrun.html
            
            // from command line application
            // vmrun revertToSnapshot [Path to .vmx file][snapshot name]

            /*
            Note: Before running this command on a Windows host, you must do one of the following:

            Change your working directory to the VMware Workstation directory. The default location is:
            c:\Program Files\VMware\VMware Workstation


            Add the VMware Workstation directory to the system path. On Windows 2000 and XP, this setting is changed from
            Control Panels > System > Advanced > Environment Variables > System variables > Path

            Examples for vmrun

            For example, to start a virtual machine:

            On the Windows command line, enter:
            vmrun start c:\My Virtual Machines\<virtual_machine_name>.vmx

            With virtual machines that require input through a VMware Workstation dialog box, vmrun may time out and fail. To disable Workstation dialog boxes, insert the following line into the .vmx configuration file for a virtual machine:

            msg.autoAnswer = TRUE
            */ 
        }

        static void CopyDumpToVm()
        {
            // Need to move dmp file after it is created to repository:
            //string savelocation = StartLocation + "\\DB_Upgrades\\E07_to_E08";
            // C:\app\oracle\admin\PLDB\dpdump - On VM

            // Need to setup a sharing between the host and vmware, then add the file to the shared folder.
        }

        /// <summary>
        /// Open 10_drop_prior_upgrade_objects.pdc and Parse and run each line.
        /// </summary>
        static void RunDropPriorUpgrade()
        {
            string dropLocation = StartLocation + "\\DB Scripts\\10_Structure\\" + CurrentEdition + "\\05_UPGRADE\\10_drop_prior_upgrade_objects.pdc";

            if (File.Exists(dropLocation))
            {
                string readLine;
                OracleClass oc = new OracleClass();
                oc.Connect();
                oc.tran = oc.conn.BeginTransaction();
                
                using (StreamReader dpuFile = new StreamReader(dropLocation))
                {
                    while((readLine = dpuFile.ReadLine()) != null)
                    {
                        if (readLine != "commit;")
                        {
                            readLine = readLine.Replace(";", "");
                            oc.NonQueryText(readLine);
                        }
                    }
                }

                oc.tran.Commit();
                oc.conn.Close();
            }
        }

        /// <summary>
        /// Import the dmp file on the VM.
        /// </summary>
        static void ImportDumpFile()
        {
            // This will need to be done in the VM, how is this going to work?
            // same call from a client computer connecting to server?
            
            Console.WriteLine("Import dmp file...");

            string fileName = "upg_" + CurrentEdition + "_" + NextEdition;
            Process p = new Process();
            p.StartInfo.FileName = "impdp.exe";
            p.StartInfo.WorkingDirectory = OracleLocation;  // C:\app\ptmp - should this be the location of the directory?
            p.StartInfo.Arguments = "lynx@beta/dang3r DUMPFILE=" + fileName + ".dmp";
            p.Start();
            p.WaitForExit();

            Console.WriteLine("Import dmp file has ran!");
        }
        
        /// <summary>
        /// Run Procedure lynx.deploy_clr_dlls.
        /// </summary>
        static void ExecuteDeployClrDll()
        {
            Console.WriteLine("Running lynx.deploy_clr_dlls ...");

            OracleClass oc = new OracleClass();
            oc.Connect();
            oc.tran = oc.conn.BeginTransaction();

            ProcedureInputClass pic = new ProcedureInputClass();
            pic.numberOfParameters = 0;
            pic.procedureName = "lynx.deploy_clr_dlls";
            oc.NonQueryProcedure(pic);

            oc.tran.Commit();
            oc.conn.Close();

            Console.WriteLine("Finished lynx.deploy_clr_dlls.");
        }

        /// <summary>
        /// Open 30_do_grants_on_new_objects.pdc and Parse and run each line.
        /// </summary>
        static void RunDoGrantsOnNew()
        {
            string dropLocation = StartLocation + "\\DB Scripts\\10_Structure\\" + CurrentEdition + "\\05_UPGRADE\\30_do_grants_on_new_objects.pdc";

            if (File.Exists(dropLocation))
            {
                string readLine;
                OracleClass oc = new OracleClass();
                oc.Connect();
                oc.tran = oc.conn.BeginTransaction();

                using (StreamReader dpuFile = new StreamReader(dropLocation))
                {
                    while ((readLine = dpuFile.ReadLine()) != null)
                    {
                        readLine = readLine.Replace(";", "");
                        oc.NonQueryText(readLine);
                    }
                }

                oc.tran.Commit();
                oc.conn.Close();
            }
        }

        /// <summary>
        /// Open 40_qualify_database_and_unlock_.pdc and Parse and run each line.
        /// </summary>
        static void RunQualifyDatabaseAndUnlock()
        {
            string dropLocation = StartLocation + "\\DB Scripts\\10_Structure\\" + CurrentEdition + "\\05_UPGRADE\\40_qualify_database_and_unlock_.pdc";

            if (File.Exists(dropLocation))
            {
                string readLine;
                OracleClass oc = new OracleClass();
                oc.Connect();
                oc.tran = oc.conn.BeginTransaction();

                using (StreamReader dpuFile = new StreamReader(dropLocation))
                {
                    while ((readLine = dpuFile.ReadLine()) != null)
                    {
                        readLine = readLine.Replace(";", "");
                        if (readLine.IndexOf("execute", StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            readLine = readLine.Remove(0, 8);
                            ProcedureInputClass pic = new ProcedureInputClass();
                            pic.numberOfParameters = 0;
                            pic.procedureName = readLine;
                            oc.NonQueryProcedure(pic);
                        }
                        else
                        {
                            oc.NonQueryText(readLine);
                        }
                    }
                }

                oc.tran.Commit();
                oc.conn.Close();
            }
        }

        /// <summary>
        /// Open 50_run_upgrade.pdc and Parse and run each line.
        /// </summary>
        static void RunUpgrade()
        {
            //string dropLocation = StartLocation + "\\DB Scripts\\10_Structure\\" + CurrentEdition + "\\05_UPGRADE\\50_run_upgrade.pdc";
            string dropLocation = StartLocation + "\\DB Scripts\\10_Structure\\" + CurrentEdition + "\\05_UPGRADE\\50_run_upgrade_original.pdc";

            if (File.Exists(dropLocation))
            {
                string readLine;
                OracleClass oc = new OracleClass();

                using (StreamReader dpuFile = new StreamReader(dropLocation))
                {
                    while ((readLine = dpuFile.ReadLine()) != null)
                    {
                        readLine = readLine.Replace(";", "");
                        
                        if (readLine.IndexOf("connect", StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            // Connect with edition or normal //
                            readLine = readLine.Remove(0, 8);
                            string UserId = readLine.Remove(readLine.IndexOf("/"));
                            string Password = readLine.Remove(0,readLine.IndexOf("/") + 1);
                            string Edition = "";
                            if (Password.IndexOf("edition") != -1)
                            {
                                // Parse out edition //
                                Edition = Password.Remove(0,Password.IndexOf("edition"));
                                Password = Password.Remove(Password.IndexOf("edition") - 1);
                            }

                            // Now try to connect //
                            //oc.Connect(UserId, Password, Edition);
                        }
                        else if (readLine.IndexOf("execute", StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            //oc.tran = oc.conn.BeginTransaction();
                            
                            // execute with a parameter (x) //
                            readLine = readLine.Remove(0, 8);

                            string proName = readLine.Remove(readLine.IndexOf("("));
                            string paramNum = readLine.Remove(0,readLine.IndexOf("(") + 1);
                            paramNum = paramNum.Remove(1);

                            ProcedureInputClass pic = new ProcedureInputClass();
                            pic.parameterName = new List<string>();
                            pic.parameterValue = new List<int>();
                            pic.numberOfParameters = 1;
                            pic.parameterName.Add("i_section_number");
                            pic.parameterValue.Add(Convert.ToInt32(paramNum));
                            pic.procedureName = proName;
                            
                            oc.NonQueryProcedure(pic);

                            //oc.tran.Commit();
                            //oc.conn.Close();
                        }
                       
                    }
                }  
            }
        }

        #endregion

        /*******************************************************************************************************/
        /*******************************************************************************************************/
        /********************************************************************************************************
        Put stuff here that is test code that may or may not be in the final code.
        ********************************************************************************************************/
        /*******************************************************************************************************/
        /*******************************************************************************************************/

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
