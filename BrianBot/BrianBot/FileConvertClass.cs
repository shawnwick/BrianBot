using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace BrianBot
{
    class FileConvertClass
    {
        #region Private Variables

        private string currentEdition;
        private string nextEdition;
        private List<string> startInsert;
        private Dictionary<string, string> retryCount;
        private Dictionary<string,string> dbConnections;

        private InsertValues insertV; 
        private class InsertValues
        {
            public int sectionNumber;
            public int executionOrder;
            public string finalIndication;
            public string retryType;
            public string upgradId;
        }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public FileConvertClass()
        {
            currentEdition = "";
            nextEdition = "";
            
            insertV = new InsertValues();
            insertV.sectionNumber = 0;
            insertV.executionOrder = 1000;
            insertV .finalIndication= "'N'";
            insertV.retryType = "1";
            insertV.upgradId = "1";
            
            startInsert = new List<string>();
            startInsert.Add("create or replace and compile java source");
            startInsert.Add("create or replace trigger");
            startInsert.Add("create or replace package");
            startInsert.Add("create or replace procedure");
            startInsert.Add("create or replace function ");
            startInsert.Add("declare");
            startInsert.Add("begin");

            retryCount = new Dictionary<string, string>();
            retryCount.Add("drop", "2");
            retryCount.Add("revoke", "2");
            retryCount.Add("commit", "3");
            retryCount.Add("grant", "3");
            retryCount.Add("purge recyclebin", "3");

            dbConnections = new Dictionary<string, string>();
            dbConnections.Add("lynx", "lynx/dang3r");
            dbConnections.Add("lynx_ne", "lynx_ne/f3line");
            dbConnections.Add("lynx_dev_ne", "lynx_dev_ne/f3line");
            dbConnections.Add("lynx_dev", "lynx_dev/f3line");
            dbConnections.Add("lynx_olap", "lynx_olap/dang3r");
            dbConnections.Add("lynx_analysis", "lynx_analysis/dang3r");
        }

        /// <summary>
        /// Reads the sql file and then parses and inserts it into the database.
        /// </summary>
        /// <param name="filePath"></param>
        public void ReadFilesToInsert()
        {
            Console.WriteLine("Converting files to inserts into DB...");
            
            // Start the Oracle Stuff //
            OracleClass oc = new OracleClass();
            oc.Connect("lynx","dang3r");
            oc.tran = oc.conn.BeginTransaction();
            oc.NonQueryText("update rt_upgrade u set u.upg_error_command_id=null, u.upg_error_ind='N', u.this_db_qualifies_ind='N' where u.upg_id='1'");
            oc.NonQueryText("delete from rt_upgrade_command");

            // Get all the schemas //
            List<string> lynxFoldersAll = GetLynxFolders();
            int numOfSchemas = lynxFoldersAll.Count;

            // Go through each schema folder //
            foreach (string lynxFolder in lynxFoldersAll)
            {
                // Each Folder write connect with section //
                WriteRunUpgrade(lynxFolder, numOfSchemas);
                string insertTextHold = "";
                bool inBlock = false;
                string readLine = "";
                int sectionNum = insertV.sectionNumber;

                // Go through each folder contained in the schema folder //
                var dir = Directory.GetDirectories(lynxFolder);
                foreach(string lynxSubFolder in dir)
                {
                    // If 900 Folder then write to finalize, does execution order need to change? //
                    insertV.finalIndication = "'N'";
                    if (lynxSubFolder.IndexOf("900") != -1)
                    {
                        WriteRunFinalize(lynxFolder, numOfSchemas);
                        insertV.finalIndication = "'Y'";
                        sectionNum = insertV.sectionNumber + numOfSchemas;
                    }
                    
                    // Go through each file in the sub folders //
                    var filesInLynxSubFolder = Directory.GetFiles(lynxSubFolder);
                    foreach (string filePath in filesInLynxSubFolder)
                    {
                        StreamReader file = new StreamReader(filePath);
                        while ((readLine = file.ReadLine()) != null)
                        {
                            insertV.executionOrder = insertV.executionOrder + 1000;
                            readLine = readLine.Replace("\r\n", "");

                            if (readLine == "")
                            {
                                // Ignore nothing //
                            }
                            //else if (readLine.IndexOf("--", StringComparison.OrdinalIgnoreCase) != -1)
                            //{
                                // This will now be in the final folder "900" instead of written on a line //
                                //if (readLine.IndexOf("finalization section", StringComparison.OrdinalIgnoreCase) != -1)
                                //    insertV.finalIndication = "'Y'";
                            //}
                            //else if (readLine.IndexOf("connect", StringComparison.OrdinalIgnoreCase) == 0)
                            //{
                                // Get Folder Name
                                // check for section 900 (sectionNumber + 30)
                                // this won't be connect anymore, it will just be the start of a new folder, so this can be moved up.
                                //insertV.sectionNumber++;
                                //insertTextHold = "";
                            //}
                            else
                            {
                                if (!inBlock)
                                    inBlock = CheckForBlock(readLine);

                                if (!inBlock)
                                {
                                    insertTextHold = insertTextHold + readLine + "\r\n";
                                    if (insertTextHold.IndexOf(";") != -1)
                                    {
                                        insertTextHold = insertTextHold.Replace(";", "");
                                        InsertIntoDatabase(ref insertTextHold, ref oc, sectionNum);
                                    }
                                }
                                else
                                {
                                    if (readLine == "/")
                                    {
                                        InsertIntoDatabase(ref insertTextHold, ref oc, sectionNum);
                                        inBlock = false;
                                    }
                                    else
                                    {
                                        insertTextHold = insertTextHold + readLine + "\r\n";
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Commit the transactions //
            oc.tran.Commit();
            oc.conn.Dispose();
            
            Console.WriteLine("File written to rt_upgrade_command."); 
        }

        /// <summary>
        /// Insert values into Oracle DB.
        /// </summary>
        /// <param name="insertTextHold"></param>
        public void InsertIntoDatabase(ref string insertTextHold, ref OracleClass oc, int sectionNum)
        {
            // Local Variables //
            string preview = "";

            // Setup Preview //
            if (insertTextHold.Length > 40)
                preview = "'" + ((insertTextHold.Substring(0, 40) + "...").Replace("\r\n", "")).Replace("'"," ") + "'";
            else
                preview = "'" + ((insertTextHold).Replace("\r\n", "")).Replace("'"," ") + "'";

            // Final funky format of insert text //
            insertV.retryType = GetRetryValue(insertTextHold);
            insertTextHold = insertTextHold.Replace("'", "''");
            insertTextHold = insertTextHold + "\r\n\r\n\r\n";
            insertTextHold = "'" + insertTextHold + "'";

            // Write insert to Oracle //
            string insertText = string.Format("insert into rt_upgrade_command (upg_id,execution_order,final_ind,retry_type,section,preview,upg_command,success_ind) values (1,{0},{1},{2},{3},{4},{5},'N')",
                insertV.executionOrder.ToString(),
                insertV.finalIndication,
                insertV.retryType,
                sectionNum.ToString(),
                preview,
                insertTextHold);

            // Check Size Constraint //
            if (insertText.Length >= 4000)
            {
                string declare = "";
                string insertDeclare = "";
                string testChar = "";
                const int MaxInsertSize = 30000;
                int chopPoint = MaxInsertSize;

                if (insertText.Length > MaxInsertSize)
                {
                    // Break into pieces //
                    insertTextHold = insertTextHold.Substring(1, insertTextHold.Length - 2); // remove the "'" from front and back
                    declare = "declare v_clob clob := null; begin ";

                    // Get substring and check for "'" //
                    testChar = insertTextHold.Substring(chopPoint, 1);
                    while((chopPoint > 1) && (testChar == "'"))
                    {
                        chopPoint--;
                        testChar = insertTextHold.Substring(chopPoint, 1);
                    }
                    
                    declare = declare + "v_clob := " + "'" + insertTextHold.Substring(0, chopPoint) + "'" + "; ";
                    insertTextHold = insertTextHold.Substring(chopPoint + 1);

                    while (insertTextHold.Length > chopPoint)
                    {
                        chopPoint = MaxInsertSize;
					    testChar = insertTextHold.Substring(chopPoint,1);
					    while ((chopPoint > 1) && (testChar == "'")) 
                        {
						    chopPoint--;
						    testChar = insertTextHold.Substring(chopPoint,1);
					    }
					    declare = declare + "v_clob := v_clob||" + "'" + insertTextHold.Substring(0, chopPoint) + "'" + "; ";
					    insertTextHold = insertTextHold.Substring(chopPoint + 1);
				    }

				    if (insertTextHold != "") 
                    {
					    declare = declare + "v_clob := v_clob||" + "'" + insertTextHold + "'" + "; ";
				    }

                    insertDeclare = string.Format("insert into rt_upgrade_command (upg_id,execution_order,final_ind,retry_type,section,preview,upg_command,success_ind) values (1,{0},{1},{2},{3},{4},{5},'N')",
                    insertV.executionOrder.ToString(),
                    insertV.finalIndication,
                    insertV.retryType,
                    sectionNum.ToString(),
                    preview,
                    "v_clob");

                    declare = declare + insertDeclare + "; end;";
                    oc.NonQueryText(declare);
                }
                else
                {
                    declare = "declare v_clob clob := null; begin v_clob := " + insertTextHold + "; ";
                    insertDeclare = string.Format("insert into rt_upgrade_command (upg_id,execution_order,final_ind,retry_type,section,preview,upg_command,success_ind) values (1,{0},{1},{2},{3},{4},{5},'N')",
                    insertV.executionOrder.ToString(),
                    insertV.finalIndication,
                    insertV.retryType,
                    sectionNum.ToString(),
                    preview,
                    "v_clob");

                    declare = declare + insertDeclare + "; end;";
                    oc.NonQueryText(declare);
                }
            }
            else
            {
                oc.NonQueryText(insertText);
            }
            
            // Clear text hold for next round //
            insertTextHold = "";
        }

        /// <summary>
        /// Get retry value based off key word.
        /// </summary>
        /// <param name="insertTextHold"></param>
        /// <returns></returns>
        public string GetRetryValue(string insertTextHold)
        {
            foreach (KeyValuePair<string, string> k in retryCount)
            {
                if (insertTextHold.IndexOf(k.Key, StringComparison.OrdinalIgnoreCase) != -1)
                    return k.Value;
            }

            return "1";
        }

        /// <summary>
        /// Check for starting key of a block.
        /// </summary>
        /// <param name="lowerLine"></param>
        /// <returns></returns>
        public bool CheckForBlock(string readLine)
        {
            foreach (string s in startInsert)
            {
                if (readLine.IndexOf(s, StringComparison.OrdinalIgnoreCase) != -1)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Get the lynx folder paths and store them.
        /// </summary>
        /// <returns></returns>
        public List<string> GetLynxFolders()
        {
            // Get the current and next Edition //
            using (var sr = new StreamReader(@"Z:\BrianF\X Project\Architecture\Build Automation\$X\Database\Trunk\DB Scripts\Current Edition.txt"))
            {
                currentEdition = sr.ReadLine();
                nextEdition = sr.ReadLine();
            }

            // Count number of schemas //
            List<string> lynxHold = new List<string>();
            string folderPath = @"Z:\BrianF\X Project\Architecture\Build Automation\$X\Database\Trunk\DB Scripts\10_Structure\" + currentEdition;
            string[] dir = Directory.GetDirectories(folderPath);
            foreach (string s in dir)
            {
                if (s.IndexOf("lynx",StringComparison.OrdinalIgnoreCase) != -1)
                    lynxHold.Add(s);
            }

            return lynxHold;
        }

        /// <summary>
        /// Write connect commands to run_upgrade.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="numOfSchemas"></param>
        public void WriteRunUpgrade(string folder, int numOfSchemas)
        {
            insertV.sectionNumber++;

            // Check for Edition Schema file //
            string editionString = "";
            if(File.Exists(folder + "\\Editioned Schema.txt"))
                editionString = "edition=" + currentEdition;

            // Get Lynx name only //
            string lynxFolderName = Path.GetFileName(folder);
            int i = lynxFolderName.IndexOf("lynx", StringComparison.OrdinalIgnoreCase);
            lynxFolderName = lynxFolderName.Remove(0, i).ToLower();

            // Get upgrad file //
            DirectoryInfo di = Directory.GetParent(folder);
            string pdcFile = di.FullName + "\\05_UPGRADE\\50_run_upgrade_shawn.pdc";
            if (insertV.sectionNumber == 1)
                File.Delete(pdcFile);

            // Write to 05_UPGRADE 50_run_upgrade_shawn.pdc //
            if (File.Exists(pdcFile))
            {
                using (StreamWriter sw = File.AppendText(pdcFile))
                {
                    string s = "";
                    if (dbConnections.TryGetValue(lynxFolderName, out s))
                    {
                        sw.WriteLine("connect " + s + " " + editionString);
                        sw.WriteLine("execute lynx.upgrade_pkg.upgrade(" + insertV.sectionNumber.ToString() + ");");
                    }
                }
            }
            else
            {
                FileStream fs = File.Create(pdcFile);
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    string s = "";
                    if (dbConnections.TryGetValue(lynxFolderName, out s))
                    {
                        sw.WriteLine("connect " + s + " " + editionString);
                        sw.WriteLine("execute lynx.upgrade_pkg.upgrade(" + insertV.sectionNumber.ToString() + ");");
                    }
                }
            }
        }

        /// <summary>
        /// Write connect commands to run_finalization.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="numOfSchemas"></param>
        public void WriteRunFinalize(string folder, int numOfSchemas)
        {
            // Check for Edition Schema file //
            string editionString = "";
            if (File.Exists(folder + "\\Editioned Schema.txt"))
                editionString = "edition=" + currentEdition;

            // Get Lynx name only //
            string lynxFolderName = Path.GetFileName(folder);
            int i = lynxFolderName.IndexOf("lynx", StringComparison.OrdinalIgnoreCase);
            lynxFolderName = lynxFolderName.Remove(0, i).ToLower();

            // Get finalized file //
            DirectoryInfo di = Directory.GetParent(folder);
            string pdcFile = di.FullName + "\\05_UPGRADE\\60_run_finalization_shawn.pdc";
            if (insertV.sectionNumber == 1)
                File.Delete(pdcFile);

            // Write to 05_UPGRADE 60_run_finalization_shawn.pdc //
            if (File.Exists(pdcFile))
            {
                using (StreamWriter sw = File.AppendText(pdcFile))
                {
                    string s = "";
                    if (dbConnections.TryGetValue(lynxFolderName, out s))
                    {
                        sw.WriteLine("connect " + s + " " + editionString);
                        sw.WriteLine("execute lynx.upgrade_pkg.upgrade(" + (insertV.sectionNumber + numOfSchemas).ToString() + ");");
                    }
                }
            }
            else
            {
                FileStream fs = File.Create(pdcFile);
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    string s = "";
                    if (dbConnections.TryGetValue(lynxFolderName, out s))
                    {
                        sw.WriteLine("connect " + s + " " + editionString);
                        sw.WriteLine("execute lynx.upgrade_pkg.upgrade(" + (insertV.sectionNumber + numOfSchemas).ToString() + ");");
                    }
                }
            }
        }
    }
}
