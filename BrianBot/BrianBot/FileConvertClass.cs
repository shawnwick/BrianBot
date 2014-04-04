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
        private List<string> _StartInsert;
        private Dictionary<string, string> _RetryCount;

        private InsertValues insertV; 
        public class InsertValues
        {
            public int SectionNumber;
            public int ExecutionOrder;
            public string FinalIndication;
            public string RetryType;
            public string UpgradId;
        }

        // This is for testing only //
        //StreamWriter sw = new StreamWriter(@"C:\Work\PL_SQL_Stuff\test_upgrade\Test.txt");
        
        public FileConvertClass()
        {
            insertV = new InsertValues();
            insertV.SectionNumber = 0;
            insertV.ExecutionOrder = 1000;
            insertV .FinalIndication= "'N'";
            insertV.RetryType = "1";
            insertV.UpgradId = "1";
            
            _StartInsert = new List<string>();
            _StartInsert.Add("create or replace and compile java source");
            _StartInsert.Add("create or replace trigger");
            _StartInsert.Add("create or replace package");
            _StartInsert.Add("create or replace procedure");
            _StartInsert.Add("create or replace function ");
            _StartInsert.Add("declare");
            _StartInsert.Add("begin");

            _RetryCount = new Dictionary<string, string>();
            _RetryCount.Add("drop", "2");
            _RetryCount.Add("revoke", "2");
            _RetryCount.Add("commit", "3");
            _RetryCount.Add("grant", "3");
            _RetryCount.Add("purge recyclebin", "3");
        }

        public void ReadFileToInsert(string FilePath)
        {
            // Local Variables //
            bool inBlock = false;
            string readLine = "";
            string insertHold = "";
            Console.WriteLine("Converting file " + FilePath + " to inserts into DB...");
            
            // Start the Oracle Stuff //
            OracleClass oc = new OracleClass();
            oc.Connect();
            oc.tran = oc.conn.BeginTransaction();
            oc.NonQuery("update rt_upgrade u set u.upg_error_command_id=null, u.upg_error_ind='N', u.this_db_qualifies_ind='N' where u.upg_id='1'");
            oc.NonQuery("delete from rt_upgrade_command");

            // @"C:\Work\PL_SQL_Stuff\test_upgrade\E03toE04.pdc"
            var filestream = new FileStream(FilePath,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);
            var file = new StreamReader(filestream, Encoding.UTF8, true, 128);

            while ((readLine = file.ReadLine()) != null)
            {
                // Make space between executions //
                insertV.ExecutionOrder = insertV.ExecutionOrder + 1000;
                readLine = readLine.Replace("\r\n", "");

                if (readLine == "")
                {
                    // Ignore nothing //
                }
                else if (readLine.IndexOf("--", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    if (readLine.IndexOf("finalization section", StringComparison.OrdinalIgnoreCase) != -1)
                        insertV.FinalIndication = "'Y'";
                }
                else if (readLine.IndexOf("connect", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    insertV.SectionNumber++;
                    insertHold = "";
                }
                else
                {
                    if (!inBlock)
                        inBlock = CheckForBlock(readLine);

                    if (!inBlock)
                    {
                        insertHold = insertHold + readLine + "\r\n";
                        if (insertHold.IndexOf(";") != -1)
                        {
                            insertHold = insertHold.Replace(";", "");
                            WriteInsertToOracle(ref insertHold,ref oc);
                        }
                    }
                    else
                    {
                        if (readLine == "/")
                        {
                            WriteInsertToOracle(ref insertHold, ref oc);
                            inBlock = false;
                        }
                        else
                        {
                            insertHold = insertHold + readLine + "\r\n";
                        }
                    }
                }
            }

            // Commit the transactions //
            oc.tran.Commit();
            oc.conn.Dispose();
            //sw.Close();
            
            Console.WriteLine("File written to rt_upgrade_command."); 
        }

        /// <summary>
        /// Insert values into Oracle DB.
        /// </summary>
        /// <param name="insertHold"></param>
        public void WriteInsertToOracle(ref string insertHold, ref OracleClass oc)
        {
            // Local Variables //
            string preview = "";

            // Setup Preview //
            if (insertHold.Length > 40)
                preview = "'" + ((insertHold.Substring(0, 40) + "...").Replace("\r\n", "")).Replace("'"," ") + "'";
            else
                preview = "'" + ((insertHold + "...").Replace("\r\n", "")).Replace("'"," ") + "'";

            // Final funky format of insert text //
            insertV.RetryType = GetRetryValue(insertHold);
            insertHold = insertHold.Replace("'", "''");
            insertHold = insertHold + "\r\n\r\n\r\n";
            insertHold = "'" + insertHold + "'";

            // Write insert to Oracle, test with text file first //
            string insertTest = string.Format("insert into rt_upgrade_command (upg_id,execution_order,final_ind,retry_type,section,preview,upg_command,success_ind) values (1,{0},{1},{2},{3},{4},{5},'N')",
                insertV.ExecutionOrder.ToString(),
                insertV.FinalIndication,
                insertV.RetryType,
                insertV.SectionNumber.ToString(),
                preview,
                insertHold);

            // Check Size Constraint //
            if (insertTest.Length >= 4000)
            {
                string declare = "";
                string insertDeclare = "";
                string testChar = "";
                int chopPoint = 30000;

                if (insertTest.Length > 30000)
                {
                    // Break in 30000 pieces //
                    insertHold = insertHold.Substring(1, insertHold.Length - 2); //remove the "'"
                    declare = "declare v_clob clob := null; begin ";

                    // Get substring and check for "'" //
                    testChar = insertHold.Substring(chopPoint, 1);
                    while((chopPoint > 1) && (testChar == "'"))
                    {
                        chopPoint--;
                        testChar = insertHold.Substring(chopPoint, 1);
                    }
                    
                    declare = declare + "v_clob := " + "'" + insertHold.Substring(0, chopPoint) + "'" + "; ";
                    insertHold = insertHold.Substring(chopPoint + 1);

                    while (insertHold.Length > chopPoint)
                    {
                        chopPoint = 30000;
					    testChar = insertHold.Substring(chopPoint,1);
					    while ((chopPoint > 1) && (testChar == "'")) 
                        {
						    chopPoint--;
						    testChar = insertHold.Substring(chopPoint,1);
					    }
					    declare = declare + "v_clob := v_clob||" + "'" + insertHold.Substring(0, chopPoint) + "'" + "; ";
					    insertHold = insertHold.Substring(chopPoint + 1);
				    }

				    if (insertHold != "") 
                    {
					    declare = declare + "v_clob := v_clob||" + "'" + insertHold + "'" + "; ";
				    }

                    insertDeclare = string.Format("insert into rt_upgrade_command (upg_id,execution_order,final_ind,retry_type,section,preview,upg_command,success_ind) values (1,{0},{1},{2},{3},{4},{5},'N')",
                    insertV.ExecutionOrder.ToString(),
                    insertV.FinalIndication,
                    insertV.RetryType,
                    insertV.SectionNumber.ToString(),
                    preview,
                    "v_clob");

                    declare = declare + insertDeclare + "; end;";
                    oc.NonQuery(declare);
                }
                else
                {
                    declare = "declare v_clob clob := null; begin v_clob := " + insertHold + "; ";
                    insertDeclare = string.Format("insert into rt_upgrade_command (upg_id,execution_order,final_ind,retry_type,section,preview,upg_command,success_ind) values (1,{0},{1},{2},{3},{4},{5},'N')",
                    insertV.ExecutionOrder.ToString(),
                    insertV.FinalIndication,
                    insertV.RetryType,
                    insertV.SectionNumber.ToString(),
                    preview,
                    "v_clob");

                    declare = declare + insertDeclare + "; end;";
                    oc.NonQuery(declare);
                }
            }
            else
            {
                oc.NonQuery(insertTest);
            }
            //sw.WriteLine(insertTest);
            insertHold = "";
        }

        /// <summary>
        /// Get retry value based off key word.
        /// </summary>
        /// <param name="insertHold"></param>
        /// <returns></returns>
        public string GetRetryValue(string insertHold)
        {
            foreach (KeyValuePair<string, string> k in _RetryCount)
            {
                if (insertHold.IndexOf(k.Key, StringComparison.OrdinalIgnoreCase) != -1)
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
            foreach (string s in _StartInsert)
            {
                if (readLine.IndexOf(s, StringComparison.OrdinalIgnoreCase) != -1)
                    return true;
            }

            return false;
        }     
    }
}
