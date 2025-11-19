using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using TSRC_Overview;

namespace Birthday
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        string LoadSetini = @"C:\Users\Public\Documents\Bill_Games\Setting\Config.ini";
 
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Trace.WriteLine("Checking setting...");
            //Check user set file
            if (!Directory.Exists(@"C:\Users\Public\Documents\Bill_Games"))
            {
                Directory.CreateDirectory(@"C:\Users\Public\Documents\Bill_Games\");

                Directory.CreateDirectory(@"C:\Users\Public\Documents\Bill_Games\Setting\");
            }

            IniFiles loadSet = new IniFiles(LoadSetini);
            if (!System.IO.File.Exists(LoadSetini))    
            {
                //建立預設設定值(LastSaveTime必須空白)
                loadSet.IniWriteValue("Save1", "Level", "1");
                loadSet.IniWriteValue("Save1","Chapter","0");
                loadSet.IniWriteValue("Save1","Money","0");
                loadSet.IniWriteValue("Save1","LastSaveTime","");

                loadSet.IniWriteValue("Save2", "Level", "1");
                loadSet.IniWriteValue("Save2", "Chapter", "0");
                loadSet.IniWriteValue("Save2", "Money", "0");
                loadSet.IniWriteValue("Save2", "LastSaveTime", "");

                loadSet.IniWriteValue("Save3", "Level", "1");
                loadSet.IniWriteValue("Save3", "Chapter", "0");
                loadSet.IniWriteValue("Save3", "Money", "0");
                loadSet.IniWriteValue("Save3", "LastSaveTime", "");
                Trace.WriteLine("找不到Config.ini，自動創建預設設定檔");
            }

            Trace.WriteLine("Check setting ok.");
        }
        
     }

}
