namespace Arc.UniInk.BestPractices
{

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Arc.UniInk;


    /// <summary> A simple class to demonstrate how to use UniInk in a game. </summary>
    public partial class ScriptAction
    {
        /// <summary> Initializes a new instance of the <see cref="ScriptAction"/> class. </summary>
        public ScriptAction()
        {
            //1. Create a new instance of the UniInk_Speed class.
            Evaluator = new UniInk();

            //2. A simple way to register Enum values.
            //or you can use 
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

            Evaluator.RegisterVariable("PHASE", InkValue.SetGetter(InkValue.GetIntValue((int)PHASE), value =>
            {
                value.Value_int = (int)PHASE;
            }));

            Evaluator.RegisterVariable("grower", InkValue.SetGetter(InkValue.GetIntValue((int)PHASE),value =>
            {
                value.Value_int = grower;
            }));
        }

        public static UniInk Evaluator;

        public List<CardData_Base> CardConfig;

        public ReadOnlyCollection<CardData_Officer>      OfficerConfig => GameConfig.CollectionOfficerCards;
        public ReadOnlyCollection<CardData_Soldier>      SoldierConfig => GameConfig.CollectionSoldierCards;
        public ReadOnlyDictionary<int, AbilityData_Base> AbilityConfig => GameConfig.CollectionAbilities;


        public EffectEntry currentEntry { get; set; }

        public RuntimeCard Target { get; set; }


        public List<ESoldierType> Soldiers { get; set; }

        public List<int> Options;


        public int Select;

        public string OnSelect;



        private DuelData Data { get; set; }



        public void DEL(RuntimeCard       card)  => Console.WriteLine("DEL" + card.ID);
        public void DEL(List<RuntimeCard> cards) => cards?.ForEach(DEL);

        public void DES(RuntimeCard card) => Console.WriteLine("DES" + card.ID);

        public void DES(List<RuntimeCard> cards) => cards.ForEach(DES);

        public void AAB(RuntimeCard holder, int abilityID, bool AutoDestroy = true) => Console.WriteLine("AAB" + holder.ID + abilityID + AutoDestroy);

        public void AAB(RuntimePlayer player, int abilityID, bool AutoDestroy = true) => Console.WriteLine("AAB" + player.ID + abilityID + AutoDestroy);

        public void AAB(List<RuntimeCard> holders, int abilityID, bool AutoDestroy = true) => holders?.ForEach(x => AAB(x, abilityID, AutoDestroy));


        public void RAB(RuntimeCard holder, int abilityID) => Console.WriteLine("RAB" + holder.ID + abilityID);

        public void RAB(List<RuntimeCard> holders, int abilityID) => holders?.ForEach(x => RAB(x, abilityID));

        public bool HAB(RuntimeCard holder, int abilityID) => holder.ID == abilityID;

        public bool HAB(List<RuntimeCard> holders, int abilityID) => holders?.Exists(x => HAB(x, abilityID)) ?? false;

        public RuntimeCard CRE(int cardID, RuntimePlayer player, EPos pos) => new RuntimeCard();

        public RuntimeCard CRE(int cardID, RuntimePlayer player, int pos) => CRE(cardID, player, (EPos)pos);

        public void BUF(RuntimeCard       card,  string param, int value, bool autoDestroy = true) => Console.WriteLine("BUF" + card.ID + param + value + autoDestroy);
        public void BUF(RuntimeCard       card,  string param, int value, int  type        = 1)    => Console.WriteLine("BUF" + card.ID + param + value + type);
        public void BUF(List<RuntimeCard> cards, string param, int value, bool autoDestroy = true) => cards.ForEach(x => BUF(x, param, value, autoDestroy));


        public void SET(RuntimeCard       card,   string param, int? value) => Console.WriteLine("SET" + card.ID + param + value);
        public void SET(List<RuntimeCard> card,   string param, int? value) => card.ForEach(x => SET(x, param, value));
        public void SET(RuntimePlayer     player, string param, int? value) => Console.WriteLine("SET" + player.ID + param + value);


        public void DMG(RuntimeCard       card,  int damage, int soldierType = 4) => Console.WriteLine("DMG" + card.ID + damage + soldierType);
        public void DMG(List<RuntimeCard> cards, int damage, int soldierType = 4) => cards?.ForEach(x => DMG(x, damage, soldierType));


        public void DMG_P(RuntimePlayer player, int damage) => Console.WriteLine("DMG_P" + player.ID + damage);


        public void LOSS(RuntimeCard card, int damage, int soldierType = 4) => DMG(card, damage, soldierType);

        public void LOSS(List<RuntimeCard> cards, int damage, int soldierType = 4) => cards?.ForEach(x => DMG(x, damage, soldierType));



        public int GET(RuntimeCard   card,   string param) => card.ID   + param.Length;
        public int GET(RuntimeCard   card,   int    param) => card.ID   + param;
        public int GET(CardData_Base card,   string param) => card.ID   + param.Length;
        public int GET(RuntimePlayer player, string param) => player.ID + param.Length;


        public void ADD(RuntimeCard       card,   string param, int value) => Console.WriteLine("ADD" + card.ID + param + value);
        public void ADD(List<RuntimeCard> cards,  string param, int value) => cards.ForEach(x => ADD(x, param, value));
        public void ADD(RuntimePlayer     player, string param, int value) => Console.WriteLine("ADD" + player.ID + param + value);

        public void ADD(RuntimeCard       card, int ShielderNum, int ArcherNum, int riderNum, int AnyNum = 0) => Console.WriteLine("ADD" + card.ID + ShielderNum + ArcherNum + riderNum + AnyNum);
        public void ADD(List<RuntimeCard> card, int ShielderNum, int ArcherNum, int riderNum, int AnyNum = 0) => card?.ForEach(x => ADD(x, ShielderNum, ArcherNum, riderNum, AnyNum));

        public void ADD(RuntimeCard       card,  List<ESoldierType> soldiers) => Console.WriteLine("ADD" + card.ID + soldiers.Count);
        public void ADD(List<RuntimeCard> cards, List<ESoldierType> soldiers) => cards?.ForEach(x => ADD(x, soldiers));

        public void ADD(RuntimeCard       card,  int AnyNum) => Console.WriteLine("ADD" + card.ID + AnyNum);
        public void ADD(List<RuntimeCard> cards, int AnyNum) => cards?.ForEach(x => ADD(x, AnyNum));


        public void SHF(RuntimeCard card) => Console.WriteLine("SHF" + card.ID);

        public void SHF(List<RuntimeCard> cards) => cards?.ForEach(SHF);

        public void SET_D(string param, int value) => Console.WriteLine("SET_D" + param + value);

        public int? GET_D(string param) => param.Length;

        public void CLEAR(RuntimeCard card, int ability) => Console.WriteLine("CLEAR" + card.ID + ability);

        public void CHANGE(RuntimeCard card, int typeForm, int typeGoto) => Console.WriteLine("CHANGE" + card.ID + typeForm + typeGoto);
        public void CHANGE(RuntimeCard card, int idGoto) => Console.WriteLine("CHANGE" + card.ID + idGoto);


        public void DISCOVERY()
        {
            Select  = -1;
            Options = new List<int>();
            Options.Clear();
        }

        public void OPTION(int       option) => Options.Add(option);
        public void OPTION(List<int> option) => Options.AddRange(option);


        public void ONSELECT(string onSelect) => Console.WriteLine(onSelect);

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



        public EPhaseTypes PHASE => EPhaseTypes.TurnAI;

        public int grower => inder++;

        public static int inder = 0;



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



        public int P1ID => P1.ID;
        public int P2ID => P2.ID;

        public RuntimePlayer P1 = new RuntimePlayer();
        public RuntimePlayer P2 = new RuntimePlayer();

        public RuntimeCard C1 = new RuntimeCard();
        public RuntimeCard C2 = new RuntimeCard();
        public RuntimeCard C3 = new RuntimeCard();
    }


    public enum EPos { Void = 0, Officer_L, Officer_R, Officer_M, Discard, Deck_Officer, Deck_Soldier, Hand, }

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

    public class DuelData { }


    public enum ESoldierType { }


    public static class GameConfig
    {
        public static ReadOnlyCollection<CardData_Officer> CollectionOfficerCards;

        public static ReadOnlyCollection<CardData_Soldier> CollectionSoldierCards;

        public static ReadOnlyDictionary<int, AbilityData_Base> CollectionAbilities;
    }

}