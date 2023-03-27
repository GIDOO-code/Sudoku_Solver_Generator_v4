using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Globalization;

using static System.Math;
using static System.Diagnostics.Debug;

using System.Windows;
using System.Windows.Controls;
//using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

using Microsoft.Win32;

using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using System.Threading.Tasks;

using GIDOOCV;

using GIDOO_space;
using System.Windows.Documents;
using System.Reflection;

namespace GNPXcore{
    using pRes=Properties.Resources;
    using sysWin=System.Windows;

    public partial class NuPz_Win{

    #region analysis
      //[Note] task,ProgressChanged,Completed,Canceled threadSafe（Prohibition of control operation）  
    #region analysis[Step] 
        private int  OnWork = 0;
        private bool ErrorStopB;
        private int  _objectKeyMemo=0;
//        private GNPZ_Engin  pGNPX_Eng => SDK_Ctrl.pGNPX_Eng;    //GNPX_000.pGNPX_Eng;





        private void SetScreen_PuzzleSolved(){
            lblAnalyzerResult.Text  = pPZLMan.PZL.Sol_ResultLong;
            lblAnalyzerResultM.Text = pPZLMan.PZL.Sol_ResultLong;

            txbStepCC.Text  = stageNo.ToString();
            txbStepMCC.Text = stageNo.ToString();

            if( !SDK_Ctrl.MltAnsSearch ){  // ----------------- single
                 _Set_DGViewMethodCounter();
            }
            else{                           // ---------------- multi
                __MultiSolve_ButtonSet();
            }
        }


        private void __MultiSolve_ButtonSet(){
            if( pPZLMan is null ){
                btnMTop.IsEnabled = false;
                btnMPre.IsEnabled = false;
            }
            else{
                btnMTop.IsEnabled = true;
                btnMPre.IsEnabled = (pPZLMan.PZLManPre!=null);
             // btnMNxt.IsEnabled = (pGPMan.GPManNxt!=null);  //202303X
            }
        }

        private void __task_SDKsolver_Completed(){
            __DispMode = "Complated";
            taskSDK = null;
            GNPZ_Engin.SolverBusy = false;
                  //  pGPMan.UPuzzleMan_stack_history( pGPMan, msg:"Solved, not executed." );           
            displayTimer.Start();  //  Conflict-free display start
        }

        private void _Set_DGViewMethodCounter(){  // Aggregation of methods used
            List<_MethodCounter> methodCounters;

            var _MethodCounter = new Dictionary<string,int>();
            var Q = pGNPX_Eng.PZLMan;

            if( false ){  //---- for debug ... solved.
                bool checkKey = (Q._objectKey == _objectKeyMemo);
                  WriteLine( $"##### {Q} _objectKeyMemo:{_objectKeyMemo} Q._objectKey:{Q._objectKey} {(checkKey? "EQ": "NG")}" );
                if( checkKey )  return;                     // Omit if same UPuzzleMan-object.
                _objectKeyMemo = Q._objectKey;
            }

            if( Q.stageNo == 0 ){ DGViewMethodCounter.ItemsSource=null; return; }            

            while( Q!=null && Q.PZL!=null && Q.stageNo>0 ){
                if( Q.PZL.pMethod!=null ){
                    string keyString = Q.PZL.pMethod.MethodKey;     //"ID"+MethodName
                    if( !_MethodCounter.ContainsKey(keyString) )  _MethodCounter[keyString] = 0;
                    _MethodCounter[keyString] += 1;
                }
                Q = Q.PZLManPre;
            }

            if( _MethodCounter.Count>0 ){
                methodCounters = new List<_MethodCounter>();
                foreach( var q in _MethodCounter )  methodCounters.Add( new _MethodCounter(q.Key, q.Value) );

                methodCounters.Sort( (a,b) => (a.ID-b.ID) );

                DGViewMethodCounter.ItemsSource = methodCounters;
                if( methodCounters.Count>0)  DGViewMethodCounter.SelectedIndex = -1;

                if(GNPX_000.GSmode=="tabASolve" && methodCounters.Count>0 && DGViewMethodCounter.Columns.Count>1){
                    Style style = new Style(typeof(DataGridCell));
                    style.Setters.Add( new Setter(DataGrid.HorizontalAlignmentProperty, HorizontalAlignment.Right) );
                    DGViewMethodCounter.Columns[1].CellStyle = style;
                    DGViewMethodCounter.Columns[2].CellStyle = style;
                }
            }   
        }
        
        private class _MethodCounter{
            public int    ID; 
            public string methodName{ get; set; }

            public string difficulty{  get; set; }
            public string count{ get; set; }
            public _MethodCounter( string nm, int cc ){
                ID = nm.Substring(0,7).ToInt();
                methodName = " "+nm.Substring(9);//.PadRight(30);
                difficulty = nm.Substring(7,2)+"  ";//.PadRight(30);
                count      = cc.ToString()+"  ";
            }

            public _MethodCounter( (string,int) Q ){
                methodName = " "+Q.Item1;//.PadRight(30);
                count = Q.Item2.ToString()+" ";
            }
        }






        private void btnSolve_Click( object sender, RoutedEventArgs e ){
            if( OnWork==2 )   return;
            if( GNPZ_Engin.SolverBusy )  return;

            if( pGNPX_Eng.IsSolved() ){ lblAnalyzerResult.Text = "\r solved."; }
            else{


                GNPX_000.AnalyzerMode = "Solve";
                GNPZ_Engin.SolverBusy = true;

                pGNPX_Eng.Set_NextStage( );
                SuDoKuSolver(); 
            }
            GNPZ_Engin.SolverBusy = false;
        }


        private void btnMPre_Click( object sender, RoutedEventArgs e ){
            GNPX_000.AnalyzerMode = "StepBack";
            if( !pGNPX_Eng.Restore_PreStage( ) ){
                LstBxMltSolutions.ItemsSource = null;
                return;  //not run with stage=0
            }

            __MultiSolve_ButtonSet();
            __task_SDKsolver_Completed();
            
            displayTimer.Start();           //  Conflict-free display start
        }

/* //202303X
        private void btnMNxt_Click( object sender, RoutedEventArgs e ){
            if( !pGNPX_Eng.Restore_NxtStage( ) ){
                LstBxMltSolutions.ItemsSource = null;
                return;  //not run with stage=0
            }

            __MultiSolve_ButtonSet();
            __task_SDKsolver_Completed();
            
            displayTimer.Start();           //  Conflict-free display start
        }
*/        



        private void btnMultiSolve_Click( object sender, RoutedEventArgs e ){
#if !DEBUG
            bool GL = (bool)GNPX_App.GMthdOption["GeneralLogic_on"];
            if( GL ){
                shortMessage("GeneralLogic is unenable.", new sysWin.Point(750,60), Colors.Red,3000);
                return;
            }
#endif 
            if( pGNPX_Eng.IsSolved() ){ lblAnalyzerResult.Text = "\r solved."; return;}

            //-----------------------------------------------------------------------------
            lblAnalyzerResultM.Text="";
            GNPX_000.AnalyzerMode = "MultiSolve";
            __MultiSolve_ButtonSet();    
            GNPZ_Engin.SolverBusy = true;
            SDK_Ctrl.Clear();

            { //---- Solve up -----
                GNPX_App.GMthdOption["MSlvrMaxLevel"]        = (int)MSlvrMaxLevel.Value;
                GNPX_App.GMthdOption["MSlvrMaxAlgorithm"]    = (int)MSlvrMaxAlgorithm.Value;
                GNPX_App.GMthdOption["MSlvrMaxAllAlgorithm"] = (int)MSlvrMaxAllAlgorithm.Value;
                GNPX_App.GMthdOption["MSlvrMaxTime"]         = (int)MSlvrMaxTime.Value;
                GNPX_App.GMthdOption["abortResult"]          = "";
                GNPX_App.MultiSolve_StartTime                = DateTime.Now;

                pGNPX_Eng.Set_NextStage( );

                SuDoKuSolver(); 
            }
        }
            


        private void SuDoKuSolver(){        // 202303-beta
            try{
                Lbl_onAnalyzing.Foreground = Brushes.LightGreen;
                Lbl_onAnalyzingM.Foreground  = Brushes.LightGreen;

                if( (string)btnSolve.Content != pRes.msgSuspend){
                 //   int mc=pGNPX_Eng.Set_Methods_for_Solving( );  //already setted
                 //   if(mc<=0) GNPX_000.ResetMethodList();
                    Lbl_onAnalyzing.Visibility = Visibility.Visible;
                    Lbl_onAnalyzingM.Visibility = Visibility.Visible;


                    if( GNPX_000.AnalyzerMode=="Solve" || GNPX_000.AnalyzerMode=="MultiSolve" ){

                        if( pPZL.SolCode<0 )  pPZL.SolCode=0;
                        ErrorStopB = !Set_CellsTruth_sub();

                        if( (pPZL.BOARD).Count(p=>p.No==0) == 0 ){ //analysis completed
                            _SetScreenProblem();
                            goto AnalyzerEnd;
                        }

                    #region Prepare
                        {
                        OnWork = 1;
                            txbStepCC.Text   = stageNo.ToString();
                            btnSolve.Content = pRes.msgSuspend;
                            Lbl_onAnalyzing.Content= pRes.Lbl_onAnalyzing;

                            txbStepMCC.Text  = txbStepCC.Text;
                            btnMultiSolve.Content= btnSolve.Content;
                            Lbl_onAnalyzingM.Content = Lbl_onAnalyzing.Content;

                            Lbl_onAnalyzing.Foreground=Brushes.Orange;
                            Lbl_onAnalyzingM.Foreground=Brushes.Orange;
                            Lbl_onAnalyzingTS.Content = "";
                            Lbl_onAnalyzingTSM.Content = "";
                            this.Cursor = Cursors.Wait;


                            if( GNPX_000.SDKCntrl.retNZ==0 )  GNPX_000.SDKCntrl.LoopCC=0;

                            SDK_Ctrl.MltProblem = 1;    //single
                            SDK_Ctrl.lvlLow = 0;
                            SDK_Ctrl.lvlHgh = 999;
                            SDK_Ctrl.NumRandmize = false;
                          //GNPX_000.SDKCntrl.CbxDspNumRandmize=false;

                            GNPX_000.SDKCntrl.GenLStyp = 1;

                            GNPX_App.chbConfirmMultipleCells = (bool)chbConfirmMultipleCells.IsChecked;
                            GNPZ_Engin.SolInfoB = true;
                            AnalyzerLap.Reset();
                        }
                    #endregion

                        //==============================================================
                        {//  Solve the problem (solver_task start)
                            if( !ErrorStopB ){
                                __DispMode="";                
                                AnalyzerLap.Start();
                                //==============================================================
                                tokSrc = new CancellationTokenSource();　//for Cancellation 
                                taskSDK = new Task( ()=> GNPX_000.SDKCntrl.Analyzer_Real(tokSrc.Token), tokSrc.Token );
                                taskSDK.ContinueWith( t=> __task_SDKsolver_Completed() ); //procedures used on completion
                                taskSDK.Start();
                            }
                            else{
                                __DispMode="Complated"; 
                            }
                        }
                        //--------------------------------------------------------------


                        //if( GNPX_000.AnalyzerMode!="MultiSolve" ) displayTimer.Start(); // <- To avoid unresolved operation trouble.
                        displayTimer.Start();           //  Conflict-free display start
                        //--------------------------------------------------------------         
                    }
                    else{
                        try{
                            tokSrc.Cancel();
                            taskSDK.Wait();
                            btnSolve.Content=pRes.btnSolve;
                        }
                        catch(AggregateException e2){ 
                            WriteLine($"{e2.Message}");
                            __DispMode="Canceled";
                        }
                    }
 
                AnalyzerEnd:
                    return;
                }
            }
            catch( Exception ex ){ WriteLine( $"{ex.Message}\r{ex.StackTrace}" ); }
        } 

        private void task_SDKsolver_ProgressChanged( object sender, SDKEventArgs e ){ _ProgressPer=e.ePara1; }

      #endregion  analysis[Step] 
    
    #region analysis[All] 
        private void task_SDKsolverAuto_ProgressChanged( object sender, ProgressChangedEventArgs e ){
            lblNoOfTrials.Content = pRes.lblNoOfTrials + GNPX_000.SDKCntrl.LoopCC;
            txbBasicPattern.Text  = GNPX_000.SDKCntrl.PatternCC.ToString();
            btnSolveUp.Content = pRes.btnSolveUp;
            OnWork=0;
            GNPZ_Engin.SolverBusy=false;
        }
        private void task_SDKsolverAuto_Completed( ){ 
            __DispMode = "Complated";
            displayTimer.Start();
        }

        private void btnSolveUp_Click( object sender, RoutedEventArgs e ){
            if( OnWork==1 ) return;
            GNPX_000.AnalyzerMode = "SolveUp";
            pGNPX_Eng.MethodLst_Run_Reset();

            // Suspend
            if( (string)btnSolveUp.Content==pRes.msgSuspend ){  
                tokSrc.Cancel();
                try{ taskSDK.Wait(); }
                catch(AggregateException){ __DispMode="Canceled"; }
                displayTimer.Start();
                OnWork = 0;
                return;
            }

            // Full analysis 
            else{
                pAnMan.Update_CellsState( pPZL.BOARD, setAllCandidates:true );  // allFlag:true : set all candidates

                // Complate (All cells are confirmed.)
                if( pPZL.BOARD.All(p=> p.No!=0) ){
                    _SetScreenProblem(); 
                    goto AnalyzerEnd;
                }
                
                // No Solution
                if( pPZL.BOARD.Any(p=>(p.No==0 && p.FreeB==0)) ){
                    lblAnalyzerResult.Text = pRes.msgNoSolution;
                    goto AnalyzerEnd;
                }

                // Preparation
                {   
                    OnWork = 2;
                    btnSolveUp.Content = null;
                    btnSolveUp.Content = pRes.msgSuspend;          
                    Lbl_onAnalyzing.Content  = pRes.Lbl_onAnalyzing;
                    Lbl_onAnalyzing.Foreground = Brushes.Orange;               
              
                    _ResetAnalyzer(true); // Clear Analysis Result
                    pGNPX_Eng.AnalyzerCounterReset(); 

                    GNPZ_Engin.SolInfoB = true;
                    SDK_Ctrl.lvlLow = 0;
                    SDK_Ctrl.lvlHgh = 999;
                    this.Cursor = Cursors.Wait;

                    displayTimer.Start();
                }


                // solver_task start
                {
                    tokSrc = new CancellationTokenSource();
                    CancellationToken ct = tokSrc.Token;   
                    taskSDK = new Task( ()=> GNPX_000.SDKCntrl.Analyzer_RealAuto(ct), ct );
                    taskSDK.ContinueWith( t=> task_SDKsolverAuto_Completed() );
                    AnalyzerLap.Reset(); 
                    taskSDK.Start();
                }

                AnalyzerLap.Start();
                __DispMode="";     
                
              AnalyzerEnd:
                displayTimer.Start();                
                return;
            }
        }

        private void btnAnalyzerResetAll_Click( object sender, RoutedEventArgs e ){
            __bruMoveSub();
          //GNPX_000.current_Puzzle_No = 888888888; // _current_Puzzle_No;
            pGNPX_Eng.MethodLst_Run_Reset();

            _ResetAnalyzer(true);
            __MultiSolve_ButtonSet();
            pGNPX_Eng.ReturnToInitial();
        }

        private void _ResetAnalyzer( bool AllF=true ){
            if(OnWork>0) return;
                       
            pGNPX_Eng.Clear_0();

                txbStepCC.Text  = stageNo.ToString();
                txbStepMCC.Text = stageNo.ToString();
            btnSolveUp.Content  = pRes.btnSolveUp;
                lblAnalyzerResult.Text    = "";
                Lbl_onAnalyzing.Content   = "";         
                Lbl_onAnalyzingTS.Content = "";

                lblAnalyzerResultM.Text   = "";
                LstBxMltSolutions.ItemsSource   = null;

            btnMultiSolve.IsEnabled   = true;
            txbEpapsedTimeTS3.Text    = "";

            DGViewMethodCounter.ItemsSource = null;     // ###
            txbStepCC.Text = "0";
            txbStepMCC.Text = "0";

            pAnMan.ResetAnalysisResult(AllF);
            pGNPX_Eng.AnalyzerCounterReset();
            pGNPX_Eng.PZLMan.PZL.pMethod = null;
#if RegularVersion
            GeneralLogicGen.GLtrialCC=0;
#endif
            displayTimer.Stop();
                _SetScreenProblem();
        }
      #endregion analysis[All] 




      #region MultiAnalysis        
        static public  int[,]        Sol99sta=new int[9,9];


        private void LstBxMltSolutions_SelectionChanged(object sender,SelectionChangedEventArgs e){
            if( GNPZ_Engin.SolverBusy )  return;
            if( pPZLMan is null  )   return;
            
            try{
                LstBxMltSolutions.SelectionChanged -= new SelectionChangedEventHandler( LstBxMltSolutions_SelectionChanged );

                var Q = (UProbS)LstBxMltSolutions.SelectedItem;
                if( Q is null )  return;
                lblAnalyzerResultM.Text= $"[{Q.__ID+1}] {Q.Sol_ResultLong}"; 

                int selX = LstBxMltSolutions.SelectedIndex;
                if( selX>=0 )  pGNPX_Eng.Set_selectedChild(selX);
            }
            catch(Exception e2){ WriteLine($"{e2.Message}\r{e2.StackTrace}"); }
            finally{
                LstBxMltSolutions.SelectionChanged += new SelectionChangedEventHandler( LstBxMltSolutions_SelectionChanged );
            }
        }
    

      #endregion MultiAnalysis    



      #region analysis[Method aggregation]
     //   public int  AnalyzerCC=0;     
        private int AnalyzerCCMemo=0;
        private int AnalyzerMMemo=0;   
        private bool Set_CellsTruth_sub(  ){   //Cell true/false setting processing     //20230220　確認が必要
            if( pPZL.SolCode<0 ) return false;
            var (codeX,eNChk) = pAnMan.Execute_Fix_Eliminate( pPZL.BOARD );
            // codeX = 0 : Complete. Go to next stage.
            //         1 : Solved. 
            //        -1 : Error. Conditions are broken.

            if( codeX==-1 && pPZL.SolCode==-9119 ){
                string st="";
                for(int h=0; h<27; h++ ){
                    if(eNChk[h]!=0x1FF){
                        st+= "Candidate #"+(eNChk[h]^0x1ff).ToBitStringNZ(9)+" disappeared in "+_ToHouseName(h)+"\r";
                        pAnMan.SetBG_OnError(h);
                    }
                }

                lblAnalyzerResult.Text=st;
                pPZL.SolCode = pPZL.SolCode;
                return false;
            }


            if( pPZL.SolCode==-999 ){
                lblAnalyzerResult.Text = "Method control error";
                pPZL.SolCode = -1;
            }

            var (_,nP,nZ,nM) = __Set_CellsPZMCount( );
            if(nZ==0){ pAnMan.SolCode=0; return true; }
            if(nM!=AnalyzerMMemo ){
                AnalyzerCCMemo = stageNo;
                AnalyzerMMemo  = nM;
            }

            if(nZ==0 && (bool)chbSetDifficulty.IsChecked){
                string prbMessage;
                int DifLevel = pGNPX_Eng.Get_DifficultyLevel( out prbMessage );
                pPZL.DifLevel = DifLevel;
                nUDDifficultyLevel.Text = DifLevel.ToString();
                lblCurrentnDifficultyLevel.Content = $"Difficulty: {pPZL.pMethod.DifLevel}"; //CurrentLevel
                if(lblAnalyzerResult.Text!="") lblCurrentnDifficultyLevel.Visibility=Visibility.Visible;
                else                           lblCurrentnDifficultyLevel.Visibility=Visibility.Hidden;
            }
            return true;

             string _ToHouseName( int h ){
                string st="";
                switch(h/9){
                    case 0: st="row";    break;
                    case 1: st="Column"; break;
                    case 2: st="block";  break;
                }
                st += ((h%9)+1).ToString();
                return st;
             }
        }


      #endregion analysis[Method aggregation]

    #endregion analysis

    #region Method selection
        private void btnDefaultSettings_Click( object sender, RoutedEventArgs e ){
            int nx=GMethod00A.SelectedIndex;  //****
            GMethod00A.ItemsSource = null; //GSel*
            GMethod00A.ItemsSource = GNPX_000.ResetMethodList();
            GMethod00A.IsEnabled = true;
            GMethod00A.SelectedIndex = nx;    //****
            GNPX_000.MethodListOutPut();
            pGNPX_Eng.Set_Methods_for_Solving(false);
            GeneralLogicOnChbx.IsChecked = (bool)GNPX_App.GMthdOption["GeneralLogic_on"];
        }

        private void GMethod01U_Click( object sender, RoutedEventArgs e ){
            int nx = GMethod00A.SelectedIndex;
            if( nx<=3 || nx==0 )  return;
            GMethod00A.ItemsSource = null; //GSel*
            GMethod00A.ItemsSource = GNPX_000.ChangeMethodList(nx,-1);
            GMethod00A.IsEnabled = true;
            GMethod00A.SelectedIndex = nx-1;
            GNPX_000.MethodListOutPut();
            pGNPX_Eng.Set_Methods_for_Solving(false);
        }
        private void GMethod01D_Click( object sender, RoutedEventArgs e ){
            int nx = GMethod00A.SelectedIndex;
            if( nx<0 || nx==GMethod00A.Items.Count-1 )  return;
        //    GMethod00A.ItemsSource = null; //GSel*
            GMethod00A.ItemsSource = GNPX_000.ChangeMethodList(nx,1);
            GMethod00A.SelectedIndex = nx+1;
            GNPX_000.MethodListOutPut();
            pGNPX_Eng.Set_Methods_for_Solving(false);
        }

        private void btnMethodCheck_Click(Object sender,RoutedEventArgs e){
            bool B=((Button)sender==btnMethodCheck);
            GNPX_000.SolverLst1.ForEach(P=>P.IsChecked=B);

            GNPX_000.SolverLst1.Find( x => x.MethodName.Contains("LastDigit") ).IsChecked=true;
            GNPX_000.SolverLst1.Find( x => x.MethodName.Contains("NakedSingle") ).IsChecked=true;
            GNPX_000.SolverLst1.Find( x => x.MethodName.Contains("HiddenSingle") ).IsChecked=true;

            UAlgMethod Q = GNPX_000.SolverLst1.Find(x=>x.MethodName.Contains("GeneralLogic"));
            if( Q != null ) Q.IsChecked = (bool)GNPX_App.GMthdOption["GeneralLogic_on"];

            _MethodSelectionMan();
        }

        private void ALSSizeMax_NumUDValueChanged(Object sender,GIDOOEventArgs args){
            if( eventSuspend )  return;
            if( ALSSizeMax==null )  return;
            GNPX_App.GMthdOption["ALSSizeMax"] = ALSSizeMax.Value;
            _MethodSelectionMan();
        }
        private void ALSChainSizeMax_NumUDValueChanged(Object sender,GIDOOEventArgs args){
            if( eventSuspend )  return;
            if( ALSSizeMax==null )  return;
            GNPX_App.GMthdOption["ALSChainSizeMax"] = ALSSizeMax.Value;
            _MethodSelectionMan();
        }
        private void NiceLoopMax_NumUDValueChanged(Object sender,GIDOOEventArgs args){
            if( eventSuspend )  return;
            if( NiceLoopMax==null )  return;
            GNPX_App.GMthdOption["NiceLoopMax"] = NiceLoopMax.Value;
            _MethodSelectionMan();
        }

        private void GdNiceLoop_CGA_Checked(Object sender,RoutedEventArgs e){
            if( eventSuspend )  return;
            if(method_NLCell==null || method_NLGCells==null || method_NLALS==null)  return;
            GNPX_App.GMthdOption["Cell"]           = (bool)method_NLCell.IsChecked;
            GNPX_App.GMthdOption["GroupedCells"]   = (bool)method_NLGCells.IsChecked;
            GNPX_App.GMthdOption["ALS"]            = (bool)method_NLALS.IsChecked;
            _MethodSelectionMan();
        }
   		private void ForceL1L2L3_Checked( object sender,RoutedEventArgs e ){
            if( eventSuspend )  return;
            if( ForceL1==null || ForceL2==null )  return;
			GNPX_App.GMthdOption["ForceLx"] = ((RadioButton)sender).Name;
            _MethodSelectionMan();
		}
        private void ShowProofMultiPaths_Checked(object sender, RoutedEventArgs e) {
            if( eventSuspend )  return;
            if( ShowProofMultiPaths==null )  return;
			GNPX_App.GMthdOption["ShowProofMultiPaths"] = (bool)ShowProofMultiPaths.IsChecked;
            _MethodSelectionMan();
        }
        private void GeneralLogicOnChbx_Checked(Object sender,RoutedEventArgs e){
            if( eventSuspend )  return;
            if( GeneralLogicOnChbx==null )  return;
            GNPX_App.GMthdOption["GeneralLogic_on"] = (bool)GeneralLogicOnChbx.IsChecked;
            _MethodSelectionMan();
        }
        private void GenLogMaxSize_NumUDValueChanged(Object sender,GIDOOEventArgs args){
            if( eventSuspend )  return;
            if( GenLogMaxSize==null )  return;
            GNPX_App.GMthdOption["GenLogMaxSize"] = GenLogMaxSize.Value;
            _MethodSelectionMan();
        }

        private void GenLogMaxRank_NumUDValueChanged(Object sender,GIDOOEventArgs args){
            if( eventSuspend )  return;
            if( GenLogMaxRank==null )  return;
            GNPX_App.GMthdOption["GenLogMaxRank"] = GenLogMaxRank.Value;
            _MethodSelectionMan();
        }


        private void MSlvrMaxLevel_NumUDValueChanged(object sender, GIDOOEventArgs args) {
            if( eventSuspend )  return;
            GNPX_App.GMthdOption["MSlvrMaxLevel"] = MSlvrMaxLevel.Value;
        }

        private void MSlvrMaxAlgorithm_NumUDValueChanged(object sender, GIDOOEventArgs args) {
            if( eventSuspend )  return;
            GNPX_App.GMthdOption["MSlvrMaxAlgorithm"] = MSlvrMaxAlgorithm.Value;
        }

        private void MSlvrMaxAllAlgorithm_NumUDValueChanged(object sender, GIDOOEventArgs args) {
            if( eventSuspend )  return;
            GNPX_App.GMthdOption["MSlvrMaxAllAlgorithm"] = MSlvrMaxAllAlgorithm.Value;
        }

        private void MSlvrMaxTime_NumUDValueChanged(object sender, GIDOOEventArgs args) {
            if( eventSuspend )  return;
            GNPX_App.GMthdOption["MSlvrMaxTime"] = MSlvrMaxTime.Value;
        }

        #endregion Method selection
    }
}