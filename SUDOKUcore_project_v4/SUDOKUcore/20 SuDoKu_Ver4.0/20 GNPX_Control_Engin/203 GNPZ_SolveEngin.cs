using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Media;

using static System.Diagnostics.Debug;
using static System.Math;

using GIDOO_space;
using GIDOOCV;
using System.Reflection;
using System.Windows.Documents;
using System.Reflection.Emit;
using SUDOKUcore;
using System.Windows.Interop;

namespace GNPXcore{
    public partial class GNPZ_Engin{                        // This class-object is unique in the system.
        static public bool       SolInfoB;    
        static public int        eng_retCode;                   // result
        static public UAlgMethod AnalyzingMethod;
        static public TimeSpan   SdkExecTime;  
        static public bool       SolverBusy=false;
                       
        private bool             __ChkPrint__ = false; // true; // for debug 

        private GNPX_App         pGNP00;                    // main control     
        public  GNPX_AnalyzerMan AnMan;                     // analyzer(methods)


        //    { get; set; } ... Techniques used for debugging.
        //                      Find out where unexpected assignments are made.
        //
      //public  UPuzzle          PZL_Initial{ get; set; }
      //public  UPuzzleMan       PZLMan{ get; set; }         // property to find out where set is executed

        public  UPuzzle          PZL_Initial;
        public  UPuzzleMan       PZLMan;

        public UPuzzle           pPZL{    get=>PZLMan.PZL; set=>PZLMan.PZL=value; } // Puzzle to analyze
        public List<UCell>       pBOARD{  get=>pPZL.BOARD; }
        private int              stageNo{ get=>PZLMan.stageNo; }

        public  List<UPuzzle>    child_PZLs  =>PZLMan.child_PZLs;
        private Random rnd = new Random();

        private List<UMthdChked> SolverLst2{ get{ return pGNP00.SolverLst2; } }  // List of algorithms to be applied.
        private List<UAlgMethod> MethodLst_Run = new List<UAlgMethod>();          // List of successful algorithms
        public  UAlgMethod       curentMethod;


        
        public GNPZ_Engin( GNPX_App pGNP00 ){           //Execute once at GNPX startup.
            this.PZLMan = new UPuzzleMan();
            this.pGNP00 = pGNP00;
            AnMan = new GNPX_AnalyzerMan(this);
        }


      #region Puzzle management
        public void Clear_0(){
            pPZL.ToInitial( ); // Clear all.
            AnMan.Update_CellsState( pPZL.BOARD, setAllCandidates:true );
        }
        public bool IsSolved() => pBOARD.All(p=> p.No!=0);
        // Set_NewPuzzle : Set the analysis Puzzle to the engine -> PZL_Initial.        
        public void Set_NewPuzzle( UPuzzle aPZL ){   
            this.PZL_Initial = aPZL;

            UPuzzle pGP000 = aPZL.Copy();
            this.PZLMan = new UPuzzleMan( pGP000, this );                // Create the UPuzzleMan for Analize.
            PZLMan.selectedIX = -1;
            this.PZLMan.stageNo = 0;                                     // set stageNo=0.
            this.PZLMan.method_maxDif = null;
        }

        public void Set_selectedChild( int selX ){
            int cnt = PZLMan.child_PZLs.Count;
            if( selX<0  || cnt<=0 || selX>=cnt ) return;
            pPZL = PZLMan.child_PZLs[selX];
            PZLMan.PZL = pPZL;                            //Set the chosen GP.
            PZLMan.selectedIX = selX;
        }

        public bool Set_NextStage( int _stageNo=-1 ){      // update the state and create the next stage.
            UPuzzle tPZL = null;
            if( _stageNo == 0 )  PZLMan.stageNo = 0;

            if( PZLMan.stageNo == 0 ){
                tPZL = PZL_Initial;
                AnMan.Update_CellsState( tPZL.BOARD, setAllCandidates:true );
            }
            else{
                int selectedIX = PZLMan.selectedIX;
                if( selectedIX < 0 )  return false; 
                tPZL = PZLMan.child_PZLs[selectedIX]; 
            }
            
            if( tPZL.BOARD.All(p=>p.No!=0) )  return false; //solved

            UPuzzleMan tPZLManNext = new UPuzzleMan();

         // PZLMan.GPManNxt        = tPZLManNext; //202303X
            tPZLManNext.PZLManPre  = PZLMan;
            tPZLManNext.PZL        = tPZL.Copy();
            tPZLManNext.stageNo    = PZLMan.stageNo+1;

                    //  tPZLManNext.UPuzzleMan_stack_history(tPZLManNext,"Before running PExecute_Fix_Eliminate ----");
            var (codeX,_) = AnMan.Execute_Fix_Eliminate( tPZLManNext.PZL.BOARD );
                // codeX  0:Complete. Go to next stage.  1:Solved.   -1:Error. Conditions are broken.
                    //  tPZLManNext.UPuzzleMan_stack_history(tPZLManNext,"After running PExecute_Fix_Eliminate +++++");
            if( codeX<0 ){  eng_retCode = -998; return false; }

            PZLMan = tPZLManNext;
            PZLMan.child_PZLs.Clear();
            return true;            //check_pGP(aPZL,"Set_NextStage");   
        }

        public bool Restore_PreStage( ){
            if( PZLMan.PZLManPre is null )  return false;
            PZLMan = PZLMan.PZLManPre;
            return true;
        }

/*  //202303X
        public bool Restore_NxtStage( ){
            if( PZLMan.GPManNxt is null )  return false;
            PZLMan = PZLMan.GPManNxt;
            return true;
        }
*/
        public void ReturnToInitial(){
            if( PZL_Initial is null )  PZL_Initial = pPZL;
            this.PZLMan = new UPuzzleMan( PZL_Initial, this );
            
            this.pPZL = PZL_Initial.Copy();
            this.PZLMan.stageNo = 0;
            this.PZLMan.PZL.ToInitial( ); //to initial stage
        }
      #endregion Puzzle management


      #region Methods_for_Solving functions
        // Listing of analysis methods
        public int Set_Methods_for_Solving( bool AllMthd=false, bool GenLogUse=true ){ 

            MethodLst_Run.Clear(); 
            foreach( var S in SolverLst2 ){
                if( !AllMthd && !S.IsChecked )  continue;
                if( S.Name==" GeneralLogic" && !GenLogUse )  continue;
                var Sobj = AnMan.SolverLst0.Find(P=>P.MethodName==S.Name); // List solver-object by name
                MethodLst_Run.Add(Sobj);    // List of algorithms to be applied.
            }
            
             //  WriteLine( $"#####Set_Methods_for_Solving  MethodLst_Run.Count:{MethodLst_Run.Count}" );  // Find out how it's used       
            return MethodLst_Run.Count;
        }

        public void MethodLst_Run_Reset(){
            MethodLst_Run.ForEach(P=>P.UsedCC=0);
            var Q = PZLMan.PZLManPre;
            while( Q != null ){ PZLMan=Q; Q=Q.PZLManPre; /*Thread.Sleep(1);*/ }
        } 

        public string DGViewMethodCounterToString(){
            var Q = MethodLst_Run.Where(p=>p.UsedCC>0).ToList();
            return Q.Aggregate("",(a,q) => a+$" {q.MethodName}[{q.UsedCC}]" );
        }

        public void AnalyzerCounterReset(){ MethodLst_Run.ForEach(P=>P.UsedCC=0); } //Clear the algorithm counter. 

        public int  Get_DifficultyLevel( out string prbMessage ){
            int DifL=0;
            prbMessage="";
            if( MethodLst_Run.Any(P=>(P.UsedCC>0) )){
                DifL = MethodLst_Run.Where(P=>P.UsedCC>0).Max(P=>P.DifLevel);
                var R = MethodLst_Run.FindLast(Q=>(Q.UsedCC>0)&&(Q.DifLevel==DifL));
                prbMessage = (R!=null)? R.MethodName: "";
            }
            return DifL;
        }
      #endregion Methods_for_Solving functions


      #region Analyzer
        // simply solve puzzles
        public void sudoku_Solver_Simple( CancellationToken ct ){
            try{
                eng_retCode=0;
                    // WriteLine( "@@@@@@@@@@@@@@@@@@@@@@@@ sudoku_Solver_Simple  ----" );  202303-beta

                AnMan.Update_CellsState( pBOARD, setAllCandidates:true );  // allFlag:true : set all candidates
                Stopwatch AnalyzerLap = new Stopwatch();
                AnalyzerLap.Start();

                Set_NewPuzzle( pPZL );        

                UPuzzleMan GPManNext=null;
                while(true){
                    if( ct.IsCancellationRequested ){ ct.ThrowIfCancellationRequested(); return; }

                    if( PZLMan.stageNo > 0 ){
                        var (codeX,_) = AnMan.Execute_Fix_Eliminate( pPZL.BOARD );
                        if( codeX<0 ){  eng_retCode=-998; return; }  //codeX=-1:Error. Conditions are broken.
                        if( codeX==1 )  break;                       //codeX=0 :Solved. 
                    }

                    // ================================================
                    var (ret,ret2) = sudoku_Solver_SingleStage( ct, false );  // <-- 1-step solver
                    if( !ret ){ eng_retCode=-999; break; }

                    SdkExecTime = AnalyzerLap.Elapsed;
                    if( eng_retCode<0 )  return;
                    PZLMan.stageNo++;
                }
                AnalyzerLap.Stop();        

                var (_,nP,nZ,nM) = AnMan.Aggregate_CellsPZM(pBOARD);
                eng_retCode = nZ;
            }
            catch( OperationCanceledException ){}
            catch( Exception e ){
                WriteLine( e.Message+"\r"+e.StackTrace );
                using(var fpW=new StreamWriter("Exception_201_0.txt",true,Encoding.UTF8)){
                    fpW.WriteLine($"---{DateTime.Now} {e.Message} \r{e.StackTrace}");
                }
            }
        }
       
        // Solve up
        public void sudoku_Solver_Complete( CancellationToken ct ){  //202303-beta
            try{
                eng_retCode=0;

                Stopwatch AnalyzerLap = new Stopwatch();
                AnalyzerLap.Start();    

                UPuzzleMan GPManNext=null;
                PZLMan.stageNo = 0;
                while(true){
                    if( ct.IsCancellationRequested ){ ct.ThrowIfCancellationRequested(); return; }

                    bool retB = Set_NextStage(PZLMan.stageNo);
                    if( !retB )  return;                    // go to the next stage

                    // ================================================
                    var (ret,ret2) = sudoku_Solver_SingleStage( ct, false ); // <-- 1-step solver
                    if( !ret ){ eng_retCode=-999; break; }
                    // -------------------------------------------------

                    SdkExecTime = AnalyzerLap.Elapsed;
                    if(eng_retCode<0)  return;
                }
                AnalyzerLap.Stop();        

                var (_,nP,nZ,nM) = AnMan.Aggregate_CellsPZM(pBOARD);
                eng_retCode=nM;
            }
            catch(OperationCanceledException){}
            catch(Exception e){
                WriteLine( e.Message+"\r"+e.StackTrace );
                using(var fpW=new StreamWriter("Exception_201_0.txt",true,Encoding.UTF8)){
                    fpW.WriteLine($"---{DateTime.Now} {e.Message} \r{e.StackTrace}");
                }
            }
        }

        // 1-step solver
        public (bool,string) sudoku_Solver_SingleStage( CancellationToken ct, /*ref int ret2,*/ bool SolInfoB ){
            if(__ChkPrint__) WriteLine( $"\n### stageNo:{stageNo}" );
        
            Stopwatch AnalyzerLap = new Stopwatch();
            bool mltAnsSearcB = SDK_Ctrl.MltAnsSearch;
            bool AnalysisResult=false;
            int  mCC=0, notFixedCells=0, freeDigits=0;

            // --- Initialize ---           
            pPZL.Sol_ResultLong = "";
            var (lvlLow,lvlHgh) = Set_AcceptableLevel( );
            AnMan.Initialize_Solvers();

            do{
                try{
					if( pBOARD.All(p=>(p.FreeB==0)) ) break; // All cells confirmed.
                    #region  Verify the solution
                    if( !AnMan.Verify_SUDOKU_Roule() ){
                        if( SolInfoB )  pPZL.Sol_ResultLong = "no solution";
                        return (false,"no solution");
                    }
                    #endregion  Verify the solution
                            
                    AnalyzerLap.Start();
                    DateTime MltAnsTimer = DateTime.Now;

                    //===================================================================================
                    pPZL.SolCode=-1;
                    bool L1SolFound=false;
                    foreach( var P in MethodLst_Run ){                                  // Sequentially execute analysis algorithms
                        if( ct.IsCancellationRequested ){ return (false,"canceled"); }   // Was the task canceled?

                        #region Execution/interruption of analysis method by difficulty 
                        if( L1SolFound && P.DifLevel>=2 )  goto LBreak_Analyzing;       //Stop if there is a solution with a difficulty level of 2 or less.

                        int lvl = P.DifLevel; 
                        int lvlAbs = Abs(lvl);
                        if( lvlAbs > lvlHgh )  continue;                                // Exclude methods with difficulty above the limit
                        if( !mltAnsSearcB && lvl<0 )  continue;  // The negative level algorithm is used only with multiple soluving.
                        #endregion Execution/interruption of analysis method by difficulty 

                        #region Algorithm execution
                        try{
                                if(__ChkPrint__) WriteLine( $"---> stageNo:{stageNo}  DifLevel:{P.DifLevel}  method: {(mCC++)} - {P.MethodName}");
                            // *==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*
                            GNPZ_Engin.AnalyzingMethod = P;
                            AnalysisResult = P.Method();            // <--- Execute
                            // *==*==*==*==*==*==*==*==*==*==*==*==*==*==*

                            if( AnalysisResult ){ // Solvrd
                                if(__ChkPrint__) WriteLine( $"========================> solved {P.MethodName}" );
                                if(  PZLMan.method_maxDif!=null && P.DifLevel>=PZLMan.method_maxDif.DifLevel )  PZLMan.method_maxDif = P;

                                // --- analysis is successful!  save the method and difficulty.
                                if( P.DifLevel<=2 )  L1SolFound=true;
                                P.UsedCC++;                  // Counter for the number of times the algorithm has been applied
                                pPZL.pMethod = P;     

                                if( !mltAnsSearcB )  goto LBreak_Analyzing;     // Abort if single search
                                else{
                                                                    }
                                // -------------------------------------------
                                if( (string)GNPX_App.GMthdOption["abortResult"]!="" )  break; // "time over"...
                            }
                        }
                        catch( Exception e ){
                            WriteLine( e.Message+"\r"+e.StackTrace );
                            using(var fpW=new StreamWriter("Exception_in_Engin.txt",true,Encoding.UTF8)){ // message for debug 
                                fpW.WriteLine($"---{DateTime.Now} {e.Message} \r{e.StackTrace}");
                            }
                            break;
                        }
                        #endregion  Algorithm execution
                    } 
                    //----------------------------------------------------------------------------
/*
                    if( mltAnsSearcB ){
                        if( child_PZLs.Count > 0 ){
                            pPZL = child_PZLs.First();
                            SDK_Ctrl.UGPMan.pPZL = pPZL;
                            AnalysisResult = true;
                            goto LBreak_Analyzing;
                        }
                    }
*/
                                    if(__ChkPrint__) WriteLine( "========================> can not solve");
                    if( SolInfoB )  pPZL.Sol_ResultLong = "no solution";
                    return (false,"no solution");

                }
                catch(OperationCanceledException){}
                catch(Exception e){
                        WriteLine(e.Message+"\r"+e.StackTrace);
                        using(var fpW=new StreamWriter("ExceptionXXX_2.txt",true,Encoding.UTF8)){
                            fpW.WriteLine($"---{DateTime.Now} {e.Message} \r{e.StackTrace}");
                        }
                        break;
                }
                finally{
                    AnalyzerLap.Stop();
                    SDK_Ctrl.solLevel_notFixedCells = notFixedCells = pBOARD.Count(p => (p.FreeB!=0));
                    SDK_Ctrl.solLevel_freeDigits = freeDigits = pBOARD.Aggregate(0,(t,p)=>t+p.FreeBC);
                      // WriteLine( $"### solLevel_notFixedCells:{notFixedCells}  solLevel_freeDigits:{freeDigits}" ); // for debug
                }

            }while(notFixedCells>0);

            if( notFixedCells <= 0 ){ AnalysisResult=true; }  //solved

          LBreak_Analyzing:  //found
            SdkExecTime = AnalyzerLap.Elapsed;

            PZLMan.selectedIX = (child_PZLs!=null && child_PZLs.Count>0 )? 0: -1;
            return (AnalysisResult,"");  // "": "solved"  



            // --------------------------------------- inner functions ---------------------------------------
            (int,int) Set_AcceptableLevel( ){
                string AnalyzerMode = pGNP00.AnalyzerMode;
                int _lvlLow = 1;
                int _lvlHgh = 5;

                if( AnalyzerMode == "CreatePuzzle"  ){
                    _lvlLow = SDK_Ctrl.lvlLow;
                    _lvlHgh = SDK_Ctrl.lvlHgh;
                }
                else if( AnalyzerMode=="Solve" || AnalyzerMode=="SolveUp" ){     // ... ?
                    _lvlLow = 1;
                    _lvlHgh = 20;
                }
                else if( AnalyzerMode == "MultiSolve" ){
                    _lvlLow = 1;
                    _lvlHgh = (int)GNPX_App.GMthdOption["MSlvrMaxLevel"];
                }
                return (_lvlLow,_lvlHgh);
            }

        }
      #endregion Analyzer

    }
}