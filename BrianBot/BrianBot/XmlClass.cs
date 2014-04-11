using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace BrianBot
{
    class XmlClass
    {
        public Dictionary<string, string> XmlValues;
        
        /// <summary>
        /// Constructor.
        /// </summary>
        public XmlClass()
        {
            XmlValues = new Dictionary<string,string>();
            XmlValues.Add("MinPoolSize", "0");
            XmlValues.Add("ConnectionLifeTime", "120");
            XmlValues.Add("ConnectionTimeout", "15");
            XmlValues.Add("IncrPoolSize", "2");
            XmlValues.Add("DecrPoolSize", "2");
            XmlValues.Add("MaxPoolSize", "15");
            XmlValues.Add("ValidateConnection", "true");
            //XmlValues.Add("UserID", "lynx_app");
            //XmlValues.Add("Password", "f3line");
            //XmlValues.Add("Host", "oracleserver1");
            XmlValues.Add("UserID", "lynx");
            XmlValues.Add("Password", "dang3r");
            XmlValues.Add("Host", "oracledba-beta");
            XmlValues.Add("Protocol", "TCP");
            XmlValues.Add("Port", "1521");
            XmlValues.Add("ServiceName", "pldb");
            XmlValues.Add("StartLocation", @"\\pl2600\Users\BrianF\X Project\Architecture\Build Automation\Sample Source Control Structure\PLX_Dev\Database");
            XmlValues.Add("OracleLocation", @"C:\app\SScribner\product\11.2.0\client_1\BIN");
            XmlValues.Add("GitLocation", @"C:\Program Files (x86)\Git\bin\git.exe");
            XmlValues.Add("RepoLocation", @"C:\Work\GitHub\BrianBot");
        }

        /// <summary>
        /// Create a default XML file for testing.
        /// </summary>
        /// <param name="xmlBlock"></param>
        public void CreateDefault()
        {
            // Setup //
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.NewLineChars = "\r\n";
            settings.NewLineHandling = NewLineHandling.Replace;
            XmlWriter xWriter = XmlWriter.Create("XmlDefault.xml",settings);
            
            // Write XML File //
            Console.WriteLine("Created Default Xml File");
            xWriter.WriteStartElement("PenlinkDbSetup");
            foreach (var d in XmlValues)
            {
                xWriter.WriteStartElement(d.Key);
                xWriter.WriteAttributeString(d.Key, d.Value);
                xWriter.WriteEndElement();
            }
            xWriter.WriteEndElement();
            xWriter.Close();
        }

        /// <summary>
        /// Read XML File and return values.
        /// </summary>
        /// <param name="XmlValues"></param>
        /// <returns></returns>
        public bool ReadFile()
        {
            // Local Variables //
            bool exists = false;

            // Setup //
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;

            // Read XML File //
            if (File.Exists("XmlDefault.xml"))
            {
                XmlReader xReader = XmlReader.Create("XmlDefault.xml", settings);

                if (xReader != null)
                {
                    exists = true;
                    while (xReader.Read())
                    {
                        if (xReader.NodeType == XmlNodeType.Element)
                        {
                            if (xReader.Name != "PenlinkDbSetup")
                            {
                                //Console.WriteLine(xReader.Name + " - " + xReader.GetAttribute(xReader.Name));
                                XmlValues[xReader.Name] = xReader.GetAttribute(xReader.Name);
                            }
                        }
                    }

                    xReader.Close();
                } 
            }

            return exists;
        }
    }
}
