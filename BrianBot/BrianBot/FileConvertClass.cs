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
        public void ReadFilesToInsert(string scriptType)
        {
            Console.WriteLine("Converting " + scriptType + " files to inserts into DB...");
            insertV.sectionNumber = 0;
            
            // Start the Oracle Stuff //
            OracleClass oc = new OracleClass();
            oc.Connect();
            oc.tran = oc.conn.BeginTransaction();
            
            // Get all the schema folders //
            List<string> lynxFoldersAll;
            if (scriptType == "Structure")
            {
                oc.NonQueryText("update rt_upgrade u set u.upg_error_command_id=null, u.upg_error_ind='N', u.this_db_qualifies_ind='N' where u.upg_id='1'");
                oc.NonQueryText("delete from rt_upgrade_command");
                lynxFoldersAll = GetStructureLynxFolders();
            }
            else
            {
                lynxFoldersAll = GetCodeLynxFolders();
            }

            // Go through each schema folder //
            foreach (string lynxFolder in lynxFoldersAll)
            {
                // Each Folder write connect with section //
                insertV.sectionNumber++;
                if (scriptType == "Structure")
                    WriteRunUpgrade(lynxFolder);
                
                // Go through each folder contained in the schema folder //
                var lynxSubFolderList = Directory.GetDirectories(lynxFolder);
                foreach(string lynxSubFolder in lynxSubFolderList)
                {
                    // If 900 Folder then write to finalize, does execution order need to change? //
                    insertV.finalIndication = "'N'";
                    int sectionNum = insertV.sectionNumber;
                    if (lynxSubFolder.IndexOf("900") != -1)
                    {
                        insertV.finalIndication = "'Y'";
                        sectionNum = insertV.sectionNumber + lynxFoldersAll.Count;
                        if (scriptType == "Structure")
                            WriteRunFinalize(lynxFolder, lynxFoldersAll.Count);
                    }
                    
                    // Go through each file in the sub folders //
                    var filesInLynxSubFolder = Directory.GetFiles(lynxSubFolder);
                    foreach (string filePath in filesInLynxSubFolder)
                        ParseForDbInsert(ref oc, filePath, sectionNum);    
                }
            }

            // Commit the transactions //
            oc.tran.Commit();
            oc.conn.Dispose();
            
            Console.WriteLine(scriptType + " files written to rt_upgrade_command.");
            Console.WriteLine();
        }

        /// <summary>
        /// Parse out the file and insert the commands into the rt_upgrade_command table.
        /// </summary>
        /// <param name="oc"></param>
        /// <param name="filePath"></param>
        /// <param name="sectionNum"></param>
        public void ParseForDbInsert(ref OracleClass oc, string filePath, int sectionNum)
        {
            bool inBlock = false;
            string readLine = "";
            string insertTextHold = "";
            StreamReader file = new StreamReader(filePath);

            while ((readLine = file.ReadLine()) != null)
            {
                insertV.executionOrder = insertV.executionOrder + 1000;
                readLine = readLine.Replace("\r\n", "");

                if (readLine == "")
                {
                    // Ignore nothing //
                }
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
					    declare = declare + "v_clob := v_clob||" + "'" + insertTextHold + "'" + "; ";

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
        /// Get the structure lynx folder paths and store them.
        /// </summary>
        /// <returns></returns>
        public List<string> GetStructureLynxFolders()
        {
            try
            {
                // For testing right now
                XmlClass xClass = new XmlClass();
                xClass.ReadFile();
                string baseFolder = xClass.XmlValues["StartLocation"];

                // Get the current and next Edition //
                using (var sr = new StreamReader(baseFolder + "\\DB Scripts\\Current Edition.txt"))
                {
                    currentEdition = sr.ReadLine();
                    nextEdition = sr.ReadLine();
                }

                // Count number of schemas //
                List<string> lynxFolderHold = new List<string>();
                string folderPath = baseFolder + "\\DB Scripts\\10_Structure\\" + currentEdition;
                string[] dir = Directory.GetDirectories(folderPath);
                foreach (string s in dir)
                {
                    if (s.IndexOf("lynx", StringComparison.OrdinalIgnoreCase) != -1)
                        lynxFolderHold.Add(s);
                }

                return lynxFolderHold;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Get the code lynx folder paths and store them.
        /// </summary>
        /// <returns></returns>
        public List<string> GetCodeLynxFolders()
        {
            try
            {
                // For testing right now
                XmlClass xClass = new XmlClass();
                xClass.ReadFile();
                string baseFolder = xClass.XmlValues["StartLocation"];

                // Count number of schemas //
                List<string> lynxFolderHold = new List<string>();
                string folderPath = baseFolder + "\\DB Scripts\\20_Code";
                string[] dir = Directory.GetDirectories(folderPath);
                foreach (string s in dir)
                {
                    if (s.IndexOf("lynx", StringComparison.OrdinalIgnoreCase) != -1)
                        lynxFolderHold.Add(s);
                }

                return lynxFolderHold;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Write connect commands to run_upgrade.
        /// </summary>
        /// <param name="lynxFolder"></param>
        public void WriteRunUpgrade(string lynxFolder)
        {
            // Check for Edition Schema file //
            string editionString = "";
            if(File.Exists(lynxFolder + "\\Editioned Schema.txt"))
                editionString = "edition=" + currentEdition;

            // Get Lynx name only //
            string lynxFolderName = Path.GetFileName(lynxFolder);
            int i = lynxFolderName.IndexOf("lynx", StringComparison.OrdinalIgnoreCase);
            lynxFolderName = lynxFolderName.Remove(0, i).ToLower();

            // Get upgrade file //
            DirectoryInfo di = Directory.GetParent(lynxFolder);
            string pdcFile = di.FullName + "\\05_UPGRADE\\50_run_upgrade_shawn.pdc";
            if (File.Exists(pdcFile))
            {
                if (insertV.sectionNumber == 1)
                    File.WriteAllText(pdcFile, string.Empty);
            }
            else
            {
                FileStream fs = File.Create(pdcFile);
                fs.Close();
            }

            // Write to 05_UPGRADE/50_run_upgrade_shawn.pdc //
            using (StreamWriter sw = File.AppendText(pdcFile))
            {
                string namePassword = "";
                if (dbConnections.TryGetValue(lynxFolderName, out namePassword))
                {
                    sw.WriteLine("connect " + namePassword + " " + editionString);
                    sw.WriteLine("execute lynx.upgrade_pkg.upgrade(" + insertV.sectionNumber.ToString() + ");");
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
            if (File.Exists(pdcFile))
            {
                if (insertV.sectionNumber == 1)
                    File.WriteAllText(pdcFile, string.Empty);
            }
            else
            {
                FileStream fs = File.Create(pdcFile);
                fs.Close();
            }

            // Write to 05_UPGRADE/60_run_finalization_shawn.pdc //
            using (StreamWriter sw = File.AppendText(pdcFile))
            {
                string namePassword = "";
                if (dbConnections.TryGetValue(lynxFolderName, out namePassword))
                {
                    sw.WriteLine("connect " + namePassword + " " + editionString);
                    sw.WriteLine("execute lynx.upgrade_pkg.upgrade(" + (insertV.sectionNumber + numOfSchemas).ToString() + ");");
                }
            }
        }
    }
}
