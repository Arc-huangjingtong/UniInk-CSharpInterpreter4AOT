namespace Arc.UniInk.BestPractices
{

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Arc.UniInk;


    public partial class DuelAction
    {
        /// <summary> 所有的卡片配置数据 </summary>
        public List<CardData_Base> CardConfig;

        /// <summary> 所有的武将配置数据 </summary>
        public ReadOnlyCollection<CardData_Officer> OfficerConfig => GameConfig.CollectionOfficerCards;

        /// <summary> 所有的士兵配置数据 </summary>
        public ReadOnlyCollection<CardData_Soldier> SoldierConfig => GameConfig.CollectionSoldierCards;

        /// <summary> 所有的技能配置数据 </summary>
        public ReadOnlyDictionary<int, AbilityData_Base> AbilityConfig => GameConfig.CollectionAbilities;

        /// <summary> 当前结算的效果 </summary>
        public EffectEntry currentEntry { get; set; }

        /// <summary>当前效应卡选中的目标</summary>
        public RuntimeCard Target { get; set; }

        /// <summary>当前兵卡可增加的兵卡</summary>
        public List<ESoldierType> Soldiers { get; set; }

        /// <summary>用于发现操作,暂时存储的选项</summary>
        public List<int> Options;

        /// <summary>发现面板中选中的选项</summary>
        public int Select;

        /// <summary>发现面板中选中的选项</summary>
        public string OnSelect;


        /// <summary>指向场景对象实例</summary>
        private DuelObjectManager Scenes { get; set; }

        private DuelAreaManager Areas { get; set; }

        private DuelPlayerManager Players { get; set; }

        private DuelCommandManager Commands { get; set; }

        /// <summary>指向决斗数据</summary>
        private DuelData Data { get; set; }


        public static UniInk_Speed Evaluator;



        public void Finalization()
        {
            Data      = null;
            Scenes    = null;
            Evaluator = null;
        }

        /// <summary> (待删除)创建一个UniInk实例 </summary>
        public UniInk_Speed CreateInk()
        {
            // var ans = new UniInk(this);
            //
            // ans.OnEvaluateScript += (sender, args) =>
            // {
            //     args.Expression = args.Expression.Trim() + ";";
            //     //Debug.Log($"执行脚本:{args.Expression}");
            //     args.Expression = args.Expression.Replace("；", ";") //中文分号转英文分号
            //                           .Replace(";;", ";")           //连续分号转换为单个分号
            //                           .Replace("（",  "(")           //中文括号转英文
            //                           .Replace("）",  ")");          //中文括号转英文
            // };
            //
            // ans.PreEvaluateFunction += (_, args) =>
            // {
            //     if (args.Name != "FLT" || args.Args.Count <= 1) return;
            //
            //     var str = args.Args[1];
            //     args.Args[1] = $"c=>{str}";
            // };
            //
            // return ans;

            return new UniInk_Speed();
        }


        /// <summary> 当前结算的卡片 </summary>
        public RuntimeCard EntryCard() => new RuntimeCard();

        /// <summary> 触发当前结算的卡片 </summary>
        public RuntimeCard EntryTriggerCard() => new RuntimeCard();

        /// <summary> 触发当前结算的卡片2(对撞被视为由两个对象同时触发) </summary>
        public RuntimeCard EntryTriggerCard2() => new RuntimeCard();

        /// <summary> 当前结算的玩家 </summary>
        public RuntimePlayer EntryPlayer() => new RuntimePlayer();

        /// <summary> 当前结算的对手 </summary>
        public RuntimePlayer EntryOpponent() => new RuntimePlayer();


        //=============================================== 指令部分 ================================================//


        /// <summary> 指令:删除一张卡片 </summary>
        /// <param name="card"> 删除目标 </param>
        /// <remarks> 删除:被删除的卡片会从游戏中彻底消失 </remarks>
        public void DEL(RuntimeCard card) => Console.WriteLine("DEL" + card.ID);

        public void DEL(List<RuntimeCard> cards) => cards?.ForEach(DEL);

        /// <summary>指令:破坏一张卡片</summary>
        /// <param name="card">破坏目标</param>
        /// <remarks>破坏:被破坏的卡片会送入<b>消耗区</b></remarks>
        public void DES(RuntimeCard card) => Console.WriteLine("DES" + card.ID);

        public void DES(List<RuntimeCard> cards) => cards.ForEach(DES);

        /// <summary>指令:给一个技能容器增加技能</summary>
        /// <param name="holder">被增加技能的容器</param>
        /// <param name="AbilityID">技能图鉴中的ID</param>
        /// <param name="AutoDestory">是否自动销毁,默认开启,开启后这个技能会被一些定义好的规则删除</param>
        /// <param name="level">技能层级,在一些特殊的层数技能中会有效果</param>
        public void AAB(RuntimeCard holder, int abilityID, bool AutoDestroy = true) => Console.WriteLine("AAB" + holder.ID + abilityID + AutoDestroy);

        public void AAB(RuntimePlayer player, int abilityID, bool AutoDestroy = true) => Console.WriteLine("AAB" + player.ID + abilityID + AutoDestroy);

        public void AAB(List<RuntimeCard> holders, int abilityID, bool AutoDestroy = true) => holders?.ForEach(x => AAB(x, abilityID, AutoDestroy));


        /// <summary>指令:给一个技能容器移除技能</summary>
        /// <param name="holder">被移除技能的容器</param>
        /// <param name="AbilityID">技能图鉴中的ID</param>
        public void RAB(RuntimeCard holder, int abilityID) => Console.WriteLine("RAB" + holder.ID + abilityID);

        public void RAB(List<RuntimeCard> holders, int abilityID) => holders?.ForEach(x => RAB(x, abilityID));

        /// <summary>指令:判断对象是否有指定的技能</summary>
        /// <param name="holder">被移除技能的容器</param>
        /// <param name="AbilityID">技能图鉴中的ID</param>
        public bool HAB(RuntimeCard holder, int abilityID) => holder.ID == abilityID;

        public bool HAB(List<RuntimeCard> holders, int abilityID) => holders?.Exists(x => HAB(x, abilityID)) ?? false;

        /// <summary>指令:从卡片图鉴中生成一张卡到指定玩家的指定坐标</summary>
        /// <param name="cardID">卡片的图鉴ID</param>
        /// <param name="playerID">玩家对象</param>
        /// <param name="pos">生成坐标</param>
        /// <returns>生成的这张卡</returns>
        public RuntimeCard CRE(int cardID, RuntimePlayer player, EPos pos) => new RuntimeCard();

        public RuntimeCard CRE(int cardID, RuntimePlayer player, int pos) => CRE(cardID, player, (EPos)pos);


        /// <summary>指令:给一个[RuntimeCard]对象添加一个Buff</summary>
        /// <param name="entry">效果结算对象(谁给予了这个BUF)</param>
        /// <param name="value">Buff的增益/减益值</param>
        /// <param name="param">参数名,可以自由扩展<see cref="GameParam"/></param>
        /// <param name="autoDestroy">是否自动销毁的标记,默认为真(会按照一定的逻辑用指定方式一起销毁)</param>
        public void BUF(RuntimeCard card, string param, int value, bool autoDestroy = true) => Console.WriteLine("BUF" + card.ID + param + value + autoDestroy);

        public void BUF(RuntimeCard       card,  string param, int value, int  type        = 1)    => Console.WriteLine("BUF" + card.ID + param + value + type);
        public void BUF(List<RuntimeCard> cards, string param, int value, bool autoDestroy = true) => cards.ForEach(x => BUF(x, param, value, autoDestroy));


        /// <summary>指令:设置一个对象的参数值</summary>
        /// <param name="value">设置的值</param>
        /// <param name="param">参数名,可以自由扩展</param>
        /// <param name="runtimeObject">对象:可以是游戏中的任何对象，卡牌，地形，玩家，等等</param>
        public void SET(RuntimeCard card, string param, int? value) => Console.WriteLine("SET" + card.ID + param + value);

        public void SET(List<RuntimeCard> card, string param, int? value) => card.ForEach(x => SET(x, param, value));

        public void SET(RuntimePlayer player, string param, int? value) => Console.WriteLine("SET" + player.ID + param + value);


        /// <summary>指令:效果的持有者对指定卡片造成伤害</summary>
        /// <param name="card">伤害承受者</param>
        /// <param name="damage">伤害值 </param>
        public void DMG(RuntimeCard card, int damage, int soldierType = 4) => Console.WriteLine("DMG" + card.ID + damage + soldierType);


        public void DMG(List<RuntimeCard> cards, int damage, int soldierType = 4) => cards?.ForEach(x => DMG(x, damage, soldierType));


        /// <summary>指令:效果的持有者对指定玩家造成伤害</summary>
        /// <param name="player">伤害承受者</param>
        /// <param name="damage">伤害值   </param>
        public void DMG_P(RuntimePlayer player, int damage) => Console.WriteLine("DMG_P" + player.ID + damage);


        public void LOSS(RuntimeCard card, int damage, int soldierType = 4) => DMG(card, damage, soldierType);

        public void LOSS(List<RuntimeCard> cards, int damage, int soldierType = 4) => cards?.ForEach(x => DMG(x, damage, soldierType));



        ///获取一个对象的属性值
        public int GET(RuntimeCard card, string param) => card.ID + param.Length;

        public int GET(RuntimeCard   card,   int    param) => card.ID   + param;
        public int GET(CardData_Base card,   string param) => card.ID   + param.Length;
        public int GET(RuntimePlayer player, string param) => player.ID + param.Length;

        /// <summary>指令:增加一个对象的参数值</summary>
        /// <param name="value">设置的值</param>
        /// <param name="param">参数名,可以自由扩展</param>
        /// <param name="card">对象:可以是游戏中的任何对象，卡牌，地形，玩家，等等</param>
        public void ADD(RuntimeCard card, string param, int value) => Console.WriteLine("ADD" + card.ID + param + value);

        public void ADD(List<RuntimeCard> cards, string param, int value) => cards.ForEach(x => ADD(x, param, value));

        public void ADD(RuntimePlayer player, string param, int value) => Console.WriteLine("ADD" + player.ID + param + value);

        public void ADD(RuntimeCard       card, int ShielderNum, int ArcherNum, int riderNum, int AnyNum = 0) => Console.WriteLine("ADD" + card.ID + ShielderNum + ArcherNum + riderNum + AnyNum);
        public void ADD(List<RuntimeCard> card, int ShielderNum, int ArcherNum, int riderNum, int AnyNum = 0) => card?.ForEach(x => ADD(x, ShielderNum, ArcherNum, riderNum, AnyNum));

        public void ADD(RuntimeCard       card,  List<ESoldierType> soldiers) => Console.WriteLine("ADD" + card.ID + soldiers.Count);
        public void ADD(List<RuntimeCard> cards, List<ESoldierType> soldiers) => cards?.ForEach(x => ADD(x, soldiers));

        public void ADD(RuntimeCard       card,  int AnyNum) => Console.WriteLine("ADD" + card.ID + AnyNum);
        public void ADD(List<RuntimeCard> cards, int AnyNum) => cards?.ForEach(x => ADD(x, AnyNum));


        public void SHF(RuntimeCard card) => Console.WriteLine("SHF" + card.ID);

        public void SHF(List<RuntimeCard> cards) => cards?.ForEach(SHF);

        /// <summary>设置一个游戏中的数据</summary>
        public void SET_D(string param, int value) => Console.WriteLine("SET_D" + param + value);

        /// <summary>获取一个游戏中的数据</summary>
        public int? GET_D(string param) => param.Length;

        public void CLEAR(RuntimeCard card, int ability) => Console.WriteLine("CLEAR" + card.ID + ability);

        public void CHANGE(RuntimeCard card, int typeForm, int typeGoto)
        {
            Console.WriteLine("CHANGE" + card.ID + typeForm + typeGoto);
        }

        public void CHANGE(RuntimeCard card, int idGoto)
        {
            Console.WriteLine("CHANGE" + card.ID + idGoto);
        }


        /// <summary>指令:初始化发现面板</summary>
        public void DISCOVERY()
        {
            Select  = -1;
            Options = new List<int>();
            Options.Clear();
        }

        /// <summary>指令:向发现面板中添加一个选项</summary>
        public void OPTION(int option) => Options.Add(option);

        /// <summary>指令:向发现面板中添加多个选项</summary>
        public void OPTION(List<int> option) => Options.AddRange(option);



        /// <summary>指令:识别选中后的逻辑</summary>
        public void ONSELECT(string onSelect)
        {
            Console.WriteLine(onSelect);
        }

        /// <summary>指令:打开发现面板</summary>
        public void OPEN()
        {
            //选择Options的前三个
            Options = Options.Take(3).ToList();

            var entry = currentEntry;

            if (entry == null) return;

            Console.WriteLine("OPEN" + Options.Count);
        }

        public void Test()
        {
            // DISCOVERY();           
            // var cards = FLT(CardConfig,GET(c,TYPE)==2);
            // OPTION(PICK(cards,5)); 
            // ONSELECT("CHANGE(C1,Select);AAB(C1,20003001);");            
            //
            // OPEN();         
        }


        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ 对象封装 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//

        /// <summary> 初始化 </summary>
        /// <param name="duelMgr"> 绑定的决斗管理器 </param>
        /// <param name="scenesMgr"> 绑定的场景管理器 </param>
        public void Initialization(object manager)
        {
            // Data     = manager.DuelData       ?? throw new Exception($"初始化失败:{nameof(manager.DuelData)}为空");
            // Scenes   = manager.ScenesManager  ?? throw new Exception($"初始化失败,{nameof(manager.ScenesManager)}为空");
            // Areas    = manager.AreaManager    ?? throw new Exception($"初始化失败,{nameof(manager.AreaManager)}为空");
            // Players  = manager.PlayerManager  ?? throw new Exception($"初始化失败,{nameof(manager.PlayerManager)}为空");
            // Commands = manager.CommandManager ?? throw new Exception($"初始化失败,{nameof(manager.CommandManager)}为空");

            //CardConfig = GameConfig.CollectionCards.Values.ToList();

            Evaluator = new UniInk_Speed(); //创建一个UniInk实例

            Evaluator.RegisterVariable("Void",    InkValue.GetIntValue((int)EPos.Void));
            Evaluator.RegisterVariable("L",       InkValue.GetIntValue((int)EPos.Officer_L));
            Evaluator.RegisterVariable("R",       InkValue.GetIntValue((int)EPos.Officer_R));
            Evaluator.RegisterVariable("M",       InkValue.GetIntValue((int)EPos.Officer_M));
            Evaluator.RegisterVariable("Discard", InkValue.GetIntValue((int)EPos.Discard));
            Evaluator.RegisterVariable("Deck_O",  InkValue.GetIntValue((int)EPos.Deck_Officer));
            Evaluator.RegisterVariable("Deck_S",  InkValue.GetIntValue((int)EPos.Deck_Soldier));
            Evaluator.RegisterVariable("Hand",    InkValue.GetIntValue((int)EPos.Hand));

            Evaluator.RegisterVariable("GAME_START", InkValue.GetIntValue((int)EPhaseTypes.GameStart));
            Evaluator.RegisterVariable("GAME_END",   InkValue.GetIntValue((int)EPhaseTypes.GameEnd));
            Evaluator.RegisterVariable("START",      InkValue.GetIntValue((int)EPhaseTypes.TurnStart));
            Evaluator.RegisterVariable("MAIN",       InkValue.GetIntValue((int)EPhaseTypes.TurnMain));
            Evaluator.RegisterVariable("BATTLE",     InkValue.GetIntValue((int)EPhaseTypes.TurnBattle));
            Evaluator.RegisterVariable("END",        InkValue.GetIntValue((int)EPhaseTypes.TurnEnd));
            Evaluator.RegisterVariable("PREPARE",    InkValue.GetIntValue((int)EPhaseTypes.TurnPrepare));
            Evaluator.RegisterVariable("AI_MAIN",    InkValue.GetIntValue((int)EPhaseTypes.TurnAI));

            Evaluator.RegisterVariable("PHASE", InkValue.GetterValue(value =>
            {
                value.ValueType = TypeCode.Int32;
                value.Value_int = (int)PHASE;
            }));

            Evaluator.RegisterVariable("grower", InkValue.GetterValue(value =>
            {
                value.ValueType = TypeCode.Int32;
                value.Value_int = grower;
            }));


            Evaluator.RegisterVariable("V1", InkValue.GetString("GameParam.V1"));
            Evaluator.RegisterVariable("V2", InkValue.GetString("GameParam.V2"));
            Evaluator.RegisterVariable("V3", InkValue.GetString("GameParam.V3"));
            Evaluator.RegisterVariable("V4", InkValue.GetString("GameParam.V4"));
            Evaluator.RegisterVariable("V5", InkValue.GetString("GameParam.V5"));
            Evaluator.RegisterVariable("V6", InkValue.GetString("GameParam.V6"));
        }


        public const int Void    = (int)EPos.Void;
        public const int L       = (int)EPos.Officer_L;
        public const int R       = (int)EPos.Officer_R;
        public const int M       = (int)EPos.Officer_M;
        public const int Discard = (int)EPos.Discard;
        public const int Deck_O  = (int)EPos.Deck_Officer;
        public const int Deck_S  = (int)EPos.Deck_Soldier;

        public const int Hand = (int)EPos.Hand;

        public const EPhaseTypes GAME_START = EPhaseTypes.GameStart;
        public const EPhaseTypes GAME_END   = EPhaseTypes.GameEnd;
        public const EPhaseTypes START      = EPhaseTypes.TurnStart;
        public const EPhaseTypes MAIN       = EPhaseTypes.TurnMain;
        public const EPhaseTypes BATTLE     = EPhaseTypes.TurnBattle;
        public const EPhaseTypes END        = EPhaseTypes.TurnEnd;
        public const EPhaseTypes PREPARE    = EPhaseTypes.TurnPrepare;
        public const EPhaseTypes AI_MAIN    = EPhaseTypes.TurnAI;



        public EPhaseTypes PHASE => AI_MAIN;

        public int grower => inder++;

        public static int inder = 0;

        public const string V1 = "GameParam.V1";
        public const string V2 = "GameParam.V2";
        public const string V3 = "GameParam.V3";
        public const string V4 = "GameParam.V4";
        public const string V5 = "GameParam.V5";
        public const string V6 = "GameParam.V6";
        public const string V7 = "GameParam.V7";
        public const string V8 = "GameParam.V8";
        public const string V9 = "GameParam.V9";

        public const string ID                  = "GameParam.ID";
        public const string HP                  = "GameParam.HP";
        public const string DP                  = "GameParam.DP";
        public const string AP                  = "GameParam.AP";
        public const string ATK                 = "GameParam.ATK";
        public const string POS                 = "GameParam.Pos";
        public const string COST                = "GameParam.Cost";
        public const string SKILL               = "GameParam.SKILL";
        public const string EATK                = "GameParam.ExtraATK";
        public const string EARM                = "GameParam.ExtraArmor";
        public const string REST                = "GameParam.RestNum";
        public const string IArcher             = "GameParam.InitArcher";
        public const string IRider              = "GameParam.InitRider";
        public const string IShielder           = "GameParam.InitShielder";
        public const string GArcher             = "GameParam.GainArcher";
        public const string GRider              = "GameParam.GainRider";
        public const string GShielder           = "GameParam.GainShielder";
        public const string RestTurnNum_Officer = "GameParam.RestTurnNum_Officer";

        public const string S1Archer   = "GameParam.Skill_1_Archer";
        public const string S1Rider    = "GameParam.Skill_1_Rider";
        public const string S1Shielder = "GameParam.Skill_1_Shielder";
        public const string S1Any      = "GameParam.Skill_1_Any";

        public const string S2Archer   = "GameParam.Skill_2_Archer";
        public const string S2Rider    = "GameParam.Skill_2_Rider";
        public const string S2Shielder = "GameParam.Skill_2_Shielder";
        public const string S2Any      = "GameParam.Skill_2_Any";

        public const string EMPTY_PUNISH     = "GameParam.DECK_EMPTY_PUNISH";
        public const string TAKE_OFFICER_NUM = "GameParam.TAKE_OFFICER_NUM";


        public const string DAMAGE  = "GameParam.Damage";
        public const string INJURY  = "GameParam.Injury";
        public const string EARLY   = "GameParam.Priority";
        public const string COUNTRY = "GameParam.Country";
        public const string TYPE    = "GameParam.CardType";
        public const string RARITY  = "GameParam.Rarity";

        public const string Archer   = "GameParam.Archer";
        public const string Shielder = "GameParam.Shielder";
        public const string Rider    = "GameParam.Rider";

        public const string PPL = "GameParam.Popularity";


        public const string TURN_NUM = "GameParam.TurnNum";

        public RuntimePlayer P1 => EntryPlayer();
        public RuntimePlayer P2 => EntryOpponent();



        public int P1ID => P1.ID;
        public int P2ID => P2.ID;

        public RuntimeCard C1 => EntryCard();
        public RuntimeCard C2 => EntryTriggerCard();
        public RuntimeCard C3 => EntryTriggerCard2();
    }


    public enum EPos { Void = -1, Officer_L = 0, Officer_R = 1, Officer_M = 2, Discard = 3, Deck_Officer = 4, Deck_Soldier = 5, Hand = 6, }

    public enum EPhaseTypes { GameStart, GameEnd, TurnStart, TurnMain, TurnBattle, TurnEnd, TurnPrepare, TurnAI, }


    public class BaseClass
    {
        private static int inder;

        public int ID;

        protected BaseClass() => ID = inder++;
    }



    public class RuntimeCard : BaseClass { }

    public class RuntimePlayer : BaseClass { }

    public class CardData_Base : BaseClass { }
    public class CardData_Officer : CardData_Base { }

    public class CardData_Soldier : CardData_Base { }

    public class AbilityData_Base { }

    public class EffectEntry { }

    public class DuelObjectManager { }

    public class DuelAreaManager { }

    public class DuelPlayerManager { }

    public class DuelCommandManager { }

    public class DuelData { }


    public enum ESoldierType { }


    public static class GameConfig
    {
        public static ReadOnlyCollection<CardData_Officer> CollectionOfficerCards;

        public static ReadOnlyCollection<CardData_Soldier> CollectionSoldierCards;

        public static ReadOnlyDictionary<int, AbilityData_Base> CollectionAbilities;
    }

}