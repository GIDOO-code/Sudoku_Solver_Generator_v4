using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Globalization;

using static System.Math;
using static System.Diagnostics.Debug;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Text;

/*
using Microsoft.Win32;

using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using System.Threading.Tasks;

//using OpenCvSharp;
//using OpenCvSharp.Extensions;
*/
using GIDOOCV;

using GIDOO_space;

namespace GNPXcore{
    using pRes=Properties.Resources;
    using sysWin=System.Windows;

    public partial class NuPz_Win{
    #region display
        private bool sNoAssist=false;
        private void _Display_GB_GBoard( UPuzzle aPZL=null, bool DevelopB=false ){
            if( GNPX_000.AnalyzerMode=="MultiSolve" && __DispMode!="Complated" )  return;
            try{
                UPuzzle tmpPZL = aPZL?? pPZL;
                if(tmpPZL==null) return;

                Lbl_onAnalyzing.Visibility = (GNPX_000.GSmode=="tabASolve")? Visibility.Visible: Visibility.Hidden; 
                Lbl_onAnalyzingM.Visibility = Visibility.Visible; 
      
                SDKGrp.GBoardPaint(bmpGZero, tmpPZL.BOARD, GNPX_000.GSmode, sNoAssist);

                PB_GBoard.Source = bmpGZero;    //◆tmpPZL.BOARD set
                gridOverlap.Visibility =  GNPZ_Graphics.overlaped? Visibility.Visible: Visibility.Hidden;

                __Set_CellsPZMCount();
                txtProbNo.Text = (tmpPZL.ID+1).ToString();
                txtProbName.Text = tmpPZL.Name;
                nUDDifficultyLevel.Text = tmpPZL.DifLevel.ToString();
    //The following code "pMethod" is rewritten to another thread. 
    //This may cause an access violation.
    //here Try with try{...} catch(Exception){...}. 
                int DiffL = (pPZL.pMethod==null)? 0: pPZL.pMethod.DifLevel; //
                lblCurrentnDifficultyLevel.Content = $"Difficulty: {DiffL}"; //CurrentLevel

                if(DevelopB) _Display_Develop();
#if RegularVersion
			    if(GNPX_000.GSmode=="tabASolve")  _Display_ExtResultWin();	
#endif
            }
            catch(Exception e){
                WriteLine( $"{e.Message}\r{e.StackTrace}" );
#if DEBUG
                using(var fpW=new StreamWriter("Exception_002e.txt",true,Encoding.UTF8)){
                    fpW.WriteLine($"---{DateTime.Now} {e.Message} \r{e.StackTrace}");
                }
#endif
            }
        }

        private void _Display_Develop(){
            int[] TrPara=pPTrans.TrPara;
            LblRg.Content    = TrPara[0].ToString();      
            LblR123g.Content = TrPara[1].ToString();
            LblR456g.Content = TrPara[2].ToString();
            LblR789g.Content = TrPara[3].ToString();

            LblCg.Content    = TrPara[4].ToString();
            LblC123g.Content = TrPara[5].ToString();
            LblC456g.Content = TrPara[6].ToString();
            LblC789g.Content = TrPara[7].ToString(); 
            LblRC7g.Content  = TrPara[8].ToString();
        }
#if RegularVersion
		private void _Display_ExtResultWin(){
            if( pPZL.extResult==null || pPZL.extResult.Length<2 ){
				if(ExtResultWin!=null && ExtResultWin.Visibility==Visibility.Visible ){
				    ExtResultWin.Close();    
                    ExtResultWin = null;   
				}
				return;
			}
            else{        	
              //WriteLine( $"_Display_ExtResultWin {DateTime.Now} {pPZL.extResult}" );    
                if( ExtResultWin != null ){ ExtResultWin.Close(); ExtResultWin=null; }
                ExtResultWin = new ExtendResultWin(this);
                ExtResultWin.Visibility = Visibility.Visible;
				ExtResultWin.Width = this.Width;
				ExtResultWin.Left  = this.Left;
				ExtResultWin.Top   = this.Top+this.Height;

                ExtResultWin.Show();
			    ExtResultWin.SetText(pPZL.extResult);
            }
		}	
#endif
        private void chbSetAnswer_Checked( object sender, RoutedEventArgs e ){
            sNoAssist = (bool)chbSetAnswer.IsChecked;
            var (nPZMb,_,_,_) = __Set_CellsPZMCount();
            if( nPZMb ) _Display_GB_GBoard( );
        }

        private (bool,int,int,int) __Set_CellsPZMCount( ){
            if( txbPuzzle==null || GNPX_000.pGNPX_Eng==null )  return (false,-1,-1,-1);
            var (confirmedAll,nP,nZ,nM) = pAnMan.Aggregate_CellsPZM( pPZL.BOARD );

            if( nP+nZ+nM>0 ){
                txbPuzzle.Text   = nP.ToString();
                txbSolved.Text   = nM.ToString();
                txbUnsolved.Text = nZ.ToString();
                if(nP>0)  txbUnsolved.Background = (nZ==0)? Brushes.Navy: Brushes.Black;

                if( GNPX_000.GSmode=="tabACreate" ){
                    int solLvl = Min( SDK_Ctrl.solLevel_notFixedCells, 81-nP);
                    lblSolutionLevel.Content = $"Solvable Probability: {solLvl}/{81-nP}";
                    lblSolutionLevel.Visibility = Visibility.Visible;
                }
                else{ lblSolutionLevel.Visibility=Visibility.Hidden; }
            }
            return ((nP+nM>0)&confirmedAll,nP,nZ,nM);
        }

        private void chbAnalyze00_Checked( object sender, RoutedEventArgs e ){
            if( bmpGZero==null )  return;
            sNoAssist = (bool)chbShowCandidate.IsChecked;//chbShowNoUsedDigits
            _SetScreenProblem();
        }
        private void chbAssist01_Checked( object sender, RoutedEventArgs e ){
            sNoAssist = (bool)chbShowNoUsedDigits.IsChecked;
            _Display_GB_GBoard();　//(Show free numbers)
        }

        private int    __GCCounter__=0;
        private int    _ProgressPer;
        private string __DispMode=null;

        private void displayTimer_Tick( object sender, EventArgs e ){
            _Display_GB_GBoard();   //******************
            
            UPuzzle tmpPZL=null;
            if(GNPX_000.GSmode=="DigRecogCmp" || GNPX_000.GSmode=="DigRecogCancel"){
                if(GNPX_000.SDK81!=null){
                    GNPX_000.current_Puzzle_No=999999999;
                    tmpPZL=GNPX_000.SDK_ToUPuzzle(GNPX_000.SDK81,saveF:true);
                    GNPX_000.current_Puzzle_No=tmpPZL.ID;     //20180731
                }
                displayTimer.Stop();

                _SetScreenProblem();
                GNPX_000.GSmode = "tabACreate";
            }

            switch(GNPX_000.GSmode){
                case "DigRecog":
                case "DigRecogTry":
                case "tabACreate": _Display_CreateProblem(); break;

                case "tabBMultiSolve":
                case "tabASolve":  _Display_AnalyzeProb(); break;
            }    

            lblResourceMemory.Content = "Memory: " + GC.GetTotalMemory(true).ToString("N0");            
            if( ((++__GCCounter__)%1000)==0 ){ GC.Collect(); __GCCounter__=0; }
        }

        private UPuzzle CreateDigitToUProblem(int[] SDK81){
            string st="";
            for(int rc=0; rc<81; rc++ ){
                int nn=SDK81[rc];
                if(nn>9) nn=0;
                st += st.ToString();
            }
            UPuzzle tmpPZL=GNPX_000.SDK_ToUPuzzle(st,saveF:true); 
            return tmpPZL;
        }
        private RenderTargetBitmap bmpPD = new RenderTargetBitmap(176,176, 96,96, PixelFormats.Default);//176=18*9+2*4+1*6        
        private void _Display_CreateProblem(){
            txbNoOfTrials.Text    = GNPX_000.SDKCntrl.LoopCC.ToString();
            txbNoOfTrialsCum.Text = SDK_Ctrl.TLoopCC.ToString();
            txbBasicPattern.Text  = GNPX_000.SDKCntrl.PatternCC.ToString();
            int n = gamGen05.Text.ToInt();
            lblNoOfProblems1.Content = (n-_ProgressPer).ToString();

            if(pPZL!=null){
                int nn=GNPX_000.SDK_PUZZLE_List.Count;
                if(nn>0){
                    txtProbNo.Text = nn.ToString();
                    txtProbName.Text = GNPX_000.SDK_PUZZLE_List.Last().Name;
                    nUDDifficultyLevel.Text = pPZL.DifLevel.ToString();
                }
            }

            string st = AnalyzerLapElaped;
            Lbl_onAnalyzingTS.Content  = st;
            Lbl_onAnalyzingTSM.Content = st;
            txbEpapsedTimeTS3.Text    = st;

            if(__DispMode!=null && __DispMode!=""){
                _SetScreenProblem();
                displayTimer.Stop();
                AnalyzerLap.Stop();
                btnCreateProblemMlt.Content = pRes.btnCreateProblemMlt;
            }
            __DispMode="";

            if((bool)chbCreateProblemEx2.IsChecked){
                SDKGrp.GBPatternDigit( bmpPD, Sol99sta );
            }
            else bmpPD.Clear();
            PB_BasePatDig.Source=bmpPD; 
        }

        private void _Display_AnalyzeProb(){
            string  AnalyzingMethodName =  GNPZ_Engin.AnalyzingMethod.MethodName;
            if( (string)GNPX_App.GMthdOption["abortResult"]!="" ){
                AnalyzingMethodName = (string)GNPX_App.GMthdOption["abortResult"];
            }

            //WriteLine("----------------"+__DispMode);
            if( __DispMode=="Canceled" ){
                shortMessage("cancellation accepted",new sysWin.Point(120,188),Colors.Red,3000);
                Lbl_onAnalyzing.Foreground = Brushes.LightCoral; 
                Lbl_onAnalyzingM.Foreground  = Brushes.LightCoral;                
                LstBxMltSolutions.IsManipulationEnabled = true;     
                displayTimer.Stop();
            }
            
            else if( __DispMode=="Complated" ){
                LstBxMltSolutions.IsManipulationEnabled = true;

                Lbl_onAnalyzing.Content = pRes.msgAnalysisComplate;
                if( (string)GNPX_App.GMthdOption["abortResult"]!="" ){
                    Lbl_onAnalyzingM.Content = GNPX_App.GMthdOption["abortResult"];
                }
                else{
                    Lbl_onAnalyzingM.Content = pRes.msgAnalysisComplate;
                    Lbl_onAnalyzingM.Foreground = Brushes.LightBlue;  

					if( (bool)chbSetDifficulty.IsChecked &&
                        (GNPX_000.AnalyzerMode=="Solve" || GNPX_000.AnalyzerMode=="SolveUp") ){
						string prbMessage;
						int DifLevel = GNPX_000.pGNPX_Eng.Get_DifficultyLevel( out prbMessage );
						pPZL.DifLevel = DifLevel;
					}
                }
                btnSolve.Content        = pRes.btnSolve;
                btnMultiSolve.Content   = pRes.btnMultiSolve;
                btnMultiSolve.IsEnabled = true;
                Lbl_onAnalyzing.Foreground = Brushes.LightBlue;   
 
                SetScreen_PuzzleSolved();

                string msgST = pPZL.Sol_ResultLong;

                if(!ErrorStopB) lblAnalyzerResult.Text = msgST;
                if( msgST.LastIndexOf("anti-rule")>=0 || msgST.LastIndexOf("Unparsable")>=0 ){ }

                var tmtmpPZLMan = pGNPX_Eng.PZLMan;
                int mpCC=0;
                List<UProbS> USolLst2 = tmtmpPZLMan.child_PZLs.ConvertAll( G => new UProbS(G,++mpCC) );
                if( USolLst2!=null && USolLst2.Count>0 ){
                    LstBxMltSolutions.ItemsSource = USolLst2;
                    LstBxMltSolutions.SelectedIndex = tmtmpPZLMan.selectedIX;
                    LstBxMltSolutions.ScrollIntoView( USolLst2.First() );

                    var Q = (UProbS)LstBxMltSolutions.SelectedItem;
                    if( Q != null )  lblAnalyzerResultM.Text= $"[{Q.__ID+1}] {Q.Sol_ResultLong}";
                }
                else{ 
                    LstBxMltSolutions.ItemsSource = null;
                }
 
                displayTimer.Stop();
            }

        //    else if( GNPX_000.AnalyzerMode == "StepBack" ){   ...
        //        Lbl_onAnalyzingM.Content = "";
        //        Lbl_onAnalyzing.Content  = "";
        //    }

            else{
                if(!ErrorStopB)  lblAnalyzerResult.Text = AnalyzingMethodName;
                Lbl_onAnalyzingM.Content = pRes.Lbl_onAnalyzing+" : " + AnalyzingMethodName;
                Lbl_onAnalyzing.Content = pRes.Lbl_onAnalyzing+" : "  + AnalyzingMethodName;
                LstBxMltSolutions.IsManipulationEnabled = false;
            }        

            string st=AnalyzerLapElaped;
            Lbl_onAnalyzingTS.Content   = st;
            Lbl_onAnalyzingTSM.Content  = st;
            txbEpapsedTimeTS3.Text     = st;
                        
            btnSolveUp.Content         = pRes.btnSolveUp;

            if( AnalyzingMethodName.Contains("sys") ){
                lblAnalyzerResultM.Text = AnalyzingMethodName;
            }

            this.Cursor = Cursors.Arrow;
            if( __DispMode=="Complated" ) _SetScreenProblem();
 
            OnWork = 0;
//            __DispMode="";
        }
 
        private void btnCopyBitMap_Click( object sender, RoutedEventArgs e ){
            try{
                var bmf = _CreateBitmapImage();
                Clipboard.SetData(DataFormats.Bitmap,bmf);
            }
            catch(System.Runtime.InteropServices.COMException){ /* NOP */ }
            //( clipboard COMException http://shen7113.blog.fc2.com/blog-entry-28.html )
        }
        private void btnSaveBitMap_Click( object sender, RoutedEventArgs e ){
            BitmapEncoder enc = new PngBitmapEncoder(); // JpegBitmapEncoder(); BmpBitmapEncoder();
          //BitmapFrame bmf = BitmapFrame.Create(bmpGZero); //#####
            var bmp = _CreateBitmapImage();
            var bmf = BitmapFrame.Create(bmp);
            enc.Frames.Add(bmf);
            try {
                Clipboard.SetData(DataFormats.Bitmap,bmf);
            }
            catch(System.Runtime.InteropServices.COMException){ /* NOP */ }

            if( !Directory.Exists(pRes.fldSuDoKuImages) ){ Directory.CreateDirectory(pRes.fldSuDoKuImages); }
            string fName = DateTime.Now.ToString("yyyyMMdd HHmmss")+".png";
            using( Stream stream = File.Create(pRes.fldSuDoKuImages+"/"+fName) ){
                enc.Save(stream);
            }    
            bitmapPath.Text = "Path : "+Path.GetFullPath( pRes.fldSuDoKuImages+"/"+fName);
        }

        private RenderTargetBitmap _CreateBitmapImage(){
            bool sWhiteBack = (bool)chbWhiteBack.IsChecked;
            if(!sWhiteBack) return bmpGZero;
            var bmpGZeroW = new RenderTargetBitmap((int)PB_GBoard.Width,(int)PB_GBoard.Height, 96,96, PixelFormats.Default);
            SDKGrp.GBoardPaint( bmpGZeroW, pPZL.BOARD, "tabACreate", whiteBack:true );
            return bmpGZeroW;
        }
    #endregion display
    }
}