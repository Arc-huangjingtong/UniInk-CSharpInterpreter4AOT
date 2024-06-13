namespace Arc.UniInk.BestPractices
{

    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Arc.UniInk;


    public partial class DuelAction
    {
        /// <summary>所有的卡片配置数据</summary>
        public List<CardData_Base> CardConfig;

        /// <summary>所有的武将配置数据</summary>
        public ReadOnlyCollection<CardData_Officer> OfficerConfig => GameConfig.CollectionOfficerCards;

        /// <summary>所有的士兵配置数据</summary>
        public ReadOnlyCollection<CardData_Soldier> SoldierConfig => GameConfig.CollectionSoldierCards;

        /// <summary>所有的技能配置数据</summary>
        public ReadOnlyDictionary<int, AbilityData_Base> AbilityConfig => GameConfig.CollectionAbilities;

        /// <summary>当前结算的效果</summary>
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

        //   /// <summary>初始化</summary>
        // /// <param name="duelMgr">绑定的决斗管理器</param>
        // /// <param name="scenesMgr">绑定的场景管理器</param>
        // public void Initialization(DuelManager manager)
        // {
        //     GameConfig.ValidateInitialization();
        //
        //     Data     = manager.DuelData       ?? throw new Exception($"初始化失败:{nameof(manager.DuelData)}为空");
        //     Scenes   = manager.ScenesManager  ?? throw new Exception($"初始化失败,{nameof(manager.ScenesManager)}为空");
        //     Areas    = manager.AreaManager    ?? throw new Exception($"初始化失败,{nameof(manager.AreaManager)}为空");
        //     Players  = manager.PlayerManager  ?? throw new Exception($"初始化失败,{nameof(manager.PlayerManager)}为空");
        //     Commands = manager.CommandManager ?? throw new Exception($"初始化失败,{nameof(manager.CommandManager)}为空");
        //
        //     CardConfig = GameConfig.CollectionCards.Values.ToList();
        //
        //     Evaluator = CreateInk();
        // }
        //
        // public void Finalization()
        // {
        //     Data      = null;
        //     Scenes    = null;
        //     Evaluator = null;
        // }
        //
        // public UniInk CreateInk()
        // {
        //     var ans = new UniInk(this);
        //
        //     ans.OnEvaluateScript += (sender, args) =>
        //     {
        //         args.Expression = args.Expression.Trim() + ";";
        //         //Debug.Log($"执行脚本:{args.Expression}");
        //         args.Expression = args.Expression.Replace("；", ";") //中文分号转英文分号
        //                               .Replace(";;", ";")           //连续分号转换为单个分号
        //                               .Replace("（",  "(")           //中文括号转英文
        //                               .Replace("）",  ")");          //中文括号转英文
        //     };
        //
        //     ans.PreEvaluateFunction += (_, args) =>
        //     {
        //         if (args.Name != "FLT" || args.Args.Count <= 1) return;
        //
        //         var str = args.Args[1];
        //         args.Args[1] = $"c=>{str}";
        //     };
        //
        //     return ans;
        // }
        //
        //
        // /// <summary>当前结算的卡片</summary>
        // public RuntimeCard EntryCard() => currentEntry.skill.Owner as RuntimeCard;
        //
        // /// <summary>触发当前结算的卡片</summary>
        // public RuntimeCard EntryTriggerCard() => Scenes.GetCardWithInstanceID(currentEntry.triggerInstanceID);
        //
        // /// <summary>触发当前结算的卡片2(对撞被视为由两个对象同时触发)</summary>
        // public RuntimeCard EntryTriggerCard2() => Scenes.GetCardWithInstanceID(currentEntry.triggerInstanceID2);
        //
        // /// <summary>当前结算的玩家</summary>
        // public RuntimePlayer EntryPlayer() => Players.GetPlayer(currentEntry.playerID);
        //
        // /// <summary>当前结算的对手</summary>
        // public RuntimePlayer EntryOpponent() => Players.GetOpponent(currentEntry.playerID);
        //
        // /// <summary>当前结算技能的地块</summary>
        // public RuntimeGrid EntryGrid() => currentEntry.skill.Owner as RuntimeGrid;
        //
        //
        // //=============================================== 指令部分 ================================================//
        //
        // #region Command
        //
        //
        // /// <summary>指令:删除一张卡片</summary>
        // /// <param name="card">删除目标</param>
        // /// <remarks>删除:被删除的卡片会从游戏中彻底消失</remarks>
        // public void DEL(RuntimeCard card) => Commands.InsertCommand(DC_DeleteCard.Create(card));
        //
        // public void DEL(List<RuntimeCard> cards) => cards?.ForEach(DEL);
        //
        // /// <summary>指令:破坏一张卡片</summary>
        // /// <param name="card">破坏目标</param>
        // /// <remarks>破坏:被破坏的卡片会送入<b>消耗区</b></remarks>
        // public void DES(RuntimeCard card) => Commands.InsertCommand(DC_DestroyOfficerCard.Create(card));
        //
        // public void DES(List<RuntimeCard> cards) => cards.ForEach(DES);
        //
        // /// <summary>指令:给一个技能容器增加技能</summary>
        // /// <param name="holder">被增加技能的容器</param>
        // /// <param name="AbilityID">技能图鉴中的ID</param>
        // /// <param name="AutoDestory">是否自动销毁,默认开启,开启后这个技能会被一些定义好的规则删除</param>
        // /// <param name="level">技能层级,在一些特殊的层数技能中会有效果</param>
        // public void AAB(RuntimeCard holder, int abilityID, bool AutoDestroy = true) => AddAbility(currentEntry, holder, abilityID, AutoDestroy);
        //
        // public void AAB(RuntimePlayer player, int abilityID, bool AutoDestroy = true) => AddAbility(player, abilityID, AutoDestroy);
        //
        // public void AAB(List<RuntimeCard> holders, int abilityID, bool AutoDestroy = true) => holders?.ForEach(x => AAB(x, abilityID, AutoDestroy));
        //
        //
        // /// <summary>指令:给一个技能容器移除技能</summary>
        // /// <param name="holder">被移除技能的容器</param>
        // /// <param name="AbilityID">技能图鉴中的ID</param>
        // public void RAB(RuntimeCard holder, int abilityID) => Commands.InsertCommand(new DC_AbilityRemove(holder, abilityID));
        //
        // public void RAB(List<RuntimeCard> holders, int abilityID) => holders?.ForEach(x => RAB(x, abilityID));
        //
        // /// <summary>指令:判断对象是否有指定的技能</summary>
        // /// <param name="holder">被移除技能的容器</param>
        // /// <param name="AbilityID">技能图鉴中的ID</param>
        // public bool HAB(RuntimeCard holder, int abilityID) => holder?.AbilityContainer.Objects.Exists(x => x.AbilityID == abilityID) ?? false;
        //
        // public bool HAB(List<RuntimeCard> holders, int abilityID) => holders?.Exists(x => HAB(x, abilityID)) ?? false;
        //
        // /// <summary>指令:从卡片图鉴中生成一张卡到指定玩家的指定坐标</summary>
        // /// <param name="cardID">卡片的图鉴ID</param>
        // /// <param name="playerID">玩家对象</param>
        // /// <param name="pos">生成坐标</param>
        // /// <returns>生成的这张卡</returns>
        // public RuntimeCard CRE(int cardID, RuntimePlayer player, EPos pos) => CreateCard(cardID, player, pos);
        //
        // public RuntimeCard CRE(int cardID, RuntimePlayer player, int pos) => CreateCard(cardID, player, (EPos)pos);
        //
        //
        // /// <summary>指令:给一个[RuntimeCard]对象添加一个Buff</summary>
        // /// <param name="entry">效果结算对象(谁给予了这个BUF)</param>
        // /// <param name="value">Buff的增益/减益值</param>
        // /// <param name="param">参数名,可以自由扩展<see cref="GameParam"/></param>
        // /// <param name="autoDestroy">是否自动销毁的标记,默认为真(会按照一定的逻辑用指定方式一起销毁)</param>
        // public void BUF(RuntimeCard card, string param, int value, bool autoDestroy = true) => Commands.InsertCommand(DC_CreatCardBuff.Create(currentEntry, card, param, value, autoDestroy));
        //
        // public void BUF(RuntimeCard card, string param, int value, int type = 1) => Commands.InsertCommand(DC_CreatCardBuff.Create(currentEntry, card, param, value, type));
        //
        //
        //
        // public void BUF(List<RuntimeCard> cards, string param, int value, bool autoDestroy = true) => cards.ForEach(x => BUF(x, param, value, autoDestroy));
        //
        //
        // /// <summary>指令:设置一个对象的参数值</summary>
        // /// <param name="value">设置的值</param>
        // /// <param name="param">参数名,可以自由扩展</param>
        // /// <param name="runtimeObject">对象:可以是游戏中的任何对象，卡牌，地形，玩家，等等</param>
        // public void SET(RuntimeCard card, string param, int? value) => DCC_SetCardParam(card, param, value);
        //
        // public void SET(List<RuntimeCard> card, string param, int? value) => card.ForEach(x => DCC_SetCardParam(x, param, value));
        //
        // public void SET(RuntimePlayer player, string param, int? value) => DCC_SetPlayerParam(player, param, value);
        //
        //
        // /// <summary>指令:效果的持有者对指定卡片造成伤害</summary>
        // /// <param name="card">伤害承受者</param>
        // /// <param name="damage">伤害值 </param>
        // public void DMG(RuntimeCard card, int damage, int soldierType = 4) => Commands.InsertCommand(DC_TakeOfficerDamage.Create(currentEntry.skill.Owner, card, damage, EDamageType.SKILL, (ESoldierType)soldierType));
        //
        //
        // public void DMG(List<RuntimeCard> cards, int damage, int soldierType = 4) => cards?.ForEach(x => DMG(x, damage, soldierType));
        //
        //
        // /// <summary>指令:效果的持有者对指定玩家造成伤害</summary>
        // /// <param name="player">伤害承受者</param>
        // /// <param name="damage">伤害值   </param>
        // public void DMG_P(RuntimePlayer player, int damage) => Commands.InsertCommand(DC_TakeMoraleDamage.Create(EntryCard(), player, damage, EDamageType.SKILL));
        //
        //
        // public void LOSS(RuntimeCard card, int damage, int soldierType = 4) => Commands.InsertCommand(DC_TakeOfficerDamage.Create(currentEntry.skill.Owner, card, damage, EDamageType.LOSS, (ESoldierType)soldierType));
        //
        // public void LOSS(List<RuntimeCard> cards, int damage, int soldierType = 4) => cards?.ForEach(x => DMG(x, damage, soldierType));
        //
        //
        //
        // ///获取一个对象的属性值
        // public int GET(RuntimeCard card, string param) => GetCardParam(card, param);
        //
        // public int GET(RuntimeCard   card,   int    param) => GetCardParam(card, param);
        // public int GET(CardData_Base card,   string param) => GetCardConfigData(card, param);
        // public int GET(RuntimePlayer player, string param) => GetPlayerParam(player, param);
        //
        // /// <summary>指令:增加一个对象的参数值</summary>
        // /// <param name="value">设置的值</param>
        // /// <param name="param">参数名,可以自由扩展</param>
        // /// <param name="card">对象:可以是游戏中的任何对象，卡牌，地形，玩家，等等</param>
        // public void ADD(RuntimeCard card, string param, int value) => DCC_AddCardParam(card, param, value);
        //
        // public void ADD(List<RuntimeCard> cards, string param, int value) => cards?.ForEach(card => DCC_AddCardParam(card, param, value));
        //
        // public void ADD(RuntimePlayer player, string param, int value) => DCC_AddPlayerParam(player, param, value);
        //
        // public void ADD(RuntimeCard       card, int ShielderNum, int ArcherNum, int riderNum, int AnyNum = 0) => Commands.InsertCommand(DC_AddSoldier.Create(card, ShielderNum, ArcherNum, riderNum, AnyNum));
        // public void ADD(List<RuntimeCard> card, int ShielderNum, int ArcherNum, int riderNum, int AnyNum = 0) => card?.ForEach(x => ADD(x, ShielderNum, ArcherNum, riderNum, AnyNum));
        //
        // public void ADD(RuntimeCard       card,  List<ESoldierType> soldiers) => Commands.InsertCommand(DC_AddSoldier.Create(card, soldiers));
        // public void ADD(List<RuntimeCard> cards, List<ESoldierType> soldiers) => cards?.ForEach(x => ADD(x, soldiers));
        //
        // public void ADD(RuntimeCard       card,  int AnyNum) => Commands.InsertCommand(DC_AddSoldier.Create(card, 0, 0, 0, AnyNum));
        // public void ADD(List<RuntimeCard> cards, int AnyNum) => cards?.ForEach(x => ADD(x, AnyNum));
        //
        //
        // public void SHF(RuntimeCard       card)  => Commands.InsertCommand(DC_ShuffleSoldier.Create(card));
        // public void SHF(List<RuntimeCard> cards) => cards?.ForEach(SHF);
        //
        //
        //
        // /// <summary>设置一个游戏中的数据</summary>
        // public void SET_D(string param, int value) => DCC_SetDataParam(Data, param, value);
        //
        // /// <summary>获取一个游戏中的数据</summary>
        // public int? GET_D(string param) => GetDataParam(Data, param);
        //
        // public void CLEAR(RuntimeCard card, int ability) => ClearBuff(card, ability);
        //
        // public void CHANGE(RuntimeCard card, int typeForm, int typeGoto)
        // {
        //     Commands.InsertCommand(DC_ChangeSoldiers.Create(card, (ESoldierType)typeForm, (ESoldierType)typeGoto));
        //     Commands.InsertCommand(new DC_ActiveAbility(card));
        // }
        //
        // public void CHANGE(RuntimeCard card, int idGoto)
        // {
        //     Debug.Log("====CHANGE====" + card + "====" + idGoto);
        //     Commands.InsertCommand(DC_CardChanging.Create(card, idGoto));
        // }
        //
        //
        // /// <summary>指令:初始化发现面板</summary>
        // public void DISCOVERY()
        // {
        //     Select  =   -1;
        //     Options ??= new List<int>();
        //     Options.Clear();
        //
        //     DuelManager.Instance.EffectManager.PauseStack();
        //     DuelManager.Instance.CommandManager.StopCommand();
        // }
        //
        // /// <summary>指令:向发现面板中添加一个选项</summary>
        // public void OPTION(int option) => Options.Add(option);
        //
        // /// <summary>指令:向发现面板中添加多个选项</summary>
        // public void OPTION(List<int> option) => Options.AddRange(option);
        //
        //
        //
        // /// <summary>指令:识别选中后的逻辑</summary>
        // public void ONSELECT(string onSelect)
        // {
        //     Debug.Log(onSelect[1..]);
        //     OnSelect = onSelect[1..];
        // }
        //
        // /// <summary>指令:打开发现面板</summary>
        // public void OPEN()
        // {
        //     //选择Options的前三个
        //     Options = Options.Shuffle().Take(3).ToList();
        //
        //     var entry = currentEntry;
        //
        //     var data = new CardOptionPanelItem(Options, 1, delegate(List<int> ints)
        //     {
        //         //给选中的对象赋值
        //         Select = ints.FirstOrDefault();
        //
        //         Debug.Log("====" + Select);
        //
        //         var entryCache = currentEntry;
        //
        //         currentEntry = entry;
        //
        //         //执行选中后的逻辑
        //         Evaluator.ScriptEvaluate(OnSelect);
        //
        //         currentEntry = entryCache;
        //
        //         //关闭选择面板
        //         Utils_PanelSystem.ClosePanel<UICardSelectionPanel>();
        //
        //         DuelManager.Instance.EffectManager.RestartStack();
        //         DuelManager.Instance.CommandManager.ContinueCommand();
        //     }, "你的选择是....", "确定");
        //
        //     UICardSelectionPanel.OpenCardSelectionPanel("Panel_CardSelection", data);
        // }
        //
        // public void Test()
        // {
        //     // DISCOVERY();           
        //     // var cards = FLT(CardConfig,GET(c,TYPE)==2);
        //     // OPTION(PICK(cards,5)); 
        //     // ONSELECT("CHANGE(C1,Select);AAB(C1,20003001);");            
        //     //
        //     // OPEN();         
        // }
        //
        //
        // #endregion
        //
        //
        // //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ 对象封装 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
        //
        //
        // public const int Void    = (int)EPos.Void;
        // public const int L       = (int)EPos.Officer_L;
        // public const int R       = (int)EPos.Officer_R;
        // public const int M       = (int)EPos.Officer_M;
        // public const int Discard = (int)EPos.Discard;
        // public const int Deck_O  = (int)EPos.Deck_Officer;
        // public const int Deck_S  = (int)EPos.Deck_Soldier;
        // public const int Hand    = (int)EPos.Hand;
        //
        // public const EPhaseTypes GAME_START = EPhaseTypes.GameStart;
        // public const EPhaseTypes GAME_END   = EPhaseTypes.GameEnd;
        // public const EPhaseTypes START      = EPhaseTypes.TurnStart;
        // public const EPhaseTypes MAIN       = EPhaseTypes.TurnMain;
        // public const EPhaseTypes BATTLE     = EPhaseTypes.TurnBattle;
        // public const EPhaseTypes END        = EPhaseTypes.TurnEnd;
        // public const EPhaseTypes PREPARE    = EPhaseTypes.TurnPrepare;
        // public const EPhaseTypes AI_MAIN    = EPhaseTypes.TurnAI;
        //
        // public EPhaseTypes PHASE => DuelManager.Instance.PhaseManager.CurrentPhase;
        //
        // public const string V1 = GameParam.V1;
        // public const string V2 = GameParam.V2;
        // public const string V3 = GameParam.V3;
        // public const string V4 = GameParam.V4;
        // public const string V5 = GameParam.V5;
        // public const string V6 = GameParam.V6;
        // public const string V7 = GameParam.V7;
        // public const string V8 = GameParam.V8;
        // public const string V9 = GameParam.V9;
        //
        // public const string ID                  = GameParam.ID;
        // public const string HP                  = GameParam.HP;
        // public const string DP                  = GameParam.DP;
        // public const string AP                  = GameParam.AP;
        // public const string ATK                 = GameParam.ATK;
        // public const string POS                 = GameParam.Pos;
        // public const string COST                = GameParam.Cost;
        // public const string SKILL               = GameParam.SKILL;
        // public const string EATK                = GameParam.ExtraATK;
        // public const string EARM                = GameParam.ExtraArmor;
        // public const string REST                = GameParam.RestNum;
        // public const string IArcher             = GameParam.InitArcher;
        // public const string IRider              = GameParam.InitRider;
        // public const string IShielder           = GameParam.InitShielder;
        // public const string GArcher             = GameParam.GainArcher;
        // public const string GRider              = GameParam.GainRider;
        // public const string GShielder           = GameParam.GainShielder;
        // public const string RestTurnNum_Officer = GameParam.RestTurnNum_Officer;
        //
        // public const string S1Archer   = GameParam.Skill_1_Archer;
        // public const string S1Rider    = GameParam.Skill_1_Rider;
        // public const string S1Shielder = GameParam.Skill_1_Shielder;
        // public const string S1Any      = GameParam.Skill_1_Any;
        //
        // public const string S2Archer   = GameParam.Skill_2_Archer;
        // public const string S2Rider    = GameParam.Skill_2_Rider;
        // public const string S2Shielder = GameParam.Skill_2_Shielder;
        // public const string S2Any      = GameParam.Skill_2_Any;
        //
        // public const string EMPTY_PUNISH     = GameParam.DECK_EMPTY_PUNISH;
        // public const string TAKE_OFFICER_NUM = GameParam.TAKE_OFFICER_NUM;
        //
        //
        // public const string DAMAGE  = GameParam.Damage;
        // public const string INJURY  = GameParam.Injury;
        // public const string EARLY   = GameParam.Priority;
        // public const string COUNTRY = GameParam.Country;
        // public const string TYPE    = GameParam.CardType;
        // public const string RARITY  = GameParam.Rarity;
        //
        // public const string Archer   = GameParam.Archer;
        // public const string Shielder = GameParam.Shielder;
        // public const string Rider    = GameParam.Rider;
        //
        // public const string PPL = GameParam.Popularity;
        //
        //
        // public const string TURN_NUM = GameParam.TurnNum;
        //
        //
        // public RuntimePlayer P1 => EntryPlayer();
        // public RuntimePlayer P2 => EntryOpponent();
        //
        // public int DELTA => currentEntry.arg1;
        //
        // public int P1ID => P1.IdentityContainer.TypeID;
        // public int P2ID => P2.IdentityContainer.TypeID;
        //
        // public RuntimeCard C1 => EntryCard();
        // public RuntimeCard C2 => EntryTriggerCard();
        // public RuntimeCard C3 => EntryTriggerCard2();
        // public RuntimeGrid G1 => EntryGrid();
        //
        //
        // public RuntimeAbility SKILL1 => GetAbility(C1, 0);
        // public RuntimeAbility SKILL2 => GetAbility(C1, 1);
        //
        // public RuntimeGrid GL1 => GRID(P1, L);
        // public RuntimeGrid GL2 => GRID(P2, L);
        // public RuntimeGrid GR1 => GRID(P1, R);
        // public RuntimeGrid GR2 => GRID(P2, R);
        // public RuntimeGrid GM1 => GRID(P1, M);
        // public RuntimeGrid GM2 => GRID(P2, M);
        //
        // public RuntimeCard L1 => CARD(P1, EPos.Officer_L).FirstOrDefault();
        // public RuntimeCard L2 => CARD(P2, EPos.Officer_L).FirstOrDefault();
        // public RuntimeCard R1 => CARD(P1, EPos.Officer_R).FirstOrDefault();
        // public RuntimeCard R2 => CARD(P2, EPos.Officer_R).FirstOrDefault();
        // public RuntimeCard M1 => CARD(P1, EPos.Officer_M).FirstOrDefault();
        // public RuntimeCard M2 => CARD(P2, EPos.Officer_M).FirstOrDefault();
        //
        // public List<RuntimeCard> ALL1
        // {
        //     get
        //     {
        //         var ans = new List<RuntimeCard>();
        //         if (L1 != null) ans.Add(L1);
        //         if (R1 != null) ans.Add(R1);
        //         if (M1 != null) ans.Add(M1);
        //         return ans;
        //     }
        // }
        //
        // public List<RuntimeCard> ALL2
        // {
        //     get
        //     {
        //         var ans = new List<RuntimeCard>();
        //         if (L2 != null) ans.Add(L2);
        //         if (R2 != null) ans.Add(R2);
        //         if (M2 != null) ans.Add(M2);
        //         return ans;
        //     }
        // }
        //
        // public List<RuntimeCard> ALL
        // {
        //     get
        //     {
        //         var ans = new List<RuntimeCard>();
        //         ans.AddRange(ALL1);
        //         ans.AddRange(ALL2);
        //
        //         return ans;
        //     }
        // }
        //
        // public List<RuntimeCard> OTHER
        // {
        //     get
        //     {
        //         var ans = new List<RuntimeCard>();
        //         if (L1 != null) ans.Add(L1);
        //         if (R1 != null) ans.Add(R1);
        //         if (M1 != null) ans.Add(M1);
        //
        //         ans.Remove(C1);
        //         return ans;
        //     }
        // }
        //
        // public int ALLArcher1   => ALL1.Sum(x => x[Archer].EffectiveValue);
        // public int ALLArcher2   => ALL2.Sum(x => x[Archer].EffectiveValue);
        // public int ALLShielder1 => ALL1.Sum(x => x[Shielder].EffectiveValue);
        // public int ALLShielder2 => ALL2.Sum(x => x[Shielder].EffectiveValue);
        // public int ALLRider1    => ALL1.Sum(x => x[Rider].EffectiveValue);
        // public int ALLRider2    => ALL2.Sum(x => x[Rider].EffectiveValue);
        //
        // public int ARG1 => currentEntry.arg1;
        // public int ARG2 => DuelExtensions.DecompressDamageNum(ARG1).riderNum;
        // public int ARG3 => DuelExtensions.DecompressDamageNum(ARG1).archerNum;
        // public int ARG4 => DuelExtensions.DecompressDamageNum(ARG1).shielderNum;
        //
        //
        //
        // ///打印日志
        // public void LOG(object msg) => Debug.Log(msg);
        //
        // ///按照位置获取一个卡片对象
        // public List<RuntimeCard> CARD(RuntimePlayer player, EPos pos) => Areas.GetCards(player.IdentityContainer.TypeID, pos);
        //
        // public List<RuntimeCard> CARD(RuntimePlayer player, int pos) => Areas.GetCards(player.IdentityContainer.TypeID, (EPos)pos);
        // public List<RuntimeCard> DECK(RuntimePlayer player) => CARD(player, EPos.Deck_Soldier);
        //
        //
        // ///按照位置获取一个地块对象
        // public RuntimeGrid GRID(RuntimePlayer player, EPos pos) => Areas.GetGrid(player.IdentityContainer.TypeID, pos);
        //
        // public RuntimeGrid GRID(RuntimePlayer player, int pos) => GRID(player, (EPos)pos);
        //
        //
        // ///获取一个正对面区域的对象
        // public RuntimeCard OPP(RuntimeCard card) => Areas.GetOppositeCard(card);
        //
        // public RuntimeCard NEAR(RuntimeCard card) => Areas.GetNearCard(card);
        //
        //
        // public List<RuntimeCard> FLT(List<RuntimeCard> cards, Predicate<RuntimeCard> filter)
        // {
        //     var ans = cards.FindAll(filter);
        //     //     DuelLog.Log("过滤器函数筛选出了:" + ans.Count);
        //     return ans;
        // }
        //
        // public List<int> FLT(List<CardData_Base> cards, Predicate<CardData_Base> filter)
        // {
        //     var ans = cards.FindAll(filter);
        //     return ans.Select(x => x.CardID).ToList();
        // }
        //
        // ///从一个卡片集合里面随机抽取一个对象
        // public RuntimeCard PICK(List<RuntimeCard> cards) => cards.PickRandom();
        //
        // ///从一个ID集合里面随机抽取一个对象
        // public int PICK(List<int> cardIDs) => cardIDs.PickRandom();
        //
        //
        // public List<RuntimeCard> PICK(List<RuntimeCard> cards,   int num) => cards.PickRandom(num);
        // public List<int>         PICK(List<int>         cardIDs, int num) => cardIDs.PickRandom(num);
        //
        //
        // public int NUM(List<RuntimeCard> cards)
        // {
        //     var num = cards?.Count ?? 0;
        //     Debug.Log("NUM:" + num);
        //
        //     return num;
        // }
        //
        //
        // public void AddCard(int ID, int num)
        // {
        //     for (int i = 0 ; i < num ; i++)
        //     {
        //         CRE(ID, P1, EPos.Deck_Soldier);
        //     }
        // }
        //
        // public int CountryNum
        // {
        //     get
        //     {
        //         var allCards   = ALL1;
        //         var allCountry = allCards.Select(card => card[Country].EffectiveValue).Distinct().Count();
        //         Debug.Log($"当前场上一共有{allCountry}个国家");
        //         return allCountry;
        //     }
        // }
        //
        // public int DeckNum => DECK(P1).Count;
        //
        // public int SoldierTypeNum(RuntimeCard card)
        // {
        //     if (card.Type == ECardType.Soldier) return 0;
        //
        //     var rider   = card[GameParam.Rider].EffectiveValue    > 0 ? 1 : 0;
        //     var archer  = card[GameParam.Archer].EffectiveValue   > 0 ? 1 : 0;
        //     var shelter = card[GameParam.Shielder].EffectiveValue > 0 ? 1 : 0;
        //
        //     return rider + archer + shelter;
        // }
        //
        //
        // //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ 私有方法 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
        //
        //
        // /// <summary>设置卡片对象参数</summary> 
        // private void DCC_SetCardParam(RuntimeCard card, string param, int? value)
        // {
        //     if (card == null || string.IsNullOrWhiteSpace(param)) return;
        //     if (value == null) return;
        //
        //     if (param == GameParam.Pos) //如果是位置,则设置位置
        //     {
        //         Commands.InsertCommand(new DC_MoveCard(card, (EPos)value));
        //     }
        //     else if (card.ParamContainer.Params.ContainsKey(param)) //如果输入的是一个属性,则设置属性值
        //     {
        //         Commands.InsertCommand(new DC_SetCardParam(card, param, (int)value));
        //         if (param is GameParam.Archer or GameParam.Shielder or GameParam.Rider)
        //         {
        //             Commands.InsertCommand(new DC_TimePoint(ETimePointTypes.TimePoint_TroopChange, card));
        //         }
        //     }
        //     else //否则设置一个标记值
        //     {
        //         Commands.InsertCommand(new DC_SetMark(card, param, (int)value));
        //     }
        // }
        //
        // /// <summary>设置玩家对象参数值</summary>
        // private void DCC_SetPlayerParam(RuntimePlayer player, string param, int? value)
        // {
        //     if (player == null || string.IsNullOrWhiteSpace(param)) return;
        //     if (value == null) return;
        //
        //     // 如果输入的是一个属性,则设置属性值
        //     if (player.ParamContainer.Params.ContainsKey(param))
        //     {
        //         Commands.InsertCommand(new DC_SetPlayerParam(player, param, (int)value));
        //     }
        //     // 否则设置一个标记值
        //     else
        //     {
        //         Commands.InsertCommand(new DC_SetMark(player, param, (int)value));
        //     }
        // }
        //
        // /// <summary>设置数据参数值</summary>
        // private void DCC_SetDataParam(DuelData data, string param, int? value)
        // {
        //     if (data == null || string.IsNullOrWhiteSpace(param)) return;
        //     if (value == null) return;
        //
        //     // 如果输入的是一个属性,则设置属性值
        //     if (data.ParamContainer.Params.ContainsKey(param))
        //     {
        //         Commands.InsertCommand(new DC_SetDuelDataParam(data, param, (int)value));
        //     }
        // }
        //
        // /// <summary>增加卡片参数值</summary>
        // private void DCC_AddCardParam(RuntimeCard card, string param, int value)
        // {
        //     if (card == null || string.IsNullOrWhiteSpace(param)) return;
        //
        //     if (card.ParamContainer.Params.ContainsKey(param))
        //     {
        //         value += GetCardParam(card, param);
        //         Commands.InsertCommand(new DC_SetCardParam(card, param, value));
        //     }
        //     else
        //     {
        //         Commands.InsertCommand(new DC_AddMark(card, param, value));
        //     }
        // }
        //
        //
        // /// <summary>增加玩家参数值</summary>
        // private void DCC_AddPlayerParam(RuntimePlayer player, string param, int value)
        // {
        //     if (player == null || string.IsNullOrWhiteSpace(param))
        //     {
        //         return;
        //     }
        //
        //     if (player.ParamContainer.Params.ContainsKey(param))
        //     {
        //         value += GetPlayerParam(player, param);
        //         Commands.InsertCommand(new DC_SetPlayerParam(player, param, value));
        //     }
        //     else
        //     {
        //         Commands.InsertCommand(new DC_AddMark(player, param, value));
        //     }
        // }
        //
        //
        // public int GetCardConfigData(CardData_Base card, string key)
        // {
        //     switch (key)
        //     {
        //         case ID :      return card.CardID;
        //         case TYPE :    return (int)card.CardType;
        //         case COUNTRY : return (int)card.CardCountry;
        //         case RARITY :  return card.Rarity;
        //         case ATK :     return card is CardData_Officer officer ? officer.CardATK : 0;
        //     }
        //
        //     return default;
        // }
        //
        //
        // /// <summary>获取卡片对象参数</summary>
        // private int GetCardParam(RuntimeCard card, string param)
        // {
        //     if (card == null) return 0;
        //     if (string.IsNullOrWhiteSpace(param)) return 0;
        //
        //
        //     if (card.ParamContainer.Params.TryGetValue(param, out var paramValue))
        //     {
        //         Debug.Log(paramValue);
        //
        //         return paramValue.EffectiveValue;
        //     }
        //     else
        //     {
        //         return card.DuelMarkContainer.GetMark(param)?.MarkValue ?? 0;
        //     }
        // }
        //
        // /// <summary>获取卡片是否有对应的技ID</summary>
        // private static int GetCardParam(RuntimeCard card, int abilityID)
        // {
        //     if (card == null) return 0;
        //
        //     var result = card.AbilityContainer.Objects.Exists(x => x.AbilityID == abilityID);
        //
        //     return result ? 1 : 0;
        // }
        //
        //
        //
        // /// <summary>获取玩家对象参数</summary>
        // private int GetPlayerParam(RuntimePlayer player, string param)
        // {
        //     if (player == null) return 0;
        //     if (string.IsNullOrWhiteSpace(param)) return 0;
        //
        //     if (player.ParamContainer.Params.TryGetValue(param, out var paramValue))
        //     {
        //         return paramValue.EffectiveValue;
        //     }
        //     else
        //     {
        //         return player.DuelMarkContainer.GetMark(param)?.MarkValue ?? 0;
        //     }
        // }
        //
        // /// <summary>获取数据参数</summary>
        // private int? GetDataParam(DuelData data, string param)
        // {
        //     if (data == null) return null;
        //     if (string.IsNullOrWhiteSpace(param)) return null;
        //
        //     if (data.ParamContainer.Params.TryGetValue(param, out var runtimeParam))
        //     {
        //         return runtimeParam.EffectiveValue;
        //     }
        //
        //     return null;
        // }
        //
        // private RuntimeAbility GetAbility(RuntimeCard card, int abilityIndex)
        // {
        //     if (card == null) return null;
        //
        //     if (card.AbilityContainer.Objects.Count < abilityIndex + 1) return null;
        //
        //     return card.AbilityContainer.Objects[abilityIndex];
        // }
        //
        // private void ClearBuff(RuntimeCard card, int abilityIndex)
        // {
        //     if (card == null) return;
        //     var ability = GetAbility(card, abilityIndex - 1);
        //
        //     // Scenes.RemoveBuff(card, ability);
        // }
        //
        // private RuntimeCard CreateCard(int cardID, RuntimePlayer player, EPos pos)
        // {
        //     var command = Scenes.DCC_CreateCard(cardID, player.IdentityContainer.OwnerID, pos, out var card);
        //
        //     Commands.InsertCommand(command);
        //
        //     return card;
        // }
        //
        //
        // /// <summary>指定概率生成一个bool值</summary>
        // /// <param name="chance">比较区间[0-100]</param>
        // /// <returns>返回随机出来的bool值</returns>
        // public bool GetChance(int chance)
        // {
        //     return UnityEngine.Random.Range(0, 100) < chance;
        // }
        //
        // public bool CHANCE(int chance) => GetChance(chance);
        //
        //
        // /// <summary>被脚本层的AAB函数调用,用于各种情况下(包括光环)增加一个技能</summary>
        // public void AddAbility(EffectEntry curEntry, RuntimeCard card, int abilityID, bool autoDestroy = true)
        // {
        //     GameConfig.ValidateAbilityID(abilityID);
        //
        //     if (card == null) return;
        //
        //     //即触发这个增加技能的对象+触发技能的这个对象的技能AbilityID组合成的标记,能确保唯一性;
        //     var Tag = $"{curEntry.skill.Owner.IdentityContainer.InstanceID}_{curEntry.skill.AbilityID}";
        //
        //     Commands.InsertCommand(new DC_AbilityAdd(card, Tag, abilityID, autoDestroy));
        // }
        //
        // public void AddAbility(RuntimePlayer player, int abilityID, bool autoDestroy = true)
        // {
        //     GameConfig.ValidateAbilityID(abilityID);
        //
        //     if (player == null) return;
        //
        //
        //     Commands.InsertCommand(DC_AbilityRemove_Player.Create(player, abilityID));
        //
        //     var Tag = $"{player.IdentityContainer.InstanceID}_{abilityID}";
        //
        //     if (player.AbilityContainer.Objects.Exists(x => x.AbilityID == abilityID || x.DuelMarkContainer.GetMark(Tag) != null))
        //     {
        //         return;
        //     }
        //
        //     Commands.InsertCommand(new DC_AbilityAdd_Player(player, C1, abilityID, autoDestroy));
        // }
        //
        //
        // private readonly Regex Regex_OnSelect = new(@"ONSELECT\(\);(.*?)OPEN\(\);", RegexOptions.Singleline | RegexOptions.Compiled);
    }


    public class RuntimeCard { }

    public class CardData_Base { }
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