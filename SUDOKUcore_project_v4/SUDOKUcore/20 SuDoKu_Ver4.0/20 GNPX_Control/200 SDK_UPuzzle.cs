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
        static private int ID00=0;
        static private Random rand = new Random(1);
        private GNPZ_Engin   pGNPX_Eng;
        public UPuzzleMan    GPManPre  = null;    // parents.
        public UPuzzle       pGP       = null;    // current state
        public List<UPuzzle> child_GPs = null;    // children
        public int           stageNo   = 0;       // stage No.
        public int           selectedIX;
        public int           _objectKey;          // for debug ... solved
        public string IsNull(object X) => (X is null)? "--null": "++object";

        public UPuzzleMan( ){
            this.GPManPre = null;
            this.pGP      = new UPuzzle();
            this.child_GPs = new List<UPuzzle>();
            this.stageNo  = 0;
            this._objectKey = rand.Next()/1000*1000 + (ID00++);
        }
        public UPuzzleMan( UPuzzle GParg, GNPZ_Engin pGNPX_Eng=null ): this( ){
            if( pGNPX_Eng != null )  this.pGNPX_Eng = pGNPX_Eng;
            this.pGP = GParg?? new UPuzzle();;
            this.pGP.pMethod = null;
        }



        public UPuzzleMan Create_NextStage( UPuzzle GParg ){
            UPuzzleMan GPManNext = null;
            if( stageNo == 0 ){
                GPManNext = new UPuzzleMan(pGP);
            }
            else{
                GPManNext = new UPuzzleMan(GParg);
                GPManNext.GPManPre = this;
            }
            GPManNext.stageNo = this.stageNo+1;
                    //WriteLine( $"Create_NextStage -> stage:{stageNo}" );
                    //if(GPManPre!=null)   check_pGP(GPManNext.pGP," GPManPre.pGP:");
            return GPManNext;
        }


        public void Add_Solution( UPuzzle GPx ){
            child_GPs.Add(GPx);
        }

        public (UPuzzleMan,int)  Restore_PreStage( UPuzzle GPx ){
            int selectedIX = this.child_GPs.FindIndex(p=>p==GPx);
            return (GPManPre,selectedIX); 
        }

        public override string ToString( ){
            string st = $"UPuzzleMan  stageNo:{stageNo} GPManPre:{IsNull(GPManPre)}";
            return st;
        }

        private string check_pGP( UPuzzle X, string name ){
            string st = $"{name}  ID:{X.ID}  IDm:{X.IDm}  ";
            foreach( var P in X.BDL ){
                if( P.rc%9==0 ) st += " ";
                int n = P.No;
                st += ((n>=0)? $" {n}": $"{n}"); 
            }

            st += "  FixedNo:";
            foreach( var P in X.BDL.Where(p=>p.FixedNo>0) )   st += $" , {P.rc.ToRCString()}#{P.FixedNo }";
            
            WriteLine(st);
            return st;
        }
    }





    // UPuzzle : Class of Sudoku problem
    public class UPuzzle{
        private Bit81[]    pConnectedCells{ get{ return AnalyzerBaseV2.ConnectedCells; } }

        public int         IDm;
        public int         IDmp1{ get{ return (IDm+1);} }
        public int         ID;
//        public int         stageNo; 
        public List<UCell> BDL;                     // Board cells(81 cell)  Defined in List for use the Linq functions.
        public int[]       AnsNum;                  // used in PuzzleTrans

        public long        HTicks;                  // Time measurement during analysis.
        public string      Name;                    // Name
        public string      TimeStamp;               // TimeStamp

        public int         DifLevel{ get; set; }    // -1:InitialState　0:Manual
        public bool        improper;                // No solution (depending on the method used)
                // Stage No.
        public UAlgMethod  pMethod = null;          // Analytical method
        public string      solMessage;              // Message of how to solve
        public string      Sol_Result{ get; set; }  // Solution description
        public string      Sol_ResultLong;          // Solution description(Long)
        public string      extResult{ get; set; }   // Solution description(ext.)

        public string      __SolResultKey;          // key for identity confirmation
        public string      AnalyzingMethodName;
      
        public int         SolCode;

//        public UPuzzle     baseGP=null;
        public List<UPuzzle> child_GPs = new List<UPuzzle>();
   
        public UPuzzle( ){
            this.ID       = -1;
            this.BDL      = new List<UCell>();
            for(int rc=0; rc<81; rc++ ) this.BDL.Add(new UCell(rc));
            this.DifLevel = 0;
            this.extResult = "";
            this.Sol_ResultLong = "";
            this.__SolResultKey = "";
            this.HTicks = DateTime.Now.Ticks;
        }
        public UPuzzle( string Name ): this(){ this.Name=Name; }

        public UPuzzle( List<UCell> BDL ){
            this.BDL      = BDL;
            this.DifLevel = 0;
            this.HTicks   = DateTime.Now.Ticks;
        }

        public UPuzzle( int ID, List<UCell> BDL, string Name="", int DifLvl=0, string TimeStamp="" ){
            this.ID       = ID;
            this.BDL      = BDL;
            this.Name     = Name;
            this.DifLevel = DifLvl;
            this.TimeStamp = TimeStamp;
            this.HTicks   = DateTime.Now.Ticks;
        }
/*
        public UPuzzle Copy( UPuzzle baseGP, int stageNo, int IDm ){
            UPuzzle qGP = Copy(stageNo,IDm);
            qGP.stageNo = baseGP.stageNo;
            qGP.baseGP = baseGP;

            return qGP;
        }
*/

        public UPuzzle Copy_0( int IDm=0 ){
            UPuzzle GPtmp = (UPuzzle)this.MemberwiseClone();
            GPtmp.BDL = new List<UCell>();
            foreach( var q in BDL ) GPtmp.BDL.Add(q.Copy());
            GPtmp.HTicks  = DateTime.Now.Ticks;;
//            GPtmp.stageNo = this.stageNo+1;
            GPtmp.IDm     = IDm;

            return GPtmp;
        }      
        
        public UPuzzle Copy( int stageNo_Increments=0, int IDm=0 ){
            UPuzzle GPtmp = (UPuzzle)this.MemberwiseClone();
            GPtmp.BDL = new List<UCell>();
            foreach( var q in BDL ) GPtmp.BDL.Add(q.Copy());
            GPtmp.HTicks  = DateTime.Now.Ticks;;
//            GPtmp.stageNo = this.stageNo+stageNo_Increments;
            GPtmp.IDm     = IDm;
          //GPtmp.ToInitial();
            return GPtmp;
        }
        public void ToInitial(){
            this.BDL.ForEach( p => p.Reset_result() );
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
            this.child_GPs  = null;
        }

        public string CopyToBuffer( bool KeySft=false ){
            string st = "";
            if( KeySft ) st = BDL.ConvertAll(q=> Abs(q.No)).Connect("").Replace("0",".");
            else         st = BDL.ConvertAll(q=> Max(q.No,0)).Connect("").Replace("0",".");
            return st;
        }

        public string ToGridString( bool SolSet ){
            string st="";
            BDL.ForEach( P =>{
                st+=(SolSet? P.No: Max(P.No,0));
                if( P.c==8 ) st+="\r";
                else if( P.rc!=80 ) st+=",";
            } );
            return st;
        }
    }


    // UProbS : Class for multiple solution analysis.
    //   Elements are information about the analysis result.
    public class UProbS{
        public int        IDmp1{ get; set; }
        public int        DifLevel{ get; set; }
        public string     Sol_Result{ get; set; }
        public string     Sol_ResultLong{ get; set; }
        public UProbS( int IDmp1, int DifLevel, string Sol_Result, string Sol_ResultLong ){
            this.IDmp1 = IDmp1; this.DifLevel = DifLevel;
            this.Sol_Result     = Sol_Result;       //new string(Sol_Result);
            this.Sol_ResultLong = Sol_ResultLong;   //new string(Sol_ResultLong);
        }
        public UProbS( UPuzzle P ){
            this.IDmp1=P.IDmp1; this.DifLevel=P.DifLevel;
            this.Sol_Result=P.Sol_Result; this.Sol_ResultLong=P.Sol_ResultLong;
        }

        public override string ToString(){
            string st = $"IDmp1:{IDmp1} DifLevel:{DifLevel}  Sol_Result:{Sol_Result}";
            return st;
        }
    }
}