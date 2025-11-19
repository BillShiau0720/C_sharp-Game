using Birthday.Story;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Birthday.Story
{
    public record StoryChoice(string Text, string? NextId);

    public class StoryStep
    {
        public string Id { get; init; } = string.Empty;
        public string? Title { get; init; }
        public string? BackgroundImage { get; init; }
        public string? CharacterImage { get; init; }
        public string? CharacterName { get; init; }
        public string? Dialogue { get; init; }
        public List<StoryChoice> Choices { get; init; } = new();
        public string? NextId { get; init; }

        public bool UseBattleLayout { get; init; }
        public string? PlayerStatus { get; init; }
        public string? AllyStatus { get; init; }
        public string? EnemyStatus { get; init; }
        public string? BattlePrompt { get; init; }
        public string? PlayerAvatar { get; init; }
        public string? AllyAvatar { get; init; }
        public string? EnemyAvatar { get; init; }

        public bool IsFinal => string.IsNullOrWhiteSpace(NextId) && Choices.Count == 0;
    }

    public class StoryService
    {
        private readonly Dictionary<string, StoryStep> _steps;
        private readonly string _entryId;
        private StoryStep? _currentStep;

        public event Action<StoryStep>? StepChanged;
        public event Action? StoryCompleted;

        private StoryService(IEnumerable<StoryStep> steps, string entryId)
        {
            _steps = steps.ToDictionary(step => step.Id);
            _entryId = entryId;
        }

        //story腳本
        public static StoryService CreateDemoStory()
        {
            var steps = new List<StoryStep>
            {
                new StoryStep
                {
                    Id = "intro",
                   Title = "第一章 · 星夜誓言",
                    BackgroundImage = "Assets/Image/Scenario/庭院.jpg",
                    CharacterImage = "Assets/Image/Figure/豹豹_全身.jpg",
                    CharacterName = "豹豹",
                    Dialogue = "兩人在星光下對招，劍氣拖出寒光。貓貓確認規則：『我是玩家主控，豹豹與敵人按劇本行動。我方行動完換敵方回合，輪流直到一方HP歸零。』豹豹笑說：『那就好好記住四式武功與攻防。』",
                    NextId = "system_brief"
                },
                new StoryStep
                {
                    Id = "system_brief",
                    Title = "第一章 · 體魄試煉",
                    BackgroundImage = "Assets/Image/Scenario/庭院.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "貓貓指向沙袋：『我HP 120，豹豹 110，訓練傀儡 90。一次行動可選普通攻擊、防禦，或四招武功：風剪、霜鎖、影縛、雷踏。也能使用道具像金創藥回復。先演練一次回合吧。』",
                    NextId = "tutorial_choice"
                },
                new StoryStep
                {
                    Id = "tutorial_choice",
                    Title = "第一章 · 招式演練",
                    BackgroundImage = "Assets/Image/Scenario/寺廟.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "訓練傀儡站好，貓貓提醒：『我先出手，豹豹會按腳本補刀，傀儡在敵方回合反擊。選一個動作測試節奏。』",
                    UseBattleLayout = true,
                    BattlePrompt = "回合制訓練：我方行動 → 敵方反擊 → 進入下一回合。",
                    PlayerStatus = "貓貓 HP 120/120 ｜ 主控先手",
                    AllyStatus = "豹豹 HP 110/110 ｜ 腳本護衛",
                    EnemyStatus = "訓練傀儡 HP 90/90",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/敵人_全身.png",
                    Choices =
                    {
                        new StoryChoice("普通攻擊：貓爪直刺，穩定扣減敵HP", "tutorial_attack"),
                        new StoryChoice("防禦：貓步後撤，提高下回合格檔", "tutorial_defend"),
                        new StoryChoice("武功·風剪四式：四連斬削弱敵防", "tutorial_skill1"),
                        new StoryChoice("武功·霜鎖月輪：二段劍勢附帶緩速", "tutorial_skill2"),
                        new StoryChoice("武功·影縛落爪：束縛敵人準備連擊", "tutorial_skill3"),
                        new StoryChoice("武功·雷踏裂地：高爆發消耗真氣", "tutorial_skill4"),
                        new StoryChoice("使用道具：金創藥，回復並穩定心態", "tutorial_item")
                    }
                },
                new StoryStep
                {
                    Id = "tutorial_attack",
                    Title = "第一章 · 普攻試手",
                    BackgroundImage = "Assets/Image/Scenario/主城二.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "貓貓刺出直擊，傀儡HP 90→72。豹豹腳本追擊至 50，敵方回合反撲 12 點，但防禦值尚存。訓練完成，進入實戰吧。",
                    UseBattleLayout = true,
                    BattlePrompt = "基礎普攻示範：之後切到敵方反擊。",
                    PlayerStatus = "貓貓 HP 120/120 ｜ 先手",
                    AllyStatus = "豹豹 HP 110/110 ｜ 腳本補刀",
                    EnemyStatus = "訓練傀儡 HP 50/90",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/敵人_全身.png",
                    NextId = "bandit_report"
                },
                new StoryStep
                {
                    Id = "tutorial_defend",
                    Title = "第一章 · 鐵尾防禦",
                    BackgroundImage = "Assets/Image/Scenario/主城二.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "貓貓後撤立尾，獲得格檔，傀儡攻擊只造成 4 點傷害。豹豹腳本反擊至傀儡HP 60。貓貓感受節奏：攻防交替才不會被反撲。",
                    UseBattleLayout = true,
                    BattlePrompt = "防禦示範：敵方回合後再輪回己方。",
                    PlayerStatus = "貓貓 HP 116/120 ｜ 格檔狀態",
                    AllyStatus = "豹豹 HP 110/110 ｜ 反擊完畢",
                    EnemyStatus = "訓練傀儡 HP 60/90",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/敵人_全身.png",
                    NextId = "bandit_report"
                },
                new StoryStep
                {
                    Id = "tutorial_skill1",
                    Title = "第一章 · 風剪試刀",
                    BackgroundImage = "Assets/Image/Scenario/主城二.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "四連風剪帶出破甲，傀儡HP 90→48，防禦下降。敵方回合的反擊被豹豹腳本壓制。貓貓點頭：『先削弱再收割。』",
                    UseBattleLayout = true,
                    BattlePrompt = "削弱示範：破甲後敵回合輸出降低。",
                    PlayerStatus = "貓貓 HP 120/120 ｜ 破甲成功",
                    AllyStatus = "豹豹 HP 110/110 ｜ 壓制中",
                    EnemyStatus = "訓練傀儡 HP 48/90 ｜ 防禦下降",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/敵人_全身.png",
                    NextId = "bandit_report"
                },
                new StoryStep
                {
                    Id = "tutorial_skill2",
                    Title = "第一章 · 霜鎖月輪",
                    BackgroundImage = "Assets/Image/Scenario/主城二.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "霜鎖兩段劍氣讓傀儡HP 90→58並緩速，下個敵方回合只打出 6 點。豹豹趁勢練拳，帶走剩餘血量。",
                    UseBattleLayout = true,
                    BattlePrompt = "緩速示範：敵方回合輸出下降。",
                    PlayerStatus = "貓貓 HP 120/120 ｜ 緩速成功",
                    AllyStatus = "豹豹 HP 110/110 ｜ 收尾",
                    EnemyStatus = "訓練傀儡 HP 0/90",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/敵人_全身.png",
                    NextId = "bandit_report"
                },
                new StoryStep
                {
                    Id = "tutorial_skill3",
                    Title = "第一章 · 影縛落爪",
                    BackgroundImage = "Assets/Image/Scenario/主城二.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "影縛定身讓傀儡無法在敵方回合行動。豹豹腳本接續『虎尾迴旋』，一擊清空 HP。貓貓微笑：『控制也是武功之一。』",
                    UseBattleLayout = true,
                    BattlePrompt = "控制示範：敵方回合被跳過。",
                    PlayerStatus = "貓貓 HP 120/120 ｜ 控制成功",
                    AllyStatus = "豹豹 HP 110/110 ｜ 追擊",
                    EnemyStatus = "訓練傀儡 HP 0/90",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/敵人_全身.png",
                    NextId = "bandit_report"
                },
                new StoryStep
                {
                    Id = "tutorial_skill4",
                    Title = "第一章 · 雷踏裂地",
                    BackgroundImage = "Assets/Image/Scenario/主城二.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "雷踏爆發直落 50 點，傀儡HP 剩 40。敵方回合重擊 12 點，提醒貓貓真氣換傷害要慎用。豹豹收尾，兩人記下節奏。",
                    UseBattleLayout = true,
                    BattlePrompt = "爆發示範：高傷後敵方反擊。",
                    PlayerStatus = "貓貓 HP 108/120 ｜ 真氣消耗",
                    AllyStatus = "豹豹 HP 110/110 ｜ 收尾",
                    EnemyStatus = "訓練傀儡 HP 0/90",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/敵人_全身.png",
                    NextId = "bandit_report"
                },
                new StoryStep
                {
                    Id = "tutorial_item",
                    Title = "第一章 · 金創藥效",
                    BackgroundImage = "Assets/Image/Scenario/主城二.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "貓貓使用金創藥將 HP 回滿，豹豹按腳本防守替她頂下 8 點傷害。『道具能救命，記得在關鍵回合用。』她將剩餘藥瓶收好。",
                    UseBattleLayout = true,
                    BattlePrompt = "道具示範：回合內立即生效，接著敵方行動。",
                    PlayerStatus = "貓貓 HP 120/120 ｜ 藥效充足",
                    AllyStatus = "豹豹 HP 102/110 ｜ 防守",
                    EnemyStatus = "訓練傀儡 HP 90/90",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/敵人_全身.png",
                    NextId = "bandit_report"
                },
                new StoryStep
                {
                    Id = "bandit_report",
                    Title = "第二章 · 山道伏訊",
                    BackgroundImage = "Assets/Image/Scenario/官門.jpg",
                    CharacterImage = "Assets/Image/Figure/女將軍_全身.jpg",
                    CharacterName = "女將軍",
                    Dialogue = "離城不久，女將軍傳來信鴿：山道匪徒挾持村民，疑似知曉劍譜下落。兩人決定先救人，再問線索。",
                    NextId = "bandit_battle_intro"
                },
                new StoryStep
                {
                    Id = "bandit_battle_intro",
                    Title = "第二章 · 山道回合戰",
                    BackgroundImage = "Assets/Image/Scenario/寺廟.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "匪徒列陣，貓貓與豹豹的 HP 分別為 120/120、110/110，敵方頭目 140/140。她提醒：『我控制貓貓，豹豹按腳本護住側翼。選擇動作後，就輪到敵方攻擊。』",
                    UseBattleLayout = true,
                    BattlePrompt = "請先下達指令，之後自動切換到敵方回合。",
                    PlayerStatus = "貓貓 HP 120/120 ｜ 主控",
                    AllyStatus = "豹豹 HP 110/110 ｜ 腳本護衛",
                    EnemyStatus = "山匪頭目 HP 140/140",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/敵人_全身.png",
                    NextId = "bandit_battle_choice"
                },
                new StoryStep
                {
                    Id = "bandit_battle_choice",
                    Title = "第二章 · 武功選擇",
                    BackgroundImage = "Assets/Image/Scenario/敵城.jpg",
                    CharacterImage = "Assets/Image/Figure/豹豹_全身.jpg",
                    CharacterName = "豹豹",
                    Dialogue = "豹豹舉劍，準備依腳本支援。貓貓需要當回合下指令：四項武功加上普通攻擊、防禦與道具。選錯會被敵方回合痛擊。",
                    UseBattleLayout = true,
                    BattlePrompt = "像寶可夢般的戰鬥介面：指令列在下方，依序輪換回合。",
                    PlayerStatus = "貓貓 HP 120/120 ｜ 指令待下達",
                    AllyStatus = "豹豹 HP 110/110 ｜ 待命",
                    EnemyStatus = "山匪頭目 HP 140/140",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/敵人_全身.png",
                    Choices =
                    {
                        new StoryChoice("普通攻擊：鎖喉刺擊，穩定輸出", "bandit_attack"),
                        new StoryChoice("防禦：貓步收刀，減免敵方回合傷害", "bandit_defend"),
                        new StoryChoice("武功·飛燕連斬：高速三劍，提升先手", "bandit_skill_swift"),
                        new StoryChoice("武功·霜步斬月：破防並附帶緩速", "bandit_skill_moon"),
                        new StoryChoice("武功·影縛勾鎖：控制敵人一回合", "bandit_skill_bind"),
                        new StoryChoice("武功·雷踏破陣：高傷耗氣，需豹豹收尾", "bandit_skill_thunder"),
                        new StoryChoice("使用道具：金創藥或爆裂符", "bandit_item"),
                        new StoryChoice("硬闖：不顧節奏衝刺", "player_defeat")
                    }
                },
                new StoryStep
                {
                    Id = "bandit_attack",
                    Title = "第二章 · 刀光試探",
                    BackgroundImage = "Assets/Image/Scenario/主城二.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "普通攻擊讓頭目 HP 140→120。敵方回合反擊 16 點，但豹豹腳本用肩膀硬吃只扣 6。貓貓喘口氣，決定再逼近核心。",
                    UseBattleLayout = true,
                    BattlePrompt = "選定指令後，敵方立即輪到行動。",
                    PlayerStatus = "貓貓 HP 104/120 ｜ 冷靜指揮",
                    AllyStatus = "豹豹 HP 104/110 ｜ 腳本反擊",
                    EnemyStatus = "山匪頭目 HP 120/140",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/敵人_全身.png",
                    NextId = "bandit_battle_result"
                },
                new StoryStep
                {
                    Id = "bandit_defend",
                    Title = "第二章 · 鐵尾護身",
                    BackgroundImage = "Assets/Image/Scenario/主城二.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "貓貓防禦，敵方回合的 18 點重劈被擋成 6 點。豹豹腳本趁縫用『虎尾迴旋』削到敵 HP 95，節奏穩住。",
                    UseBattleLayout = true,
                    BattlePrompt = "防禦後會換敵方行動，再回到我方。",
                    PlayerStatus = "貓貓 HP 114/120 ｜ 防禦加成",
                    AllyStatus = "豹豹 HP 110/110 ｜ 斜切準備",
                    EnemyStatus = "山匪頭目 HP 95/140",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/敵人_全身.png",
                    NextId = "bandit_battle_result"
                },
                new StoryStep
                {
                    Id = "bandit_skill_swift",
                    Title = "第二章 · 飛燕破陣",
                    BackgroundImage = "Assets/Image/Scenario/主城二.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "飛燕連斬打出先手，頭目 HP 140→96。敵方回合倉促還擊只造成 10 點。豹豹按腳本追擊，連斬逼退匪徒。",
                    UseBattleLayout = true,
                    BattlePrompt = "高速三斬後，敵人會在下一個回合衝撞。",
                    PlayerStatus = "貓貓 HP 110/120 ｜ 先手優勢",
                    AllyStatus = "豹豹 HP 104/110 ｜ 追擊準備",
                    EnemyStatus = "山匪頭目 HP 96/140",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/敵人_全身.png",
                    NextId = "bandit_battle_result"
                },
                new StoryStep
                {
                    Id = "bandit_skill_moon",
                    Title = "第二章 · 霜步斬月",
                    BackgroundImage = "Assets/Image/Scenario/主城二.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "霜步斬月破防，頭目 HP 掉到 82，並被緩速。敵方回合的刀勢遲緩只落在豹豹護肩。貓貓聽到村民喝彩。",
                    UseBattleLayout = true,
                    BattlePrompt = "破防後，敵人下一回合輸出下降。",
                    PlayerStatus = "貓貓 HP 112/120 ｜ 霜氣護體",
                    AllyStatus = "豹豹 HP 101/110 ｜ 防守反擊",
                    EnemyStatus = "山匪頭目 HP 82/140 ｜ 被緩速",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/敵人_全身.png",
                    NextId = "bandit_battle_result"
                },
                new StoryStep
                {
                    Id = "bandit_skill_bind",
                    Title = "第二章 · 影縛定形",
                    BackgroundImage = "Assets/Image/Scenario/主城二.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "影縛鎖住頭目，敵方回合被迫跳過。豹豹腳本重拳直墜，打到 HP 僅剩 70。貓貓準備下一擊收網。",
                    UseBattleLayout = true,
                    BattlePrompt = "控制類武功會跳過敵方行動，便於收割。",
                    PlayerStatus = "貓貓 HP 120/120 ｜ 影縛成功",
                    AllyStatus = "豹豹 HP 110/110 ｜ 準備終結",
                    EnemyStatus = "山匪頭目 HP 70/140 ｜ 無法行動",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/敵人_全身.png",
                    NextId = "bandit_battle_result"
                },
                new StoryStep
                {
                    Id = "bandit_skill_thunder",
                    Title = "第二章 · 雷踏破陣",
                    BackgroundImage = "Assets/Image/Scenario/主城二.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "雷踏爆擊將敵 HP 140→60，但貓貓真氣下滑，敵方回合打出 18 點重擊。豹豹腳本撐住，怒吼拖住陣腳。",
                    UseBattleLayout = true,
                    BattlePrompt = "高輸出伴隨敵方反擊，記得看血條。",
                    PlayerStatus = "貓貓 HP 92/120 ｜ 真氣消耗",
                    AllyStatus = "豹豹 HP 98/110 ｜ 撐住陣線",
                    EnemyStatus = "山匪頭目 HP 60/140",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/敵人_全身.png",
                    NextId = "bandit_battle_result"
                },
                new StoryStep
                {
                    Id = "bandit_item",
                    Title = "第二章 · 道具逆轉",
                    BackgroundImage = "Assets/Image/Scenario/主城二.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "貓貓吞下金創藥回到 110 HP，再丟出爆裂符讓頭目 HP 掉到 100。敵方回合慌亂，只打出 8 點，士氣崩潰。",
                    UseBattleLayout = true,
                    BattlePrompt = "道具同回合生效，之後換敵人出招。",
                    PlayerStatus = "貓貓 HP 110/120 ｜ 回復完成",
                    AllyStatus = "豹豹 HP 110/110 ｜ 戒備",
                    EnemyStatus = "山匪頭目 HP 100/140 ｜ 士氣下降",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/敵人_全身.png",
                    NextId = "bandit_battle_result"
                },
                new StoryStep
                {
                    Id = "bandit_battle_result",
                    Title = "第二章 · 獲得線索",
                    BackgroundImage = "Assets/Image/Scenario/庭院.jpg",
                    CharacterImage = "Assets/Image/Figure/女將軍_全身.jpg",
                    CharacterName = "女將軍",
                    Dialogue = "匪徒被擊潰，村民重獲自由。老人提供情報：下一章要前往東海青城，黑袍人正在搜刮武功。貓貓擦去刀痕，收好藥瓶與剩餘符紙。",
                    NextId = "seaside_city"
                },
                new StoryStep
                {
                    Id = "seaside_city",
                    Title = "第三章 · 青城潮聲",
                    BackgroundImage = "Assets/Image/Scenario/敵城.jpg",
                    CharacterImage = "Assets/Image/Figure/豹豹_全身.jpg",
                    CharacterName = "豹豹",
                    Dialogue = "東海青城籠罩濃霧，黑袍人以禁法操控海獸守城。貓貓摸著藥包：『HP 還夠，四式武功也恢復冷卻。豹豹，你按照腳本拖住守衛，我來主導回合。』",
                    NextId = "seaside_choice"
                },
                new StoryStep
                {
                    Id = "seaside_choice",
                    Title = "第三章 · 潮音迷蹤",
                    BackgroundImage = "Assets/Image/Scenario/寺廟.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "貓貓敲響船錨，巡邏被誘走。第一回合她用霜步斬月破鎖，第二回合豹豹腳本爆發雷踏，兩人趁巡邏空窗搶到殘頁。",
                    NextId = "seaside_result"
                },
                new StoryStep
                {
                    Id = "seaside_result",
                    Title = "第三章 · 暗潮將至",
                    BackgroundImage = "Assets/Image/Scenario/主城二.jpg",
                    CharacterImage = "Assets/Image/Figure/豹豹_全身.jpg",
                    CharacterName = "豹豹",
                    Dialogue = "殘頁記載最後一章在天璇峰之巔。黑袍首領步步逼近，貓貓檢查 HP 與道具存量：『決戰若失手，就會進入死亡畫面，得選讀檔或重來。』",
                    NextId = "final_battle_intro"
                },
                new StoryStep
                {
                    Id = "final_battle_intro",
                    Title = "第四章 · 天璇決戰",
                    BackgroundImage = "Assets/Image/Scenario/敵城.jpg",
                    CharacterImage = "Assets/Image/Figure/魔王_大頭照.jpg",
                    CharacterName = "黑袍首領",
                    Dialogue = "天璇峰寒風如刃，黑袍首領冷笑：『你們的回合制武功不過如此。』貓貓 HP 120，豹豹 95，首領 180。她深吸氣：『輪到我指揮，若失敗就會彈出死亡畫面。』",
                    UseBattleLayout = true,
                    BattlePrompt = "指令列與血條分離於戰場，交替回合。",
                    PlayerStatus = "貓貓 HP 120/120 ｜ 最終決戰",
                    AllyStatus = "豹豹 HP 95/110 ｜ 自動支援",
                    EnemyStatus = "黑袍首領 HP 180/180",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/魔王_全身.jpg",
                    NextId = "final_battle_choice"
                },
                new StoryStep
                {
                    Id = "final_battle_choice",
                    Title = "第四章 · 連攜武功",
                    BackgroundImage = "Assets/Image/Scenario/庭院.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "戰局將定生死。貓貓可在回合中選擇：普通攻擊、防禦、四招武功，或用最後的金創藥。首領會在敵方回合釋放魔焰。慎選。",
                    UseBattleLayout = true,
                    BattlePrompt = "下達指令後，會切換到敵方魔焰回合。",
                    PlayerStatus = "貓貓 HP 120/120 ｜ 指令中",
                    AllyStatus = "豹豹 HP 95/110 ｜ 待機",
                    EnemyStatus = "黑袍首領 HP 180/180",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/魔王_全身.jpg",
                    Choices =
                    {
                        new StoryChoice("霜雷雙絕：豹豹霜雷削弱，貓貓雷踏收割", "final_combo_storm"),
                        new StoryChoice("星落護連：貓貓星落防禦，豹豹蓄力反擊", "final_combo_guard"),
                        new StoryChoice("普攻與影縛：穩定打點並控制", "final_combo_control"),
                        new StoryChoice("道具急救：使用金創藥續命", "final_combo_item"),
                        new StoryChoice("硬撐不防守：正面硬吃魔焰", "player_defeat")
                    }
                },
                new StoryStep
                {
                    Id = "final_combo_storm",
                    Title = "第四章 · 霜雷定音",
                    BackgroundImage = "Assets/Image/Scenario/主城一.jpg",
                    CharacterImage = "Assets/Image/Figure/豹豹_全身.jpg",
                    CharacterName = "豹豹",
                    Dialogue = "第一回合豹豹霜雷劍勢打到首領 HP 180→140，敵方回合魔焰被冰意削弱。第二回合貓貓雷踏裂地終結戰鬥，避免了死亡畫面。",
                    UseBattleLayout = true,
                    BattlePrompt = "雙人連攜後，敵方魔焰回合失效。",
                    PlayerStatus = "貓貓 HP 104/120 ｜ 即將收割",
                    AllyStatus = "豹豹 HP 90/110 ｜ 霜雷加持",
                    EnemyStatus = "黑袍首領 HP 0/180",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/魔王_全身.jpg",
                    NextId = "epilogue"
                },
                new StoryStep
                {
                    Id = "final_combo_guard",
                    Title = "第四章 · 星落反擊",
                    BackgroundImage = "Assets/Image/Scenario/主城一.jpg",
                    CharacterImage = "Assets/Image/Figure/豹豹_全身.jpg",
                    CharacterName = "豹豹",
                    Dialogue = "貓貓星落防禦，敵方回合魔焰只造成 6 點。豹豹蓄力後以『玄霜逆流』回敬 60 點，兩回合內反殺首領。",
                    UseBattleLayout = true,
                    BattlePrompt = "防禦動畫與血條分開顯示，清楚看回合。",
                    PlayerStatus = "貓貓 HP 96/120 ｜ 星落護盾",
                    AllyStatus = "豹豹 HP 95/110 ｜ 逆流待發",
                    EnemyStatus = "黑袍首領 HP 0/180",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/魔王_全身.jpg",
                    NextId = "epilogue"
                },
                new StoryStep
                {
                    Id = "final_combo_control",
                    Title = "第四章 · 影縛斷勢",
                    BackgroundImage = "Assets/Image/Scenario/主城一.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "貓貓先以普通攻擊試探，再用影縛鎖足。首領敵方回合被迫跳過，豹豹腳本雷踏直擊，HP 只剩 20。最後一擊收尾，躲過死亡畫面。",
                    UseBattleLayout = true,
                    BattlePrompt = "控制成功會跳過敵方回合，血條在上方同步。",
                    PlayerStatus = "貓貓 HP 118/120 ｜ 影縛控制",
                    AllyStatus = "豹豹 HP 95/110 ｜ 雷踏蓄力",
                    EnemyStatus = "黑袍首領 HP 20/180 ｜ 無法行動",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/魔王_全身.jpg",
                    NextId = "epilogue"
                },
                new StoryStep
                {
                    Id = "final_combo_item",
                    Title = "第四章 · 藥效續命",
                    BackgroundImage = "Assets/Image/Scenario/主城一.jpg",
                    CharacterImage = "Assets/Image/Figure/豹豹_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "貓貓喝下最後一瓶金創藥，HP 回到 90。敵方回合魔焰仍灼傷 14 點，但豹豹腳本防守住。補血後使用霜步斬月終結首領。",
                    UseBattleLayout = true,
                    BattlePrompt = "補給後照樣切換到敵方行動，血量即時更新。",
                    PlayerStatus = "貓貓 HP 90/120 ｜ 藥效生效",
                    AllyStatus = "豹豹 HP 92/110 ｜ 防守",
                    EnemyStatus = "黑袍首領 HP 0/180",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/魔王_全身.jpg",
                    NextId = "epilogue"
                },
                new StoryStep
                {
                    Id = "player_defeat",
                    Title = "死亡畫面",
                    BackgroundImage = "Assets/Image/Scenario/敵城.jpg",
                    CharacterImage = "Assets/Image/Figure/魔王_大頭照.jpg",
                    CharacterName = "系統",
                    Dialogue = "貓貓HP 歸零，畫面迅速變暗。系統彈出選單：讀取存檔或重來。豹豹的聲音若隱若現：『別放棄，我會等你。』",
                    UseBattleLayout = true,
                    BattlePrompt = "死亡畫面：選擇讀檔或重來。",
                    PlayerStatus = "貓貓 HP 0/120 ｜ 戰敗",
                    AllyStatus = "豹豹 HP ? ｜ 無法行動",
                    EnemyStatus = "敵方行動完畢",
                    PlayerAvatar = "Assets/Image/Figure/貓貓_全身.jpg",
                    AllyAvatar = "Assets/Image/Figure/豹豹_全身.jpg",
                    EnemyAvatar = "Assets/Image/Figure/魔王_全身.jpg",
                    Choices =
                    {
                        new StoryChoice("讀檔：回到山道戰前存檔", "bandit_battle_intro"),
                        new StoryChoice("重來：從序章重新開始", "intro")
                    }
                },
                new StoryStep
                {
                    Id = "epilogue",
                    Title = "終章 · 迎向晨曦",
                    BackgroundImage = "Assets/Image/Scenario/庭院.jpg",
                    CharacterImage = "Assets/Image/Figure/豹豹_全身.jpg",
                    CharacterName = "豹豹",
                    Dialogue = "黎明灑滿峰頂，豹豹與貓貓合上完整的《玄霜劍譜》。四式武功、道具與攻防節奏都刻進骨血，他們相視而笑，準備迎接下一段江湖旅途。"
                }
            };

            return new StoryService(steps, "intro");
        }

        //Need to add fight.

        public void Start() => Start(_entryId);

        public void Start(string stepId)
        {
            if (!_steps.TryGetValue(stepId, out var step))
            {
                throw new ArgumentException($"找不到劇情節點 {stepId}", nameof(stepId));
            }

            _currentStep = step;
            StepChanged?.Invoke(step);
        }

        public void Continue()
        {
            if (_currentStep == null)
            {
                StoryCompleted?.Invoke();
                return;
            }

            if (_currentStep.Choices.Count > 0)
            {
                // 等待玩家做出選擇
                return;
            }

            if (string.IsNullOrWhiteSpace(_currentStep.NextId))
            {
                _currentStep = null;
                StoryCompleted?.Invoke();
                return;
            }

            if (!_steps.TryGetValue(_currentStep.NextId, out var nextStep))
            {
                _currentStep = null;
                StoryCompleted?.Invoke();
                return;
            }

            _currentStep = nextStep;
            StepChanged?.Invoke(nextStep);
        }

        public void Choose(string? nextId)
        {
            if (string.IsNullOrWhiteSpace(nextId))
            {
                Continue();
                return;
            }

            if (!_steps.TryGetValue(nextId, out var nextStep))
            {
                _currentStep = null;
                StoryCompleted?.Invoke();
                return;
            }

            _currentStep = nextStep;
            StepChanged?.Invoke(nextStep);
        }
    }
}