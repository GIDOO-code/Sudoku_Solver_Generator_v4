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
        static public string     AnalyzingMethodName="";
        static public TimeSpan   SdkExecTime;  
        static public bool       SolverBusy=false;
                       
        private bool             __ChkPrint__ = false;   // true; // for debug 

        private GNPX_App         pGNP00;                    // main control     
        public  GNPX_AnalyzerMan AnMan;                     // analyzer(methods)


        //    { get; set; } ... Techniques used for debugging.
        //                      Find out where unexpected assignments are made.
        //

      //public UPuzzle           pGP_Initial{ get; set; }
      //public  UPuzzleMan       GPMan{ get; set; }         // property to find out where set is executed
        public UPuzzle           pGP_Initial;
        public  UPuzzleMan       GPMan;

        public  UPuzzle          pGP{   get=>GPMan.pGP; set=>GPMan.pGP=value; }// =null;                  // Problem to analyze
        public List<UCell>       pBDL{  get=>pGP.BDL; }
        public  int              selectedIX;
        private int              stageNo{ get=>GPMan.stageNo;  }

        public  List<UPuzzle>    child_GPs => GPMan.child_GPs;
        private Random rnd = new Random();

        private List<UMthdChked> SolverLst2{ get{ return pGNP00.SolverLst2; } }  // List of algorithms to be applied.
        private List<UAlgMethod> MethodLst_Run = new List<UAlgMethod>();          // List of successful algorithms



        
        public GNPZ_Engin( GNPX_App pGNP00 ){           //Execute once at GNPX startup.
            this.GPMan = new UPuzzleMan();
            this.pGNP00 = pGNP00;
            AnMan = new GNPX_AnalyzerMan(this);
        }


      #region Puzzle management
        public void Clear_0(){
            pGP.ToInitial( ); // Clear all.
            AnMan.Update_CellsState( pGP.BDL, setAllCandidates:true );
        }
        public bool IsSolved() => pBDL.All(p=> p.No!=0);
        // Set_NewPuzzle : Set the analysis Puzzle to the engine -> pGP_Initial.        
        public void Set_NewPuzzle( UPuzzle GParg ){   
            this.pGP_Initial = GParg;

            UPuzzle pGP000 = GParg.Copy();
            this.GPMan = new UPuzzleMan( pGP000, this );                // Create the UPuzzleMan for Analize.
            GPMan.selectedIX = -1;
            this.GPMan.stageNo = 0;                                     // set stageNo=0.
            this.GPMan.method_maxDif = (MethodLst_Run.Count>0)? MethodLst_Run.First(): null;
        }

        public void Set_selectedChild( int selX ){
            int cnt = GPMan.child_GPs.Count;
            if( selX<0  || cnt<=0 || selX>=cnt ) return;
            pGP = GPMan.child_GPs[selX];
            GPMan.pGP = pGP;                            //Set the chosen GP.
            GPMan.selectedIX = selX;
        }

        public bool Set_NextStage( int _stageNo=-1 ){      // update the state and create the next stage.
            UPuzzle GPtmp = null;
            if( _stageNo == 0 )  GPMan.stageNo = 0;

            if( GPMan.stageNo == 0 ){
                GPtmp = pGP_Initial;
                AnMan.Update_CellsState( GPtmp.BDL, setAllCandidates:true );
            }
            else{
                int selectedIX = GPMan.selectedIX;
                if( selectedIX < 0 )  return false; 
                GPtmp = GPMan.child_GPs[selectedIX]; 
            }
            
            if( GPtmp.BDL.All(p=>p.No!=0) )  return false; //solved

            UPuzzleMan GPMan_next = new UPuzzleMan();

         // GPMan.GPManNxt        = GPMan_next; //202303X
            GPMan_next.GPManPre   = GPMan;
            GPMan_next.pGP        = GPtmp.Copy();
            GPMan_next.stageNo    = GPMan.stageNo+1;

                    //  GPMan_next.UPuzzleMan_stack_history(GPMan_next,"Before running PExecute_Fix_Eliminate ----");
            var (codeX,_) = AnMan.Execute_Fix_Eliminate( GPMan_next.pGP.BDL );
                // codeX  0:Complete. Go to next stage.  1:Solved.   -1:Error. Conditions are broken.
                    //  GPMan_next.UPuzzleMan_stack_history(GPMan_next,"After running PExecute_Fix_Eliminate +++++");
            if( codeX<0 ){  eng_retCode = -998; return false; }

            GPMan = GPMan_next;
            GPMan.child_GPs.Clear();
            return true;            //check_pGP(GPx,"Set_NextStage");   
        }

        public bool Restore_PreStage( ){
            if( GPMan.GPManPre is null )  return false;
            GPMan = GPMan.GPManPre;
            return true;
        }
/*  //202303X
        public bool Restore_NxtStage( ){
            if( GPMan.GPManNxt is null )  return false;
            GPMan = GPMan.GPManNxt;
            return true;
        }
*/
        public void ReturnToInitial(){
            if( pGP_Initial is null )  pGP_Initial = pGP;
            this.GPMan = new UPuzzleMan( pGP_Initial, this );
            
            this.pGP = pGP_Initial.Copy();
            this.GPMan.stageNo = 0;
            this.GPMan.pGP.ToInitial( ); //to initial stage
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
            var Q = GPMan.GPManPre;
            while( Q != null ){ GPMan=Q; Q=Q.GPManPre; /*Thread.Sleep(1);*/ }
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

                AnMan.Update_CellsState( pBDL, setAllCandidates:true );  // allFlag:true : set all candidates
                Stopwatch AnalyzerLap = new Stopwatch();
                AnalyzerLap.Start();

                Set_NewPuzzle( pGP );        

                UPuzzleMan GPManNext=null;
                while(true){
                    if( ct.IsCancellationRequested ){ ct.ThrowIfCancellationRequested(); return; }

                    if( GPMan.stageNo > 0 ){
                        var (codeX,_) = AnMan.Execute_Fix_Eliminate( pGP.BDL );
                        if( codeX<0 ){  eng_retCode=-998; return; }  //codeX=-1:Error. Conditions are broken.
                        if( codeX==1 )  break;                       //codeX=0 :Solved. 
                        // codeX=0 : Complete. Go to next stage.
                    }

                    // ================================================
                    var (ret,ret2) = sudoku_Solver_SingleStage( ct, false );  // <-- 1-step solver
                    if( !ret ){ eng_retCode=-999; break; }

                    SdkExecTime = AnalyzerLap.Elapsed;
                    if( eng_retCode<0 )  return;
                    GPMan.stageNo++;
                }
                AnalyzerLap.Stop();        

                var (_,nP,nZ,nM) = AnMan.Aggregate_CellsPZM(pBDL);
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
                GPMan.stageNo = 0;
                while(true){
                    if( ct.IsCancellationRequested ){ ct.ThrowIfCancellationRequested(); return; }

                    bool retB = Set_NextStage(GPMan.stageNo);
                    if( !retB )  return;                    // go to the next stage

                    // ================================================
                    var (ret,ret2) = sudoku_Solver_SingleStage( ct, false ); // <-- 1-step solver
                    if( !ret ){ eng_retCode=-999; break; }
                    // -------------------------------------------------

                    SdkExecTime = AnalyzerLap.Elapsed;
                    if(eng_retCode<0)  return;
                }
                AnalyzerLap.Stop();        

                var (_,nP,nZ,nM) = AnMan.Aggregate_CellsPZM(pBDL);
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
            pGP.Sol_ResultLong = "";
            var (lvlLow,lvlHgh) = Set_AcceptableLevel( );
            AnMan.Initialize_Solvers();
            if( GPMan.method_maxDif is null )  GPMan.method_maxDif = MethodLst_Run.First();

            do{
                try{
					if( pBDL.All(p=>(p.FreeB==0)) ) break; // All cells confirmed.
                    #region  Verify the solution
                    if( !AnMan.Verify_SUDOKU_Roule() ){
                        if( SolInfoB )  pGP.Sol_ResultLong = "no solution";
                        return (false,"no solution");
                    }
                    #endregion  Verify the solution
                            
                    AnalyzerLap.Start();
                    DateTime MltAnsTimer = DateTime.Now;

                    //===================================================================================
                    pGP.SolCode=-1;
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
                            GNPZ_Engin.AnalyzingMethodName  = P.MethodName;// to display the method in action
                            AnalysisResult = P.Method();            // <--- Execute
                            // *==*==*==*==*==*==*==*==*==*==*==*==*==*==*

                            if( AnalysisResult ){ // Solvrd
                                if(__ChkPrint__) WriteLine( $"========================> solved {P.MethodName}" );
                                if( P.DifLevel >= GPMan.method_maxDif.DifLevel )  GPMan.method_maxDif = P;

                                // --- analysis is successful!  save the method and difficulty.
                                if( P.DifLevel<=2 )  L1SolFound=true;
                                P.UsedCC++;                  // Counter for the number of times the algorithm has been applied
                                pGP.pMethod = P;     
         // 202303                       pGP.DifLevel = P.DifLevel;   // Set the maximum level of the algorithm to the problem level

                                if( !mltAnsSearcB )  goto LBreak_Analyzing;     // Abort if single search
                                else{
                                                                    }
                                // -------------------------------------------

                                if( (string)GNPX_App.GMthdOption["abortResult"]!="" ){
                                    AnalyzingMethodName = (string)GNPX_App.GMthdOption["abortResult"];
                                    break;
                                }
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
                        if( child_GPs.Count > 0 ){
                            pGP = child_GPs.First();
                            SDK_Ctrl.UGPMan.pGP = pGP;
                            AnalysisResult = true;
                            goto LBreak_Analyzing;
                        }
                    }
*/
                                    if(__ChkPrint__) WriteLine( "========================> can not solve");
                    if( SolInfoB )  pGP.Sol_ResultLong = "no solution";
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
                    SDK_Ctrl.solLevel_notFixedCells = notFixedCells = pBDL.Count(p => (p.FreeB!=0));
                    SDK_Ctrl.solLevel_freeDigits = freeDigits = pBDL.Aggregate(0,(t,p)=>t+p.FreeBC);
                      // WriteLine( $"### solLevel_notFixedCells:{notFixedCells}  solLevel_freeDigits:{freeDigits}" ); // for debug
                }

            }while(notFixedCells>0);

            if( notFixedCells <= 0 ){ AnalysisResult=true; }  //solved

          LBreak_Analyzing:  //found
            SdkExecTime = AnalyzerLap.Elapsed;

            GPMan.selectedIX = (child_GPs!=null && child_GPs.Count>0 )? 0: -1;
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