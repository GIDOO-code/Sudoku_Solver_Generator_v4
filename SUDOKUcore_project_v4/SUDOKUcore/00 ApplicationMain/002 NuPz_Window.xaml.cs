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

using Microsoft.Win32;
using System.Runtime.InteropServices;

using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using System.Threading.Tasks;

using DColor = System.Drawing.Color;

using GIDOOCV;
using GIDOO_space;
    



    /* *==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*

        For simple version:

            For consistency with the regular version, there are some unnecessary but unchanged parts.
 

        How to change the version ( Regular version -> Simple version )

            (1) Remove "RegularVersion" from the conditional compilation symbol field.

            (2) Exclude the source files in the folder with "nnEx" in the name from the project.
                - There are four folders to exclude. (00Ex,24Ex,26Ex,27Ex)
                - Exclude, not delete. So that it can be restored.
                - Exclude the files in the folder, not the folder.

            (3) It can also be undone( Simple version -> Regular version ).   Do try!
                - Set "RegularVersion" to the conditional compilation symbol field.
                - Add files to the "nnEx" folders.
                - Don't forget to add both .xaml and .cs to 00Ex. 

       *==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*
    */


    /* #==#==#==#==#==#==#==#==#==#==#==#==#==#==#==#==#==#==#==#==#==#==#==#==#==#==#==#==#==#==#==#==#
        {TBD]
        Abnormal behavior of tabControl event handling.
        This is a problem to be solved.
       #--#--#--#--#--#--#--#--#--#--#--#--#--#--#--#--#--#--#--#--#--#--#--#--#--#--#--#--#--#--#--#--#
    */




namespace GNPXcore{
    using pRes=Properties.Resources;
    using sysWin=System.Windows;

    public partial class NuPz_Win: sysWin.Window{
        static public bool    eventSuspend=false;
        [DllImport("USER32.dll",CallingConvention=CallingConvention.StdCall)]
        static private extern void SetCursorPos(int X,int Y); //Move the mouse cursor to Control

        private sysWin.Point    _WinPosMemo;

    // *==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==
        public  GNPX_App        GNPX_000;

        public SDK_Ctrl         pSDKCntrl => GNPX_000.SDKCntrl;
        private GNPX_AnalyzerMan pAnMan   => pGNPX_Eng.AnMan;
        private GNPZ_Engin      pGNPX_Eng => GNPX_000.pGNPX_Eng;
        private UPuzzleMan      pPZLMan    => pGNPX_Eng.PZLMan;
        private UPuzzle         pPZL      => pPZLMan.PZL;      // current board
        private int  stageNo => (pPZLMan is null)? 0: pPZLMan.stageNo;

    // *==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==

    // ----- display-related definitions -----
        public  GNPZ_Graphics   SDKGrp; //board surface display bitmap

        private List<RadioButton> patSelLst;
        private List<RadioButton> rdbVideoCameraLst;
      //private List<Control>  _contorols;

        public  CultureInfo     culture => pRes.Culture;

        private int             WOpacityCC=0;

        // ----- timer -----
        private Stopwatch       AnalyzerLap;
        private string          AnalyzerLapElaped{
            get{
                TimeSpan ts = AnalyzerLap.Elapsed;
                string st = "";
                if( ts.TotalSeconds>1.0 ) st += ts.TotalSeconds.ToString("0.0") + " sec";
                else                      st += ts.TotalMilliseconds.ToString("0.0") + " msec";
                return st;
            }
        }
        private DispatcherTimer startingTimer;
        private DispatcherTimer endingTimer;
        private DispatcherTimer displayTimer;   
        private DispatcherTimer bruMoveTimer;
        private DispatcherTimer timerShortMessage;
        private RenderTargetBitmap bmpGZero;

    // ----- Extend -----
#if RegularVersion
        private DevelopWin      devWin;
        private ExtendResultWin ExtResultWin;
#endif

    

    #region Application start/end
        public NuPz_Win(){
            try{
                GNPX_App.pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

                GNPX_000  = new GNPX_App(this);
                SDKGrp = new GNPZ_Graphics(GNPX_000);

                InitializeComponent();     
                
                lblCurrentnDifficultyLevel.Visibility=Visibility.Hidden;
                lblShortMessage.Visibility = Visibility.Hidden;
                LblGeneralLogic.Visibility = Visibility.Hidden;
                GNPXGNPX.Content = "GNPXbv4 "+DateTime.Now.Year;
#if RegularVersion       
                LblGeneralLogic.Visibility = Visibility.Visible;
                devWin = new DevelopWin(this);
			    GroupedLinkGen.devWin = devWin;
                GNPXGNPX.Content = "GNPXv4 "+DateTime.Now.Year;
#endif
                cmbLanguageLst.ItemsSource = GNPX_000.LanguageLst;  
           
                //RadioButton Controls Collection
                var rdbLst = GNPZExtender.GetControlsCollection<RadioButton>(this);
                patSelLst  = rdbLst.FindAll(p=>p.Name.Contains("patSel"));
                rdbVideoCameraLst = rdbLst.FindAll(p=>p.Name.Contains("rdbCam"));

                 //_contorols = GNPZExtender.GetControlsCollection<Control>(this);

              #region Timer
                AnalyzerLap = new Stopwatch();

                timerShortMessage = new DispatcherTimer(DispatcherPriority.Normal);
                timerShortMessage.Interval = TimeSpan.FromMilliseconds(50);
                timerShortMessage.Tick += new EventHandler(timerShortMessage_Tick);

                startingTimer = new DispatcherTimer( DispatcherPriority.Normal, this.Dispatcher );
                startingTimer.Interval = TimeSpan.FromMilliseconds(70);
                startingTimer.Tick += new EventHandler(startingTimer_Tick);
                this.Opacity=0.0;
                startingTimer.Start();

                endingTimer = new DispatcherTimer( DispatcherPriority.Normal, this.Dispatcher );
                endingTimer.Interval = TimeSpan.FromMilliseconds(70);
                endingTimer.Tick += new EventHandler(endingTimer_Tick);

                displayTimer = new DispatcherTimer( DispatcherPriority.Normal, this.Dispatcher );
                displayTimer.Interval = TimeSpan.FromMilliseconds(100);//50
                displayTimer.Tick += new EventHandler(displayTimer_Tick);

                bruMoveTimer = new DispatcherTimer( DispatcherPriority.Normal, this.Dispatcher );
                bruMoveTimer.Interval = TimeSpan.FromMilliseconds(20);
                bruMoveTimer.Tick += new EventHandler(bruMoveTimer_Tick);
              #endregion Timer

                bmpGZero = new RenderTargetBitmap((int)PB_GBoard.Width,(int)PB_GBoard.Height, 96,96, PixelFormats.Default);
                SDKGrp.GBoardPaint( bmpGZero, (new UPuzzle()).BOARD, "tabACreate" );
                PB_GBoard.Source = bmpGZero;    //◆Initial setting

                lblProcessorCount.Content = "ProcessorCount:"+Environment.ProcessorCount;

              #region Copyright
                string endl = "\r";
                string st = "【著作権】" + endl;
                st += "本ソフトウエアと付属文書に関する著作権は、作者GNPX に帰属します。" + endl;
                st += "本ソフトウエアは著作権法及び国際著作権条約により保護されています。" + endl;
                st += "使用ユーザは本ソフトウエアに付された権利表示を除去、改変してはいけません" + endl + endl;

                st += "【配布】" + endl;
                st += "インターネット上での二次配布、紹介等は事前の承諾なしで行ってかまいません。";
                st += "バージョンアップした場合等には、情報の更新をお願いします。" + endl;
                st += "雑誌・書籍等に収録・頒布する場合には、事前に作者の承諾が必要です。" + endl + endl;
                   
                st += "【禁止事項】" + endl;
                st += "以下のことは禁止します。" + endl;
                st += "・オリジナル以外の形で、他の人に配布すること" + endl;
                st += "・第三者に対して本ソフトウエアを販売すること" + endl;
                st += "・販売を目的とした宣伝・営業・複製を行うこと" + endl;
                st += "・第三者に対して本ソフトウエアの使用権を譲渡・再承諾すること" + endl;
                st += "・本ソフトウエアに対してリバースエンジニアリングを行うこと" + endl;
                st += "・本承諾書、付属文書、本ソフトウエアの一部または全部を改変・除去すること" + endl + endl;

                st += "【免責事項】" + endl;
                st += "作者は、本ソフトウエアの使用または使用不能から生じるコンピュータの故障、情報の喪失、";
                st += "その他あらゆる直接的及び間接的被害に対して一切の責任を負いません。" + endl;
                CopyrightJP=st;

                st="===== CopyrightDisclaimer =====" + endl;
                st += "Copyright" + endl;
                st += "The copyright on this software and attached document belongs to the author GNPX" + endl;
                st += "This software is protected by copyright law and international copyright treaty." + endl;
                st += "Users should not remove or alter the rights indication attached to this software." + endl + endl;

                st += "distribution" + endl;
                st += "Secondary distribution on the Internet, introduction etc. can be done without prior consent.";
                st += "Please update the information when upgrading etc etc." + endl;
                st += "In the case of recording / distributing in magazines · books, etc., consent of the author is necessary beforehand." + endl + endl;
                   
                st += "Prohibited matter" + endl;
                st += "The following things are forbidden." + endl;
                st += "Distribute it to other people in forms other than the original." + endl;
                st += "Selling this software to a third party." + endl;
                st += "Promotion, sales and reproduction for sale." + endl;
                st += "Transfer and re-accept the right to use this software to a third party." + endl;
                st += "Modification / removal of this consent form and attached document" + endl + endl;

                st += "Disclaimer" + endl;
                st += "The author assumes no responsibility for damage to computers, loss of information or any other direct or indirect damage resulting from the use or inability of the Software." + endl;
                CopyrightEN=st;
                txtCopyrightDisclaimer.Text = CopyrightEN;
              #endregion Copyright

                tabCtrlMode.Focus();
                PB_GBoard.Focus();                 
            }
            catch(Exception e){
                Debug.WriteLine(e.Message+"\r"+e.StackTrace);
            }
        }
        private void Window_Loaded( object sender, RoutedEventArgs e ){
            _Display_GB_GBoard( );       //board setting
            _SetBitmap_PB_pattern();     //Pattern setting

            eventSuspend = true;
                Lbl_onAnalyzing.Content    = "";
                Lbl_onAnalyzingM.Content   = "";
                Lbl_onAnalyzingTS.Content  = "";
                Lbl_onAnalyzingTSM.Content = "";
            
                //===== solution list setting =====           
                GMethod00A.ItemsSource = GNPX_000.GetMethodListFromFile();
                GMethod00A.IsEnabled = true;

                MSlvrMaxLevel.Value        = (int)GNPX_App.GMthdOption["MSlvrMaxLevel"];
                MSlvrMaxAlgorithm.Value    = (int)GNPX_App.GMthdOption["MSlvrMaxAlgorithm"];
                MSlvrMaxAllAlgorithm.Value = (int)GNPX_App.GMthdOption["MSlvrMaxAllAlgorithm"];
                MSlvrMaxTime.Value         = (int)GNPX_App.GMthdOption["MSlvrMaxTime"];

                NiceLoopMax.Value         = (int)GNPX_App.GMthdOption["NiceLoopMax"];
                ALSSizeMax.Value          = (int)GNPX_App.GMthdOption["ALSSizeMax"];
                ALSChainSizeMax.Value     = (int)GNPX_App.GMthdOption["ALSChainSizeMax"];

                method_NLCell.IsChecked   = (bool)GNPX_App.GMthdOption["Cell"];
                method_NLGCells.IsChecked = (bool)GNPX_App.GMthdOption["GroupedCells"];
                method_NLALS.IsChecked    = (bool)GNPX_App.GMthdOption["ALS"];

                int Solver_MaxLevel = pAnMan.SolverLst0.Max( p=>p.DifLevel );
                MSlvrMaxLevel.MaxValue = Solver_MaxLevel;
                MSlvrMaxLevel.ToolTip = $"max:{Solver_MaxLevel}";

			    string st=(string)GNPX_App.GMthdOption["ForceLx"];
			    switch(st){
				    case "ForceL1": ForceL1.IsChecked=true; break;
				    case "ForceL2": ForceL2.IsChecked=true; break;
				    case "ForceL3": ForceL3.IsChecked=true; break;
			    }
                ShowProofMultiPaths.IsChecked  = (bool)GNPX_App.GMthdOption["ShowProofMultiPaths"];

            eventSuspend = false;
            //*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*
        #if !DEBUG
            GNPX_App.GMthdOption["GeneralLogic_on"] = false;   //GeneralLogic Off
        #endif
            //*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*

            GeneralLogicOnChbx.IsChecked = (bool)GNPX_App.GMthdOption["GeneralLogic_on"];
            int GLMaxSize = (int)GNPX_App.GMthdOption["GenLogMaxSize"];
            if( GLMaxSize>0 && GLMaxSize<=(int)GenLogMaxSize.MaxValue ) GenLogMaxSize.Value=GLMaxSize;

            int GLMaxRank = (int)GNPX_App.GMthdOption["GenLogMaxRank"];
            if( GLMaxRank>=0 && GLMaxRank<=(int)GenLogMaxRank.MaxValue ) GenLogMaxRank.Value=GLMaxRank;

            WOpacityCC = 0;
            startingTimer.Start();

            _WinPosMemo = new sysWin.Point(this.Left,this.Top+this.Height);
            
            { //Move the mouse cursor to Button:btnOpenPuzzleFile                 
                var btnQ=btnOpenPuzzleFile;                    
                var ptM=new Point(btnQ.Margin.Left+btnQ.Width/2,btnQ.Margin.Top+btnQ.Height/2);//Center coordinates
                var pt = grdFileIO.PointToScreen(ptM);  //Grid relative coordinates to screen coordinates.
                SetCursorPos((int)pt.X,(int)pt.Y);      //Move the mouse cursor
            }

//X            GNPX_App._Loading_ = false;
        }
/*
        private void eventSuppression( bool onOff ){
            var controls = GNPZExtender.GetControlsCollection<Control>(this);
            controls.ForEach( P => {
                if( P is CheckBox )  P. 
            } );           
        }
*/
        private void Window_Unloaded( object sender, RoutedEventArgs e ){
            Environment.Exit(0);
        }
    #endregion Application start/end 

        string CopyrightJP, CopyrightEN;
        private void  MultiLangage_JP_Click(Object sender,RoutedEventArgs e){
            ResourceService.Current.ChangeCulture("ja-JP");
            txtCopyrightDisclaimer.Text = CopyrightJP;
            _MethodSelectionMan();
            __bruMoveSub();
        }
        private void  btnMultiLangage_Click(object sender, RoutedEventArgs e){
            ResourceService.Current.ChangeCulture("en");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            txtCopyrightDisclaimer.Text = CopyrightEN;
            _MethodSelectionMan();
            __bruMoveSub();
        }
        private void  cmbLanguageLst_SelectionChanged(Object sender,SelectionChangedEventArgs e){
            string lng=(string)cmbLanguageLst.SelectedValue;
            ResourceService.Current.ChangeCulture(lng);
            __bruMoveSub();
        }


        public object Culture{ get=> pRes.Culture; }

        private void  GNPXGNPX_MouseDoubleClick( object sender, MouseButtonEventArgs e ){
#if RegularVersion
            if(devWin==null) devWin=new DevelopWin(this);
            devWin.Show();
            devWin.Set_dev_GBoard(pPZL.BOARD);
#endif
        }


    #region Start/end Timer    
        private void appExit_Click( object sender, RoutedEventArgs e ){
            GNPX_000.MethodListOutPut();

            WOpacityCC=0;
            endingTimer.IsEnabled = true;
            endingTimer.Start();
        }
        private void startingTimer_Tick( object sender, EventArgs e){
            WOpacityCC++;
            if( WOpacityCC >= 25 ){ this.Opacity=1.0; startingTimer.Stop(); }
            else this.Opacity=WOpacityCC/25.0;
        }
        private void endingTimer_Tick( object sender, EventArgs e){
            if( (++WOpacityCC)>10 )  Environment.Exit(0);   //Application.Exit();
            double dt = 1.0-WOpacityCC/12.0;
            this.Opacity = dt*dt;
        }
        
        private void __bruMoveSub(){ 
            Thickness X=PB_GBoard.Margin;
            PB_GBoard.Margin=new Thickness(X.Left+2,X.Top+2,X.Right,X.Bottom);
            bruMoveTimer.Start();
        }      
        private void bruMoveTimer_Tick( object sender, EventArgs e){
            Thickness X=PB_GBoard.Margin;   //◆
            PB_GBoard.Margin=new Thickness(X.Left-2,X.Top-2,X.Right,X.Bottom);
            bruMoveTimer.Stop();
        }       
    #endregion TimerEvent

    #region Location
		private void GNPXwin_LocationChanged( object sender,EventArgs e ){ //Synchronously move open window
            foreach( sysWin.Window w in Application.Current.Windows )  __GNPXwin_LocationChanged(w);
            _WinPosMemo = new sysWin.Point(this.Left,this.Top);
		}
		private void __GNPXwin_LocationChanged( sysWin.Window w ){
            if( w==null || w.Owner==this || w==this )  return;
            w.Left = this.Left-_WinPosMemo.X+w.Left;
            w.Top  = this.Top -_WinPosMemo.Y+w.Top ;
            w.Topmost=true;
        }	
    #endregion Location

        private void Window_MouseDown( object sender, MouseButtonEventArgs e ){
            if(e.Inner(PB_GBoard))    return; //◆
            if(e.Inner(tabCtrlMode))  return;
            this.DragMove();
        }
        
        private void btnHomePageGitHub_Click( object sender, RoutedEventArgs e ){
            string cul=Thread.CurrentThread.CurrentCulture.Name;
            Debug.WriteLine("The current culture is {0}", cul);
            string urlHP="";
            if(cul=="ja-JP") urlHP = "https://gidoo-code.github.io/Sudoku_Solver_Generator_v4_jp/";
            else             urlHP = "https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/"; 
          //Process.Start(urlHP);        //? in development
            HP_address.Text = urlHP;
            Clipboard.SetData(DataFormats.Text, urlHP);
            CopiedHP.Visibility = Visibility.Visible;
            GNPZExtender.ProcessExe(urlHP);
        }  
   
    #region ShortMessage
        public void shortMessage( string st, sysWin.Point pt, Color cr, int tm ){
            lblShortMessage.Content = st;
            lblShortMessage.Foreground = new SolidColorBrush(Colors.White);
            lblShortMessage.Background = new SolidColorBrush(cr);
            lblShortMessage.Margin = new Thickness(pt.X,pt.Y,0,0);

            if( tm==9999 ) timerShortMessage.Interval = TimeSpan.FromSeconds(5);
            else           timerShortMessage.Interval = TimeSpan.FromMilliseconds(tm);            
            timerShortMessage.Start();
            lblShortMessage.Visibility = Visibility.Visible;
        }
        private void timerShortMessage_Tick( object sender, EventArgs e ){
            lblShortMessage.Visibility = Visibility.Hidden;
            timerShortMessage.Stop();
        }
    #endregion ShortMessage

    #region　operation mode
        private void tabCtrlMode_SelectionChanged( object sender, SelectionChangedEventArgs e ){
            if( (TabControl)sender!=tabCtrlMode ) return;
            CopiedHP.Visibility = Visibility.Hidden;
            Clipboard.Clear();
            HP_address.Text = "";
            TabItem tb=tabCtrlMode.SelectedItem as TabItem;
            if(tb==null)  return;
            if( tb.Name.Substring(0,4)!="tabA" )  return;
            GNPX_000.GSmode = (string)tb.Name;    //Tab Name -> State mode
                //WriteLine( $"tabCtrlMode_SelectionChanged TabItem:{GNPX_000.GSmode}" );

            switch(GNPX_000.GSmode){
                case "tabASolve": sNoAssist = (bool)chbShowCandidate.IsChecked; break;
                case "tabACreate":
                    TabItem tb2=tabAutoManual.SelectedItem as TabItem;
                    if( tb2==null )  return ;
                    if( (string)tb2.Name=="tabBAuto" )  sNoAssist=false;
                    else sNoAssist = (bool)chbShowNoUsedDigits.IsChecked;
                    gridExhaustiveSearch.Visibility=
                        (int.Parse(GenLStyp.Text)==2)? Visibility.Visible: Visibility.Hidden;
                    break;

                case "tabAOption":
                    bool sAssist=true;
                    chbSetAnswer.IsChecked=sAssist;
                    sNoAssist=sAssist;
                    break;
        
                default: sNoAssist=false; break;
            }

            _Display_GB_GBoard();   
            //tabSolver_SelectionChanged(sender,e);
        }      
        private void _MethodSelectionMan(){
            GMethod00A.ForceCursor=true;          

            if(GNPX_000!=null){
                GMethod00A.ItemsSource = null; //GSel*
                GMethod00A.ItemsSource = GNPX_000.SetMethodLis_1to2(true);
                GMethod00A.IsEnabled = true;
            }

            string st;;
            bool B = (bool)GNPX_App.GMthdOption["GeneralLogic_on"];
            string cul=Thread.CurrentThread.CurrentCulture.Name;
            string st2;
            if(cul=="ja-JP") st2= B? "有効": "無効";
            else             st2= (B? "":"not ") + "available";
            LblGeneralLogic.Content = "GeneralLogic :"+ st2;
            LblGeneralLogic.Foreground = (B)? Brushes.LightBlue: Brushes.Yellow;

            B = (bool)GNPX_App.GMthdOption["ForceChain_on"];
            if(cul == "ja-JP") st2 = B ? "有効" : "無効";
            else st2 = (B? "" : "not ") + "available";
        }


        private string preTabName="";

        private void tabSolver_SelectionChanged( object sender, SelectionChangedEventArgs e ){
            TabItem tabItm = tabSolver.SelectedItem as TabItem;
            if( tabItm!=null ){
                SDK_Ctrl.MltAnsSearch = (tabItm.Name=="tabBMultiSolve");  
                
                if( preTabName=="tabBMethod" )  GNPX_000.MethodListOutPut(); //*****
                preTabName = tabItm.Name;
            }
        }
        #endregion operation mode


    }
}