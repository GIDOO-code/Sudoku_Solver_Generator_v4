using System;
using System.Collections.Generic;
using static System.Math;
using static System.Diagnostics.Debug;

using GIDOO_space;
using System.Linq;
using System.Windows.Documents;

namespace GNPXcore{

    // UPuzzleMan : Class for multiple solution analysis.
    //   Represent the solution in a tree structure.
    //   There are elements that represent the current state and the state of parents and children.
    public class UPuzzleMan{
        static private Random rand = new Random(1);
        static private int   ID00=0;

        private GNPZ_Engin   pGNPX_Eng;
        public UPuzzleMan    PZLManPre  = null;    // parents.
     // public UPuzzleMan    GPManNxt   = null;    // child.     202303X
     
        public UPuzzle       PZL        = null;    // current state
        public List<UPuzzle> child_PZLs = null;    // children
        public int           stageNo    = 0;       // stage No.
        public int           selectedIX = -1;
        public int           _objectKey;           // for debug ... solved

        public UAlgMethod    method_maxDif = new UAlgMethod();
        public string IsNull(object X) => (X is null)? "--null": "++object";

        public UPuzzleMan( ){
            this.PZLManPre = null;
            this.PZL      = new UPuzzle();
            this.child_PZLs = new List<UPuzzle>();
            this.stageNo  = 0;
            this._objectKey = rand.Next()/1000*1000 + (ID00++);
        }
        public UPuzzleMan( UPuzzle GParg, GNPZ_Engin pGNPX_Eng=null ): this( ){
            if( pGNPX_Eng != null )  this.pGNPX_Eng = pGNPX_Eng;
            this.PZL = GParg?? new UPuzzle();;
        }






        public void Add_Solution( UPuzzle aPZL ){
            child_PZLs.Add(aPZL);
        }

        public (UPuzzleMan,int)  Restore_PreStage( UPuzzle aPZL ){
            int selectedIX = this.child_PZLs.FindIndex(p=>p==aPZL);
            return (PZLManPre,selectedIX); 
        }
/*
        public override string ToString( ){
            string st = $"UPuzzleMan  stageNo:{stageNo} PZLManPre:{IsNull(PZLManPre)}";
            return st;
        }
*/
/*
        private string check_pGP( UPuzzle aPZL, string name ){
            string st = $"{name}  ID:{aPZL.ID}  IDm:{aPZL.ID_obj}  ";
            foreach( var P in aPZL.BOARD ){
                if( P.rc%9==0 ) st += " ";
                int n = P.No;
                st += ((n>=0)? $" {n}": $"{n}"); 
            }

            st += "  FixedNo:";
            foreach( var P in aPZL.BOARD.Where(p=>p.FixedNo>0) )   st += $" , {P.rc.ToRCString()}#{P.FiaPZLedNo }";
            
            WriteLine(st);
            return st;
        }
*/

        public void UPuzzleMan_stack_history( UPuzzleMan aPZLMan, string msg="" ){
            WriteLine( $"\r============== {msg}");
            var Q = aPZLMan;
            while(Q!=null){
                WriteLine( $"stage:{Q.stageNo} {__BDL_ToString(Q.PZL)}" );
                if( child_PZLs !=null ){
                    int ch=0;
                    child_PZLs.ForEach(R=> WriteLine( $"     #{ch++} {__BDL_ToString(R)}") );
                }
                Q = Q.PZLManPre;
            }
        }

        private static string __BDL_ToString( UPuzzle aPZL ){
            string st = $" ID:{aPZL.ID} ID_obj:" + aPZL.ID_obj.ToString().PadLeft(7, '0') + "  ";
            foreach(var P in aPZL.BOARD) {
                if(P.rc % 9 == 0) st += " #";
                int n = P.No;
                //st += ((n>=0)? $" {n}": $"{n}"); 
                st += ((n==0)? " .": (n>0)? $" {n}": $"{n}");
            }
            
            st += "  FixedNo:";
            foreach( var P in aPZL.BOARD.Where(p=>p.FixedNo>0)) st += $" , {P.rc.ToRCString()}#{P.FixedNo}";

            st += "  Canceled: ";
            foreach( var P in aPZL.BOARD.Where(p=>p.CancelB>0)) st += $" , {P.rc.ToRCString()}#{P.CancelB.ToBitStringN(9)}";

            if( aPZL.pMethod != null )  st += $" method:{aPZL.pMethod.MethodName}";
            return st;
        }
    }





    // UPuzzle : Class of Sudoku problem
    public class UPuzzle{
        static private int _IDpzl000=-1;            //Used twice in preparation. (1,2,3,...)
        private Bit81[]    pConnectedCells{ get{ return AnalyzerBaseV2.ConnectedCells; } }

        private int        _ID_obj;
        public int         ID_obj{ 
                                get =>  _ID_obj;
                                set{ _ID_obj=value; /*WriteLine( $"_ID_obj:{_ID_obj}" );*/ }  
                            }
        public int         ID;
        public List<UCell> BOARD;                     // Board cells(81 cell)  Defined in List for use the Linq functions.
        public int[]       AnsNum;                  // used in PuzzleTrans

        public long        HTicks;                  // Time measurement during analysis.
        public string      Name;                    // Name
        public string      TimeStamp;               // TimeStamp

        private int         DifLevel_13;
        public int         DifLevel;    //{ get; set; }    // -1:InitialState　0:Manual
      //public int         DifLevel{ get{ return DifLevel_13; } set{ DifLevel_13=value; } }    // -1:InitialState　0:Manual
        public bool        improper;                // No solution (depending on the method used)
        public UAlgMethod  pMethod = null;          // Analytical method
        public string      solMessage;              // Message of how to solve
        public string      Sol_Result;  //{ get; set; }  // Solution description
        public string      Sol_ResultLong;          // Solution description(Long)
        public string      extResult;   //{ get; set; }   // Solution description(ext.)

        public string      __SolResultKey;          // key for identity confirmation
        public string      AnalyzingMethodName;
      
        public int         SolCode;

        public List<UPuzzle> child_PZLs = new List<UPuzzle>();
   
        public UPuzzle( ){
            this.ID_obj    = _IDpzl000++;
            this.BOARD      = new List<UCell>();
            for(int rc=0; rc<81; rc++ ) this.BOARD.Add(new UCell(rc));
            this.DifLevel = 0;
            this.extResult = "";
            this.Sol_ResultLong = "";
            this.__SolResultKey = "";
            this.HTicks = DateTime.Now.Ticks;
        }
        public UPuzzle( string Name ): this(){ this.Name=Name; }

        public UPuzzle( List<UCell> BOARD ): this(){
            this.BOARD      = BOARD;
            this.DifLevel = 0;
            this.HTicks   = DateTime.Now.Ticks;
        }

        public UPuzzle( int ID, List<UCell> BOARD, string Name="", int DifLvl=0, string TimeStamp="" ): this(){
            this.ID       = ID;
            this.BOARD    = BOARD;
            this.Name     = Name;
            this.DifLevel = DifLvl;
            this.TimeStamp = TimeStamp;
            this.HTicks   = DateTime.Now.Ticks;
        }
       
        public UPuzzle Copy( int stageNo_Increments=0, int IDm=0 ){
            UPuzzle tmpPZL = (UPuzzle)this.MemberwiseClone();
            tmpPZL.ID_obj  = _IDpzl000*10000 + (this.ID_obj)%1000+1; 
            tmpPZL.BOARD = new List<UCell>();
            foreach( var q in BOARD ) tmpPZL.BOARD.Add(q.Copy());
            tmpPZL.HTicks = DateTime.Now.Ticks;;
            return tmpPZL;
        }

        public void ToPreStage( ) => ToInitial( resetAll:false );
        public void ToInitial( bool resetAll=true ){
            this.BOARD.ForEach( p => p.Reset_result(resetAll:resetAll) );
            this.AnsNum     = null;     // used in PuzzleTrans
         // this.DifLevel   = -1;       // -1:InitialState　0:Manual
            this.improper   = false;    // No solution (depending on the method used)
            this.pMethod    = null;     // Analytical method
            this.solMessage = "";       // Message of how to solve
            this.Sol_Result = "";       // Solution description
            this.Sol_ResultLong = "";   // Solution description(Long)
            this.extResult  = "";       // Solution description(ext.)

            this.__SolResultKey = "";   // key for identity confirmation
            this.AnalyzingMethodName = "";
      
            this.SolCode    = 0;
            this.child_PZLs  = null;
        }

        public string CopyToBuffer( bool KeySft=false ){
            string st = "";
            if( KeySft ) st = BOARD.ConvertAll(q=> Abs(q.No)).Connect("").Replace("0",".");
            else         st = BOARD.ConvertAll(q=> Max(q.No,0)).Connect("").Replace("0",".");
            return st;
        }

        public string ToGridString( bool SolSet ){
            string st="";
            BOARD.ForEach( P =>{
                st+=(SolSet? P.No: Max(P.No,0));
                if( P.c==8 ) st+="\r";
                else if( P.rc!=80 ) st+=",";
            } );
            return st;
        }

        public string ToString_check( UPuzzle aPZL ){
            string st = $" ID:{aPZL.ID} ID_obj:{aPZL.ID_obj}  ";
            foreach( var P in aPZL.BOARD ){
                if( P.rc%9==0 ) st += " ";
                int n = P.No;
              //st += ((n>=0)? $" {n}": $"{n}"); 
                st += ((n==0)? " .": (n>0)? $" {n}": $"{n}"); 
            }

            st += "  FixedNo:";
            foreach( var P in aPZL.BOARD.Where(p=>p.FixedNo>0) )   st += $" , {P.rc.ToRCString()}#{P.FixedNo }";

            st += "  Canceled: ";
            foreach( var P in aPZL.BOARD.Where(p=>p.CancelB>0) )   st += $" , {P.rc.ToRCString()}#{P.CancelB.ToBitStringN(9)}";

         //   WriteLine(st);
            return st;
        }

    }


    // UProbS : Class for multiple solution analysis.
    //   Elements are information about the analysis result.
    public class UProbS{
        public int        __ID{ get; set; }
        public int        DifLevel{ get; set; }
        public string     Sol_Result{ get; set; }
        public string     Sol_ResultLong{ get; set; }
/*
        public UProbS( int IDmp1, int DifLevel, string Sol_Result, string Sol_ResultLong ){
            this.__ID = IDmp1; this.DifLevel = DifLevel;
            this.Sol_Result     = Sol_Result;       //new string(Sol_Result);
            this.Sol_ResultLong = Sol_ResultLong;   //new string(Sol_ResultLong);
        }
*/
        public UProbS( UPuzzle aPZL, int __ID ){
            this.__ID           = __ID;
            this.DifLevel       = aPZL.DifLevel;
            this.Sol_Result     = aPZL.Sol_Result;
            this.Sol_ResultLong = aPZL.Sol_ResultLong;
        }

        public override string ToString(){
            string st = $"IDmp1:{__ID} DifLevel:{DifLevel}  Sol_Result:{Sol_Result}";
            return st;
        }
    }
}