using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace TSRC_Overview
{
    internal class IniFiles
    {
        public string inipath;

        [System.Reflection.Obfuscation(Exclude = true)]
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [System.Reflection.Obfuscation(Exclude = true)]
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
        /// <summary> 
        /// 
        /// </summary> 
        /// <param name="INIPath">文件路徑</param> 
        public IniFiles(string INIPath)
        {
            inipath = INIPath;
        }

        public IniFiles() { }

        /// <summary> 
        /// 寫入INI文件 
        /// </summary> 
        /// <param name="Section">項目名稱(如 [TypeName] )</param> 
        /// <param name="Key">鍵</param> 
        /// <param name="Value">值</param> 
        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.inipath);
        }
        /// <summary> 
        /// 讀出INI文件 
        /// </summary> 
        /// <param name="Section">項目名稱(如 [TypeName] )</param> 
        /// <param name="Key">鍵</param> 
        public string IniReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(500);
            int i = GetPrivateProfileString(Section, Key, "", temp, 500, this.inipath);
            return temp.ToString();
        }
        /// <summary> 
        /// 驗證文件是否存在 
        /// </summary> 
        /// <returns>布林值</returns> 
        public bool ExistINIFile()
        {
            return File.Exists(inipath);
        }
    
}
}
