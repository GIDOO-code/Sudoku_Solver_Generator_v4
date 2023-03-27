using System;
using System.Linq;
using System.Collections.Generic;
using static System.Diagnostics.Debug;
using static System.Math;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection.Metadata.Ecma335;

namespace GNPXcore{

    // ### This class will change in the future.  ............
    //     Organize items into the following classes. ---> GNPX_AnalyzerMan, SDK_Ctrl, GNPZ_Engin, ...

    public partial class SDK_Ctrl{    
        static public event   SDKEventHandler Send_Progress; 
        static public Random  GRandom= new Random();
        static public int     TLoopCC = 0;
        static public int     lvlLow;
        static public int     lvlHgh;
        static public bool    NumRandmize;
        static public bool    FilePut_GenPrb;
        static public GNPZ_Engin   pGNPX_Eng;

        private UPuzzleMan    pPZLMan => pGNPX_Eng.PZLMan;
        private UPuzzle       pPZL    => pPZLMan.PZL;      // current board


        private GNPX_App      pGNP00;
        private NuPz_Win      pGNP00win{ get=> pGNP00.pGNP00win; } 
        private bool          __SimpleAnalyzerB__ => AnalyzerBaseV2.__SimpleAnalyzerB__;
      
      //===== Multiple Solution Analysis ====================
        static public bool   MltAnsSearch;
        static public int    MltProblem;
        static public bool   GenLS_turbo;
        static public UPuzzleMan UGPMan=null;       // <= 202303  not recommended, Deprecated object.
      //---------------------------------------------------

        public int           retNZ; 
        
        public int           CellNumMax;
        public int           LoopCC=0;
        public int           PatternCC=0;

        public int           ProgressPer;
        public bool          CanceledFlag;
        public int           GenLStyp;
        public bool          CbxNextLSpattern;   //Change Latin-Square Pattern on Success
    
        public PatternGenerator PatGen;         //Puzzle Pattern

        public int          randomSeedVal=0;
        public bool         threadRet;

        private bool        _DEBUGmode_= false; //false; //true;// 

        public SDK_Ctrl( GNPX_App pGNP00, int FirstCellNum ){
            this.pGNP00 = pGNP00;
            Send_Progress += new SDKEventHandler(pGNP00win.BWGenPrb_ProgressChanged);      
            
            CellNumMax = FirstCellNum; 

            PatGen = new PatternGenerator( this );
            LSP    = new LatinSquareGen( );
        }

        static public void Clear(){
            if( SDK_Ctrl.UGPMan is null )  return;
        }

        public bool CheckPattern(){
            int cnt=0;
            for(int rc=0; rc<81; rc++ ){
                if(PatGen.GPat[rc/9,rc%9]>0) cnt++;
            }
            return (cnt>17);
        }
        private void _ApplyPattern( int[] X ){
            for(int rc=0; rc<81; rc++ ) if(PatGen.GPat[rc/9,rc%9]==0) X[rc]=0;
                    //if( _DEBUGmode_ )  __DBUGprint2(X, "_ApplyPattern");
        }
        private void _ApplyPattern( int[,] X2 ){
            for(int rc=0; rc<81; rc++ ) if(PatGen.GPat[rc/9,rc%9]==0) X2[rc/9,rc%9]=0;
                    //if( _DEBUGmode_ )  __DBUGprint2(X, "_ApplyPattern");
        }
        
        public void SetRandomSeed( int rs ){
#if DEBUG
            randomSeedVal = rs;         // Debug with fixed conditions.
#else
            if(rs==0){
              //int nn=Environment.TickCount&Int32.MaxValue;
                int nn = (int)DateTime.Now.Ticks;
                randomSeedVal = Abs(nn);
                WriteLine($"DateTime.Now.Ticks:{DateTime.Now.Ticks}  randomSeedVal:{randomSeedVal}");
            }
#endif
            GRandom=null; 
            GRandom=new Random(randomSeedVal);
        }

    #region Generate Puzzle Candidate
        internal int[,] ASDKsol = new int[9,9];
        private int[] prKeeper = new int[9];
        private Random rnd = new Random();
        public List<UCell> GeneratePuzzleCandidate( ){      // Generate puzzle candidate
            int[]  P = Create_SolutionCandidatesList(SDK_Ctrl.NumRandmize, GenLStyp); //*****

            List<UCell> BDLa = new List<UCell>();
            for(int rc=0; rc<81; rc++ )  BDLa.Add(new UCell(rc,P[rc]));
            if( _DEBUGmode_ ) __DBUGprint(BDLa);

            //__DBUGprint(BDLa);      // for debug

            return BDLa;
        }    
        private void __DBUGprint( List<UCell> BOARD ){
            string st;
            WriteLine("\r");
            for(int r=0; r<9; r++ ){
                st = r.ToString("            ##0:");
                for(int c=0; c<9; c++ ){
                    int No = BOARD[r*9+c].No;
                    if( No==0 ) st += " .";
                    else st += No.ToString(" #");
                }
                WriteLine(st);
            }
        }
    #endregion Generate puzzle candidate

    #region Create Puzzle
        //====================================================================================
        public void SDK_PuzzleMaker_Real( CancellationToken ct ){ //Creating problems[Automatic]
            // 1)Run task in main　　2)Here is the task　　3)Generate Latin-Square
            // 　　④エンジン上に PZLMan+pPZL 生成　　⑤Auto_simpleで解く

            int solCC=0;
            try{
                int mlt = MltProblem;                   // Number of puzzles to solve
                pGNPX_Eng.Set_Methods_for_Solving();    // Prepare the method for analysis





                do{
                    if(ct.IsCancellationRequested){ ct.ThrowIfCancellationRequested(); return; }

                    // =================================================== Generate problem candidate.
                    LoopCC++; TLoopCC++;
                    List<UCell>   BOARD = GeneratePuzzleCandidate( ); // 
                    UPuzzle qGP = new UPuzzle( BOARD );               // new UPuzzle
                    pGNPX_Eng.Set_NewPuzzle( qGP );                 // new UPuzzleMan

                    // =================================================== Solving
                    if( !__SimpleAnalyzerB__ )  pGNPX_Eng.AnalyzerCounterReset();
                    pGNPX_Eng.sudoku_Solver_Simple(ct);         // Apply algorithms to candidate and try to solve them.
                    // ---------------------------------------------------    

                    if( GNPZ_Engin.eng_retCode==0 ){
                        var tmpPZL = pGNPX_Eng.PZLMan.method_maxDif;
                        int DifLevel = (tmpPZL!=null)? tmpPZL.DifLevel: SDK_Ctrl.lvlLow;
                        if( DifLevel<SDK_Ctrl.lvlLow || SDK_Ctrl.lvlHgh<DifLevel )  continue;
#if DEBUG
                        __ret001=true;  ////for debug...Generating Puzzle Candidates(Latin Square)   
#endif                  
                        
                        qGP.DifLevel = DifLevel;
                        qGP.Name = (tmpPZL!=null)? tmpPZL.MethodName: "";
                        qGP.TimeStamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                        qGP.solMessage = pGNPX_Eng.DGViewMethodCounterToString();
                        pGNP00.save_created_PUZZLE(qGP);

                        SDKEventArgs se = new SDKEventArgs( ePara0:MltProblem, ePara1:(--mlt));
                        Send_Progress( this, se );             //(can send information in the same way as LoopCC.)
                        if( CbxNextLSpattern ) rxCTRL=0;      //Change LS pattern at next problem generation
                    }
                }while( mlt>0 ); 
                // ... Reached target number of puzzles
            }
            catch(TaskCanceledException){ WriteLine("...Canceled by user."); }
            catch(Exception ex){ WriteLine(ex.Message+"\r"+ex.StackTrace); }           
        }
    #endregion Create Puzzle

    #region Analizer
        public void Analyzer_Real( CancellationToken ct ){      //Analysis[step]
            try{
                retNZ=-1; LoopCC++; TLoopCC++;
                if( pGNPX_Eng.Set_Methods_for_Solving(false) < 0 )  return;      // Run every analysis
                pGNPX_Eng.sudoku_Solver_SingleStage( ct, true );
              //  SDKEventArgs se = new SDKEventArgs(ProgressPer:retNZ);
              //  Send_Progress(this,se);
            }
            catch(TaskCanceledException){ WriteLine("...Canceled by user."); }
            catch(Exception ex){ WriteLine(ex.Message+"\r"+ex.StackTrace); }   
        }





        public void Analyzer_RealAuto( CancellationToken ct ){   //Analysis[solveUp]
            try{
                LoopCC++; TLoopCC++;
                bool chbConfirmMultipleCells = GNPX_App.chbConfirmMultipleCells;
                if( pGNPX_Eng.Set_Methods_for_Solving(false) < 0 )  return;      // Run every analysis
                pGNPX_Eng.sudoku_Solver_Complete(ct);
              //  SDKEventArgs se = new SDKEventArgs(ProgressPer:(GNPZ_Engin.eng_retCode));
              //  Send_Progress(this,se);
            }
            catch(TaskCanceledException){ WriteLine("...Canceled by user."); }
            catch(Exception ex){ WriteLine(ex.Message+"\r"+ex.StackTrace); }   
        }



    #endregion Analizer
 
        private void __DBUGprint2( int[,] pSol99, string st="" ){
            string st2, st0="";
            WriteLine("\r");
            for(int r=0; r<9; r++ ){
                st2 = st+r.ToString("##0:");
                for(int c=0; c<9; c++ ){
                    int wk=pSol99[r,c];
                    if(wk==0) st2 += " .";
                    else st2 += wk.ToString(" #");
                    st0 += wk.ToString();
                }
                WriteLine(st2);
            }
            WriteLine(st0);
        }

        private void __DBUGprint2( int[] X, bool sqF, string st="" ){
            string st2, p2="";
            if(sqF) WriteLine("\r");
            for(int r=0; r<9; r++ ){
                st2 = "";
                for(int c=0; c<9; c++ ){
                    int wk=Abs(X[r*9+c]);
                    if(wk==0) st2 += " .";
                    else st2 += wk.ToString(" #");
                }
                if(sqF) WriteLine(st+r.ToString("##0:")+st2);
                p2 += " "+st2.Replace(" ","");
            }
            WriteLine(st+" "+p2);
        }
    }
}
    