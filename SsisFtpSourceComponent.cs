using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Net;

//Gjermund Skobba - 2016.10.14
//https://bennyaustin.wordpress.com/2009/07/03/debugging-custom-ssis-components/
//https://www.simple-talk.com/sql/ssis/developing-a-custom-ssis-source-component/
//http://regexstorm.net/tester
//http://www.rebex.net/sftp.net/

namespace SsisFtpSource
{
    public class SsisFtpSourceComponent
    {
        [DtsPipelineComponent(DisplayName = "FTP Source",
             ComponentType = ComponentType.SourceAdapter, IconResource = "SsisFtpSource.ftp.ico")] //
 
        public class SsisSourceComponent : PipelineComponent
        {
            List<CustomDataColumn> _columnsList;
            FtpHelper _ftpHelper = new FtpHelper();

            public override void PrimeOutput(int outputs, int[] outputIDs, PipelineBuffer[] buffers)
            {
                //Do some stuff
                base.PrimeOutput(outputs, outputIDs, buffers);
                
                //SSIS STUFF
                IDTSOutput100 output = ComponentMetaData.OutputCollection.FindObjectByID(outputIDs[0]);
                PipelineBuffer buffer = buffers[0];



                string path = ComponentMetaData.CustomPropertyCollection["FTP Path"].Value;
                int nrOfDays = ComponentMetaData.CustomPropertyCollection["FTP Files Not Older Then (Days)"].Value;

                
                //Get path part of file path
                string pathPart = System.IO.Path.GetDirectoryName(path);


                //ftp://ftp.matchit.no/Maintenance/Archive/*.*
                FileStruct[] files = _ftpHelper.GetFiles(path);

                foreach (FileStruct file in files)
                {
                    //SKIP IF FILS IS OLDER THEN x DAYS
                    if (file.CreateTime < DateTime.Now.AddDays(-nrOfDays))
                        continue;

                    //BUILD THE PATH FOR EACH FILE
                    string filePath = System.IO.Path.Combine(pathPart, file.Name);
                    
                    //Switch "\" with "/"
                    filePath = filePath.Replace(@"\", @"/");

                    UriBuilder builder = new UriBuilder();
                    builder.Scheme = "ftp";
                    builder.Host = _ftpHelper.HostName;
                    builder.Path = filePath;

                    //GOT THE PATH
                    String url = builder.ToString();

                    //GET THE STREAM
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(url);
                    request.Method = WebRequestMethods.Ftp.DownloadFile;

                    //Login
                    string usr = ComponentMetaData.CustomPropertyCollection["FTP User"].Value;
                    string pwd = ComponentMetaData.CustomPropertyCollection["FTP Password"].Value;
                    request.Credentials = new NetworkCredential(usr, pwd);

                    FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                    //StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8, true);
                    Stream responseStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(responseStream);


                    //Get lines to skip
                    int rowsToSkip = ComponentMetaData.CustomPropertyCollection["Skip rows"].Value;

                    //READ THE STREAM
                    int currentLineNumber = 0;
                    while (!reader.EndOfStream)
                    {
                        try
                        {
                            //Read line
                            string line = reader.ReadLine();

                            //Skip x number of rows
                            if (rowsToSkip > currentLineNumber)
                            {
                                currentLineNumber++;
                                continue;
                            }

                            //splitt line
                            string separator = ComponentMetaData.CustomPropertyCollection["Separator"].Value;
                            char c = separator.ToCharArray()[0];
                            string[] values = line.Split(c);

                            //create output buffer
                            buffer.AddRow();

                            int i = 0;
                            foreach (CustomDataColumn column in _columnsList)
                            {
                                switch (column.Type)
                                {
                                    case "I4":
                                        buffer[i] = int.Parse(values[i]);
                                        break;
                                    case "WSTR":
                                        buffer[i] = values[i];
                                        break;
                                    case "DBTIMESTAMP":
                                        string dateFormatString = column.Length; //For DateTime "length" is used for describing datetime format
                                        buffer[i] = DateTime.ParseExact(values[i], dateFormatString, CultureInfo.InvariantCulture);
                                        break;
                                    default:
                                        throw new NotImplementedException("Error, " + column.Type + " is not implemented");
                                }
                                i++;
                            }

                            currentLineNumber++;

                        }
                        catch (Exception e)
                        { 
                            buffer.DirectErrorRow(0, 1, currentLineNumber);
                        }
                    }
                }

                buffer.SetEndOfRowset();
            }

            public override void PreExecute()
            {
                string columnDefinitions = ComponentMetaData.CustomPropertyCollection["Column Definition"].Value;
                _columnsList = GetDataColumn(columnDefinitions);

                string host = ComponentMetaData.CustomPropertyCollection["FTP Host"].Value;
                string usr = ComponentMetaData.CustomPropertyCollection["FTP User"].Value;
                string pwd = ComponentMetaData.CustomPropertyCollection["FTP Password"].Value;

                _ftpHelper.Connect(usr, pwd, host);

                base.PreExecute();
            }

            public override void ProcessInput(int inputID, PipelineBuffer buffer)
            {
                Debug.WriteLine("ProcessInput");
                int numberOfRows = buffer.RowCount;
                bool eof = buffer.EndOfRowset;
            }

            public override void ProvideComponentProperties()
            {
                //var test = SsisFtpSource.Resources.FTPSource;

                // Reset the component.  
                base.ProvideComponentProperties();
                base.RemoveAllInputsOutputsAndCustomProperties();
                ComponentMetaData.RuntimeConnectionCollection.RemoveAll();


                IDTSCustomProperty100 ftpHost = ComponentMetaData.CustomPropertyCollection.New();
                ftpHost.Description = "FTP host name";
                ftpHost.Name = "FTP Host";
                ftpHost.Value = "ftp.example.com";

                IDTSCustomProperty100 sftpUser = ComponentMetaData.CustomPropertyCollection.New();
                sftpUser.Description = "FTP user name";
                sftpUser.Name = "FTP User";
                sftpUser.Value = "[username]";

                IDTSCustomProperty100 sftpPass = ComponentMetaData.CustomPropertyCollection.New();
                sftpPass.Description = "FTP Password";
                sftpPass.Name = "FTP Password";
                sftpPass.Value = "[password]";

                IDTSCustomProperty100 sftpFilePath = ComponentMetaData.CustomPropertyCollection.New();
                sftpFilePath.Description = "FTP input file path";
                sftpFilePath.Name = "FTP Path";
                sftpFilePath.Value = "/Remote/Path/File_*.txt";

                IDTSCustomProperty100 sftpFromDate = ComponentMetaData.CustomPropertyCollection.New();
                sftpFromDate.Description = "FTP read only new files";
                sftpFromDate.Name = "FTP Files Not Older Then (Days)";
                sftpFromDate.Value = 1;

                IDTSCustomProperty100 columnSeparator = ComponentMetaData.CustomPropertyCollection.New();
                columnSeparator.Description = "Column separator";
                columnSeparator.Name = "Separator";
                columnSeparator.Value = ",";

                IDTSCustomProperty100 skipNumberOfRows = ComponentMetaData.CustomPropertyCollection.New();
                skipNumberOfRows.Description = "Skip number of rows";
                skipNumberOfRows.Name = "Skip rows";
                skipNumberOfRows.Value = 1;

                IDTSCustomProperty100 columnDefinition = ComponentMetaData.CustomPropertyCollection.New();
                columnDefinition.Description = "Column Definition";
                columnDefinition.Name = "Column Definition";
                columnDefinition.Value = "<ID>[I4],<Description>[WSTR](100),<Updated>[DBTIMESTAMP](yyyyMMdd)";


                ComponentMetaData.OutputCollection.RemoveAll();
                IDTSOutput100 output = ComponentMetaData.OutputCollection.New();
                output.Name = "Output";

                // Create output objects. This allows the custom component to have a 'Error' output data flow line
                IDTSOutput100 errorOutput = ComponentMetaData.OutputCollection.New();
                errorOutput.IsErrorOut = true;
                errorOutput.Name = "ErrorOutput";
                //errorOutput.SynchronousInputID = output.ID;
                //errorOutput.ExclusionGroup = 1;

                IDTSOutputColumn100 outputCol = errorOutput.OutputColumnCollection.New();
                outputCol.Name = "Error Line";
                outputCol.SetDataTypeProperties(DataType.DT_WSTR, 100, 0, 0, 0);

                
            }

            public List<CustomDataColumn> GetDataColumn(string columnDefinitions)
            {
                List<CustomDataColumn> columsList = new List<CustomDataColumn>();

                string regexString = @"\<(?<name>.*?)\>\[(?<type>.*?)\](\((?<length>.*?)\))?";
                MatchCollection matches = Regex.Matches(columnDefinitions, regexString);
                Debug.WriteLine("There were {0} matches:", matches.Count);

                //Create column list
                foreach (Match m in matches)
                {
                    string name = m.Groups["name"].Value;
                    string type = m.Groups["type"].Value;
                    string length = m.Groups["length"].Value;

                    CustomDataColumn dataColumn = new CustomDataColumn(name, type, length);
                    columsList.Add(dataColumn);
                }

                return columsList;
            }

            

            public override IDTSCustomProperty100 SetComponentProperty(string propertyName, object propertyValue)
            {
                if (propertyName == "Column Definition")
                {
                    //Get regex
                    string columnDefinitions = propertyValue.ToString();

                    List<CustomDataColumn> columsList = GetDataColumn(columnDefinitions);

                    //Remove old
                    ComponentMetaData.OutputCollection[0].OutputColumnCollection.RemoveAll();

                    //Add output columns
                    AddOutputColumns(columsList);
                }

                return base.SetComponentProperty(propertyName, propertyValue);
            }

            public void AddOutputColumns(List<CustomDataColumn> columnsList)
            {
                foreach (CustomDataColumn column in columnsList)
                {
                    Debug.WriteLine(column.Name + " - " + column.Type + " - " + column.Length);

                    IDTSOutputColumn100 outputCol = ComponentMetaData.OutputCollection[0].OutputColumnCollection.New();

                    bool isLong = false;
                    int length = 0;
                    int precision = 0;
                    int scale = 0;
                    DataType dType;
                    int codePage = 1250;

                    switch (column.Type)
                    {
                        case "I4":
                            dType = DataType.DT_I4;
                            length = 0;
                            precision = 0;
                            scale = 0;
                            codePage = 0;
                            break;
                        case "WSTR":
                            dType = DataType.DT_WSTR;
                            length = int.Parse(column.Length);
                            precision = 0;
                            scale = 0;
                            codePage = 0;
                            break;
                        case "DBTIMESTAMP":
                            dType = DataType.DT_DBTIMESTAMP;
                            length = 0;
                            precision = 0;
                            codePage = 0;
                            break;
                        default:
                            throw new NotImplementedException("Error, " + column.Type + " is not implemented");
                    }

                    outputCol.Name = column.Name;
                    outputCol.SetDataTypeProperties(dType, length, precision, scale, codePage);
                }
            }

 
        }
    }
}


////Do some more stuff
//IDTSOutput100 output = ComponentMetaData.OutputCollection.FindObjectByID(outputIDs[0]);
//PipelineBuffer buffer = buffers[0];

////Open file stream and create streamreader
//string remoteFilePath = ComponentMetaData.CustomPropertyCollection["FTP input file path"].Value;
//Stream stream = _ftp.GetStream(remoteFilePath, FileMode.Open);
//StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8, true);

////Get lines to skip
//int rowsToSkip = ComponentMetaData.CustomPropertyCollection["Skip rows"].Value;

////Read the stream
//int currentLineNumber = 0;
//while (!reader.EndOfStream)
//{
//    //read line
//    string line = reader.ReadLine();

//    //Skip x number of rows
//    if (rowsToSkip > currentLineNumber)
//    {
//        currentLineNumber++;
//        continue;
//    }

//    //splitt line
//    string separator = ComponentMetaData.CustomPropertyCollection["Separator"].Value;
//    char c = separator.ToCharArray()[0];
//    string[] values = line.Split(c);

//    //create output buffer
//    buffer.AddRow();

//    int i = 0;
//    foreach (CustomDataColumn column in _columnsList)
//    {
//        switch (column.Type)
//        {
//            case "I4":
//                buffer[i] = int.Parse(values[i]);
//                break;
//            case "WSTR":
//                buffer[i] = values[i];
//                break;
//            case "DBTIMESTAMP":
//                string dateFormatString = column.Length; //For DateTime "length" is used for describing datetime format
//                buffer[i] = DateTime.ParseExact(values[i], dateFormatString, CultureInfo.InvariantCulture);
//                break;
//            default:
//                throw new NotImplementedException("Error, " + column.Type + " is not implemented");
//        }
//        i++;
//    }

//    currentLineNumber++;
//}

//stream.Close();