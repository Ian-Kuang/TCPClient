
using System.Configuration;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using TaskOneTCPClient;
using System.Xml.Linq;
using System.Drawing;


string IP = null;
int Port = 0;
try
{
    IP = ConfigurationManager.AppSettings["IP"];
    Port = int.Parse(ConfigurationManager.AppSettings["Port"]);
}
catch (Exception ex)
{
    Console.WriteLine($"Reading IP or Port failed! {ex.Message}");
}

AsyncTcpClient asyncTcpClient = new AsyncTcpClient() { Log = Log, MESHandle = MESHandle };
asyncTcpClient.ConnectServer(IP, Port);

StringBuilder stringBuilder = new StringBuilder();
while (true)
{
    var line = Console.ReadLine();

    if (line == "exit")
    {
        asyncTcpClient.Close();
        break;
    }

    stringBuilder.AppendLine(line);

    ConsoleKeyInfo keyInfo = Console.ReadKey(true);

    if (keyInfo.Key == ConsoleKey.Enter)
    {
        var cmd = stringBuilder.ToString();
        Log($"Sent cmd to TCP server {cmd}");
        asyncTcpClient.SendMsg(cmd);
        stringBuilder.Clear();
    }

    //Thread.Sleep(1000);
}

void Log(string message)
{
    Console.WriteLine(message, Color.Green);
}

void MESHandle(string message, Action<string> Send)
{
   
    if (!string.IsNullOrEmpty(message) && message.TrimEnd().EndsWith(">"))
    {
        string data = message.Substring(message.IndexOf("<REHM")); ;
        try
        {

            var doc = XDocument.Parse(message);
            var result = doc.Root.Element(Topic.PROCESS_INTERLOCK_REQs);

            if (result != null)
                if (result.Name == Topic.PROCESS_INTERLOCK_REQs)
                {
                    IEnumerable<XElement> element = doc.Descendants().Where(p => p.Name.LocalName == Topic.PROCESS_INTERLOCK_REQs);
                    XDocument xDocument = new XDocument();
                    XElement element1 = new XElement("REHM");
                    xDocument.Add(element1);
                    XElement element2 = new XElement(Topic.PROCESS_INTERLOCK_RESP);
                    element1.Add(element2);

                    var trace = element.Attributes().ToList();
                    var s_serialNo = element.Attributes("s_serialNo");
                    List<XAttribute> xAttributes = new List<XAttribute>();
                    Dictionary<string, string> keyValues = new Dictionary<string, string>();

                    foreach (var attr in trace)
                    {
                        keyValues.Add(attr.Name.ToString(), attr.Value.ToString());
                        //xAttributes.Add(new XAttribute(attr.Name, attr.Value.ToString()));
                    }

                    xAttributes.Add(new XAttribute("s_serialNo", keyValues["s_serialNo"]));
                   // xAttributes.Add(new XAttribute("s_product", keyValues["s_product"]));
                    xAttributes.Add(new XAttribute("s_productRevision", "2012-07-28T16:45:00+00:00"));
                    //xAttributes.Add(new XAttribute("s_program", ""));
                    xAttributes.Add(new XAttribute("s_programRevision", "2012-07-28T16:45:00+00:00"));
                    xAttributes.Add(new XAttribute("b_result", true));
                    element2.Add(xAttributes);


                    var cmdstring = xDocument.ToString();
                    Send(cmdstring);
                }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message, Color.Red);
        }

    }
}






