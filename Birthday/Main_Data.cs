using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Birthday
{
    internal class Main_Data
    {
        //目前遊戲暫存資訊
        public static int Now_Level { get; set; }       //目前等級
        public static int Now_Chapter {  get; set; }    //目前章節
        public static int Now_Health { get; set; }     //目前血量
        public static int Now_Money { get; set; }   //目前金額
        public static int Last_SaveTime { get; set; }   //最後存檔時間
        public static bool Status_IsSaving { get; set; }       //是否正在儲存

    }
}
