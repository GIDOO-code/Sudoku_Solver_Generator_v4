using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using static System.Math;
using static System.Diagnostics.Debug;

using System.Windows.Media;
using System.Threading;

using GIDOO_space;
using GIDOOCV;
using System.Globalization;
using System.Text.RegularExpressions;
//using System.Drawing;
//using System.Drawing;


namespace GNPXcore{

    // For simple version:
    //   Not modified for consistency with the regular version.
    //   Therefore, this contains unnecessary definitions and declarations.
    //

    public class GNPX_App{
        public NuPz_Win         pGNP00win;
        static public double    pixelsPerDip;
        static public bool[]    SlvMtdCList = new bool[60];
        static public bool      chbConfirmMultipleCells;
        static public Dictionary<string,object> GMthdOption;
        static public Dictionary<string,Color> ColorDic=new Dictionary<string,Color>();  
        static public DateTime  MultiSolve_StartTime = DateTime.Now;
        static public string    fNamePara;

//X        static public bool      _Loading_ = true;

        public int              cellSize = 36;
        public int              cellSizeP;
        public int              lineWidth = 1;
        public GFont            gsFont = new GFont( "Times New Romaon", 22 );
        public bool             developMode = false;
        
        public SDK_Ctrl         SDKCntrl;                       //Problem Generator
        public GNPZ_Engin       pGNPX_Eng;                      //Analysis Engine
        public UPuzzleMan       pPZLMan  => pGNPX_Eng.PZLMan;   //current problem is in pGNPX_Eng
        public UPuzzle          pPZL{ get=>pPZLMan.PZL; set=>pPZLMan.PZL=value; }  //current problem is in pGNPX_Eng

        public string           SDK_MethodsFileName = "SDK_Methods_V4.txt";

        public List<UPuzzle>    SDK_PUZZLE_List = new List<UPuzzle>();

        public int[]            SDK81;

        private int             _current_Puzzle_No;
        public int              current_Puzzle_No{
            get=>_current_Puzzle_No;
            set{ 
                int nn=value;
                if( nn == 888888888 ) nn = _current_Puzzle_No;
                if( nn == 999999999 ) nn = SDK_PUZZLE_List.Count-1;
                if( nn<0 ) nn=0;
                if( SDK_PUZZLE_List.Count>0 ){
                    if( nn>=SDK_PUZZLE_List.Count ) nn = SDK_PUZZLE_List.Count-1;
                    _current_Puzzle_No = nn;
                    pGNPX_Eng.Set_NewPuzzle( SDK_PUZZLE_List[nn] );
                }
            }
        }

        public GNPZ_Graphics    SDKGrp;             //board bitmap
  
        public string           GSmode = "tabACreate";
        public int              SelectedIndexPre = 0;
        public string           AnalyzerMode;

        public List<UAlgMethod> SolverLst1 = new List<UAlgMethod>();
        public List<UMthdChked> SolverLst2 = new List<UMthdChked>(); //valid analysis routines List
        public List<string>     LanguageLst;
   
        public PuzzleTrans      PTrans;             //Transform 


        static GNPX_App( ){
            GMthdOption = new Dictionary<string,object>();
            GMthdOption["MSlvrMaxLevel"]        = 13;
            GMthdOption["MSlvrMaxAlgorithm"]    = 50;
            GMthdOption["MSlvrMaxAllAlgorithm"] = 400;
            GMthdOption["MSlvrMaxTime"]         = 10;   //sec

            GMthdOption["StartTime"]            = DateTime.Now;
            GMthdOption["abortResult"]          = "";
        
            GMthdOption["NiceLoopMax"]          = 10;
            GMthdOption["ALSSizeMax"]           = 5;
            GMthdOption["ALSChainSizeMax"]      = 8;

            GMthdOption["Cell"]                 = true;
            GMthdOption["GroupedCells"]         = false;
            GMthdOption["ALS"]                  = false;
			GMthdOption["ForceLx"]              = "ForceL2";
			GMthdOption["ShowProofMultiPaths"]  = false;
        
            GMthdOption["GeneralLogic_on"]      = false;          
            GMthdOption["GenLogMaxSize"]        = 3;
            GMthdOption["GenLogMaxRank"]        = 1;

            GMthdOption["ForceChain_on"]        = true;   // Always ON!

            ColorDic = new Dictionary<string,Color>();
            ColorDic["Board"]        = Color.FromArgb(255,220,220,220);
            ColorDic["BoardLine"]    = Colors.Navy;

            ColorDic["CellForeNo"]   = Colors.Navy;
            ColorDic["CellBkgdPNo"]  = Color.FromArgb(255,160,160,160);
            ColorDic["CellBkgdMNo"]  = Color.FromArgb(255,190,190,200);
            ColorDic["CellBkgdZNo"]  = Colors.White;
            ColorDic["CellBkgdZNo2"] = Color.FromArgb(255,150,150,250);

            ColorDic["CellBkgdFix"]  = Colors.LightGreen;
            ColorDic["CellFixed"]    = Colors.Red;
            ColorDic["CellFixedNo"]  = Colors.Red;
        }
        public GNPX_App( NuPz_Win pGNP00win ){
            List<string> DirLst=Directory.EnumerateDirectories(".").ToList();
            LanguageLst=new List<string>();
            LanguageLst.Add("en");
            foreach( var P in DirLst ){
                var Q=P.Replace(".","").Replace("\\","");
                if(Q=="en")  continue;
                if(Q.Length==2 ||(Q[2]=='-' && Q.Length==5)) LanguageLst.Add(Q);
            }

            LanguageLst = LanguageLst.FindAll(P=>(P.Length==2 ||(P[2]=='-' && P.Length==5)));
            LanguageLst.Sort();

            this.pGNP00win = pGNP00win;
            cellSizeP  = cellSize+lineWidth;
            SDKCntrl   = new SDK_Ctrl(this,0);

            //=======================================================
            pGNPX_Eng   = new GNPZ_Engin( this );   // unique in the system
            //-------------------------------------------------------

            SDK_Ctrl.pGNPX_Eng = pGNPX_Eng;
            PTrans = new PuzzleTrans(this);
            SolverLst2 = new List<UMthdChked>();
        }
        public bool _SDK_Ctrl_Initialize(){
            AnalyzerMode = "Solve";
            UPuzzle tPZL = GetCurrentProble();
            if( tPZL is null )  return false;
            pGNPX_Eng.Set_NewPuzzle(tPZL);

            pGNPX_Eng.AnalyzerCounterReset( );
            pGNPX_Eng.AnMan.ResetAnalysisResult(true);   //Return to initial state
            pGNPX_Eng.AnMan.Update_CellsState( pGNPX_Eng.pBOARD );
            SDK_Ctrl.UGPMan=null;                       //initialize Multi_solver
			pPZL.extResult="";
            return true;
        }

    #region Puzzle Management
        public UPuzzle GetCurrentProble( ){
            UPuzzle P=null;
            if( current_Puzzle_No>=0 && current_Puzzle_No<=SDK_PUZZLE_List.Count-1 ){
                P = SDK_PUZZLE_List[current_Puzzle_No];
            }
            return P;
        }
            
        public void SDK_Save( UPuzzle aPZL ){
            aPZL.ID = SDK_PUZZLE_List.Count;
            SDK_PUZZLE_List.Add(aPZL);
        }
        public void SDK_Save_EngGP(){
            SDK_Save(pPZL);
        }   
        public void CreateNewPrb( UPuzzle aPZL=null ){
            if(aPZL==null) aPZL = new UPuzzle("New Problem");
            aPZL.ID=SDK_PUZZLE_List.Count; 
            pGNPX_Eng.Set_NewPuzzle(aPZL);
            SDK_Save(aPZL);
            current_Puzzle_No=999999999;
        }
     
        public void SDK_Save_ifNotContain(){
            if( !Contain(pPZL) )  SDK_Save_EngGP();
        }
        public void SDK_Remove(){
            int PnoMemo=current_Puzzle_No;
            if( PnoMemo==SDK_PUZZLE_List.Count-1 ) PnoMemo--;
            if( Contain(pPZL) ) SDK_PUZZLE_List.Remove(pPZL);
            int id=0;
         //   SDK_PUZZLE_List.ForEach(P=>P.ID=(id++)); //
            current_Puzzle_No=PnoMemo;
        }

        public bool Contain( UPuzzle aPZL ){
            return (SDK_PUZZLE_List.Find(P=>P.HTicks==aPZL.HTicks)!=null);
        }
     
//        public void CESetGP( UPuzzle aPZL ){
//            pGNPX_Eng.SetGP(aPZL);
//        }


    #endregion Puzzle management

    #region file I/O
        public int SDK_FileInput( string fName, bool prbIniFlag ){
            char[] sep=new Char[]{ ' ', ',', '\t' };        
            string LRecord, pName="";

            using( StreamReader SDKfile=new StreamReader(fName) ){
                SDK_PUZZLE_List.Clear();

                while( (LRecord=SDKfile.ReadLine()) !=null ){
                    if( LRecord=="" ) continue;
                    
                    // Supports the format "Contain a blank every 9 digits" and similar type.
                    int n81 = Find_81Digits(LRecord);
                    if( n81 > 81 ){
                        string st = LRecord.Substring(0,n81).Replace(" ","").Replace(".","0");
                        if( st.Length==81 && st.All(char.IsDigit) ){
                            LRecord = st + LRecord.Substring(n81);
                        }
                    }

                    // string[] eLst = LRecord.Split(sep,StringSplitOptions.RemoveEmptyEntries);
                    string[] eLst = LRecord.SplitEx(sep);

                    if( LRecord[0]=='#' ){ pName=LRecord.Substring(1); continue; }
                    
                    int nc = eLst.Length;
                    if( eLst[0] == "sPos" ) continue;   //obsolete format

                    string name="";
                    int difLvl=1;
                    if( eLst[0].Length>=81 ){
                        try{
                            if( nc>=4 && eLst[2].IsNumeric() ){       difLvl=eLst[2].ToInt(); name=eLst[3]; }
                            else if( nc>=3 && !eLst[2].IsNumeric() ){ difLvl=1;               name=eLst[2]; }
                            else{                                     difLvl=1;               name=""; }

                            string st = eLst[0].Replace(".", "0").Replace("-", "0").Replace(" ", "");
                            List<UCell> tmpBoard = _stringToBDL(st);
                            string TimeStamp =  Find_TimeStamp( eLst );
                            int ID = SDK_PUZZLE_List.Count;
                            SDK_PUZZLE_List.Add( new UPuzzle(ID,tmpBoard,name,difLvl,TimeStamp) );  
                        }
                        catch{ continue; }                   
                    }
                    else if(nc>=2){
                        try{
                            name = (eLst.Length>=2)? eLst[1]: "";
                            difLvl =  (eLst.Length>=3)? eLst[1].ToInt(): 0;
                            List<UCell> tmpBoard = new List<UCell>();
                            for(int r=0; r<9; r++ ){
                                LRecord = SDKfile.ReadLine();
                                eLst = LRecord.SplitEx(sep);
                                for( int c=0; c<9; c++ ){
                                    int n = Convert.ToInt32(eLst[c]);
                                    n = (n<0 && prbIniFlag)? 0: n;
                                    tmpBoard.Add( new UCell(r*9+c,n) );
                                }
                            }
                            int ID = SDK_PUZZLE_List.Count+1;
                            SDK_PUZZLE_List.Add( new UPuzzle(ID,tmpBoard,name,difLvl) );
                        }
                        catch{ continue; }
                    }
                }

                current_Puzzle_No = 0;
                return current_Puzzle_No;

                // ----- inner function -----
                int Find_81Digits( string st ){
                    int n=0, m=0;
                    foreach( var p in st ){
                        m++;
                        if( (char.IsDigit(p) || p=='.') && (++n)>= 81 )  break;                
                    }
                    return (n==81)? m: 0;
                }   

                string Find_TimeStamp( string[] eList ){
                    string TimeStamp="";
                    foreach( var e in eList){
                        if( DateTime.TryParse(e,out _) ){ TimeStamp=e; break; }
                    }
                    return TimeStamp;
                }
            }   
        }
/*
        public void SDK_StringInput( string st ){
            st = st.Replace(".", "0");
            List<UCell> BDLa=_stringToBDL(st);
            SDK_PUZZLE_List.Add(new UPuzzle(999999999,BDLa,"",0));
            int ID=0;
            SDK_PUZZLE_List.ForEach(P=>P.ID=(ID++));
            current_Puzzle_No=(ID-1);
        }
*/
        public UPuzzle SDK_ToUPuzzle( int[] SDK81, string name="", int difLvl=0, bool saveF=false ){
            if(SDK81==null) return null;
            string st="";
            foreach( var p in SDK81 ){ 
                if(p>9) st += "0";
                else st += (p<=9)? p.ToString(): ".";
            }
            var tmpPZL=SDK_ToUPuzzle( st, name, difLvl, saveF);
            return tmpPZL;
        }
        public UPuzzle SDK_ToUPuzzle( string st, string name="", int difLvl=0, bool saveF=false ){
            List<UCell> B=_stringToBDL(st);
            if(B==null)  return null;
            var tmpPZL=new UPuzzle(999999999,B,name,difLvl);
            if(saveF) SDK_Save(tmpPZL);  
            return tmpPZL;
        }   

        public List<UCell> _stringToBDL( string stOrg ){
            string st = stOrg.Replace(".", "0").Replace("-", "0").Replace(" ", "");
            try{               
                List<UCell> B = new List<UCell>();
                int rc=0;
                for(int k=0; rc<81; ){
                    if(st[k]=='+'){ k++; B.Add(new UCell(rc++,-( st[k++].ToInt())) ); }
                    else{
                        while(!st[k].IsNumeric()) k++;
                        B.Add( new UCell(rc++, st[k++].ToInt() ) );
                    }
                }
                return B;
            }
            catch(Exception e){
                WriteLine($"_stringToBDL \rstOrg:{stOrg} \r   st:{st}");
                WriteLine("string error:"+e.Message+"\r"+e.StackTrace);
            }
            return null;
        }
                
        public string SetSolution( UPuzzle aPZL, bool SolSet2, bool SolAll=false ){
            string solMessage="";
            pPZL = aPZL;

            string prbMessage="";
            if( SolAll || pPZL.DifLevel<=0 || pPZL.Name=="" ){
                foreach( var p in aPZL.BOARD )  if( p.No<0 ) p.No=0;

                pGNPX_Eng.AnMan.Update_CellsState( aPZL.BOARD );
                pGNPX_Eng.AnalyzerCounterReset();

                var tokSrc = new CancellationTokenSource();　        //for suspension
                pGNPX_Eng.sudoku_Solver_Simple(tokSrc.Token);                      
                if( GNPZ_Engin.eng_retCode<0 ){
                    pPZL.DifLevel = -999;
                    pPZL.Name = "unsolvable";
                }
                else{
                    if(pPZL.DifLevel<=1 || pPZL.Name==""){
                        int difficult = pGNPX_Eng.Get_DifficultyLevel( out prbMessage);
                        if(pPZL.DifLevel<=1)  pPZL.DifLevel = difficult;
                        if(pPZL.Name=="")     pPZL.Name = prbMessage;
                    }
                }
            }     
            solMessage = "";    //prbMessage;
            if(SolSet2) solMessage += pGNPX_Eng.DGViewMethodCounterToString();　//適用手法を付加
            solMessage=solMessage.Trim();

            return solMessage;
        }

        public void SDK_FileOutput( string fName, bool append, bool fType81, bool SolSort, bool SolSet, bool SolSet2, bool blank9 ){
            if( SDK_PUZZLE_List.Count==0 )  return;

            SDK_Ctrl.MltProblem = 1;
            SDK_Ctrl.lvlLow = 0;
            SDK_Ctrl.lvlHgh = 999;

            string LRecord, solMessage="";
            GNPX_App.SlvMtdCList[0] = true;  //use all methods

            var tokSrc = new CancellationTokenSource();　        //for suspension

            int m=0;
            SDK_PUZZLE_List.ForEach( p=>p.ID=(m++) );
            IEnumerable<UPuzzle> qry;
            if(SolSort) qry = from p in SDK_PUZZLE_List orderby p.DifLevel ascending select p;
            else qry = from p in SDK_PUZZLE_List select p;

            using( StreamWriter fpW=new StreamWriter(fName,append,Encoding.UTF8) ){
                foreach( var P in qry ){

                    //===== Preparation =====
                    solMessage = "";
                    if(SolSet) solMessage = SetSolution(aPZL:P,SolSet2:SolSet2,SolAll:SolSet);//output Solution UPuzzle GP, bool SolSet2, bool SolAll=false
                  
                  
                    if(fType81){　//Solution(tytpe:line)
                        LRecord = ""; 
                        P.BOARD.ForEach( q => {
                            LRecord += Max(q.No,0).ToString();
                            if( blank9 && q.c==8 )  LRecord += " ";        //Supports the format "Contain a blank every 9 digits"
                        } );
                        LRecord = LRecord.Replace("0",".");

                        LRecord += $" {(P.ID+1)} {P.DifLevel} \"{P.Name}\"";
                        if(SolSet&&SolSet2) LRecord += $" \"{SetSolution(P,SolSet2:true,SolAll:true)}\"";//解出力
                        if(pPZL.TimeStamp!=null) LRecord += $" \"{pPZL.TimeStamp}\"";
                        fpW.WriteLine(LRecord);
                    }
                    else{ //problem_name and Solution(tytpe:matrix)
                        LRecord = $"{(P.ID+1)}, {P.DifLevel}, \"{P.Name}\", {solMessage}";
                        pPZL.BOARD.ForEach( q =>{
                            if( q.c==0 ) LRecord += "\r";
                            int n = q.No;
                            if( !SolSet )  n = Abs(n);
                            LRecord += n.ToString().PadLeft(3,' ');
                        } );
                        fpW.WriteLine(LRecord);
                    }
                }
            }
            GNPX_App.SlvMtdCList[0] = false;             //restore method selection
        }
        public void btn_FavoriteFileOutput( bool fType81=true, bool SolSet=false, bool SolSet2=false ){
            if( SolSet ){
                var tokSrc = new CancellationTokenSource(); //procedures for suspension
                GNPX_App.SlvMtdCList[0] = true;             //use all methods
                pGNPX_Eng.AnMan.Update_CellsState( pGNPX_Eng.pBOARD );
                pGNPX_Eng.sudoku_Solver_Simple(tokSrc.Token);
            }

            string prbMessage;
            int difLvl = pGNPX_Eng.Get_DifficultyLevel(out prbMessage);          
            string LRecord = "";  
            if( fType81 ){
                pPZL.BOARD.ForEach( q =>{ LRecord += Max(q.No,0).ToString(); } );
                LRecord = LRecord.Replace("0",".");  

                LRecord += $" {(pPZL.ID+1)} {pPZL.DifLevel} \"{pPZL.Name}\"";
                if( SolSet&&SolSet2 ) LRecord += $" \"{SetSolution(pPZL,SolSet2:true,SolAll:true)}\"";//解を出力
                if( pPZL.TimeStamp!=null ) LRecord += $" \"{pPZL.TimeStamp}\"";
            }
            else{
                LRecord = $"{pPZL.ID+1}, {pPZL.DifLevel} \"{pPZL.Name}\", \"{prbMessage}\"";
                pPZL.BOARD.ForEach( q =>{
                    LRecord += (q.r==0)? "\r": " "; 
                    LRecord += Max(q.No,0).ToString();
                } );
            }
            
            string fNameFav = "SDK_Favorite.txt";
            using( var fpW=new StreamWriter(fNameFav,true,Encoding.UTF8) ){
                fpW.WriteLine(LRecord);
            }
            GNPX_App.SlvMtdCList[0] = false;//use selected methods
        }

        public void save_created_PUZZLE( UPuzzle aPZL ){
            aPZL.ID = SDK_PUZZLE_List.Count;
            aPZL.BOARD.ForEach( p=>p.Reset_All() );

            SDK_PUZZLE_List.Add(aPZL);
            if( SDK_Ctrl.FilePut_GenPrb){
                string fName = "AutoGen" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
                emergencyFilePut( "AutoGen", fName );
            }
            current_Puzzle_No = SDK_PUZZLE_List.Count-1;
        }
        private  void emergencyFilePut( string dirStr, string fName ){
            if( !Directory.Exists(dirStr) ){ Directory.CreateDirectory(dirStr); }
            using( var fpW=new StreamWriter(dirStr+@"\"+fName,false,Encoding.UTF8) ){     
                foreach( UPuzzle P in SDK_PUZZLE_List ){
                    string LRecord = "";
                    P.BOARD.ForEach( q =>{ LRecord += Max(q.No,0).ToString(); } );
                    LRecord=LRecord.Replace("0",".");
                    LRecord += $" {(P.ID+1)} \"{P.Name}\" {P.DifLevel} \"{P.TimeStamp}\"";
                    fpW.WriteLine(LRecord);
                }
            }   
        }
    #endregion  file I/O

    #region SuDoKu Algorithm
        public List<UMthdChked> GetMethodListFromFile( ){
            if(SolverLst1==null) new List<UAlgMethod>();
            else SolverLst1.Clear();

            WriteLine( $"########## GetMethodListFromFile ##########" );

            SolverLst1.AddRange(pGNPX_Eng.AnMan.SolverLst0);
            SolverLst1.ForEach(P=>P.IsChecked=true);


            char[] sep=new char[]{' ',','};
            string st;
            int IDx=0, intResult=0;
            bool boolResult;
            var culture = CultureInfo.CreateSpecificCulture("en-US");
            var styles = DateTimeStyles.None;
            DateTime dateResult = DateTime.Now;

            if( File.Exists(SDK_MethodsFileName) ){
                using( var fIn=new StreamReader(SDK_MethodsFileName) ){
                    while( (st=fIn.ReadLine()) !=null ){

                        if( st.Contains("abortResult") ) continue;
                        bool bChk = true;

                        if( st[0]=='*' ){
                            if( st[1] == '*' ) continue;

                            var mLst= st.Split(sep,StringSplitOptions.RemoveEmptyEntries);
                            string key = mLst[0].Substring(1);
                            string st2 = (mLst.Length>1)? mLst[1]: "";
 
                            if( st2.IsNumeric() )  GMthdOption[key] = st2.ToInt();
                            else if( int.TryParse(st2,out intResult) )  GMthdOption[key] = intResult;
                            else if( bool.TryParse(st2,out boolResult) )  GMthdOption[key] = boolResult;
                            else if( st2=="True" || st2=="False" )  GMthdOption[key] = st2=="True";
                            else if( DateTime.TryParse(st2, culture, styles, out dateResult) ){
                                GMthdOption[key] = dateResult;
                            }
                            else GMthdOption[key] = st2;
                        }
                        else{
                            if(st[0]=='-'){ bChk=false; st=st.Substring(1); }
                            UAlgMethod Q= SolverLst1.Find(x=>x.MethodName.Contains(st));
                            if(Q is UAlgMethod){ Q.ID=IDx++; Q.IsChecked=bChk; }
                        }
                    }
                }
            }
            SolverLst1.Sort( (p,q)=>(p.ID-q.ID) );
            SetMethodLis_1to2(FileOutput:false);
            return SolverLst2;
        }

        public List<UMthdChked> ResetMethodList(){
            int IDx=0;
            if(SolverLst1==null) new List<UAlgMethod>();
            else SolverLst1.Clear();

            SolverLst1.AddRange(pGNPX_Eng.AnMan.SolverLst0);
            SolverLst1.ForEach(P=> { P.IsChecked=true; P.ID=IDx++; });

            SetMethodLis_1to2(true);
            return SolverLst2;
        }    

        public List<UMthdChked> SetMethodLis_1to2( bool FileOutput ){
            if( SolverLst1==null || SolverLst1.Count==0 )  return null;
            SolverLst2 = SolverLst1.ConvertAll(Q=>new UMthdChked(Q));

            if(FileOutput)  MethodListOutPut();
            return SolverLst2;
        }


        public List<UMthdChked> ChangeMethodList( int nx, int UD ){
            UAlgMethod MA=SolverLst1[nx], MB;
            if(UD<0){ MB=SolverLst1[nx-1]; SolverLst1[nx-1]=MA; SolverLst1[nx]=MB; }   
            if(UD>0){ MB=SolverLst1[nx+1]; SolverLst1[nx+1]=MA; SolverLst1[nx]=MB; }
            SetMethodLis_1to2(FileOutput:true);
            return SolverLst2;
        }

        public void MethodListOutPut( ){
//X            if( _Loading_ || SolverLst1 == null || SolverLst1.Count<=1 ) return;
            if( SolverLst1 == null || SolverLst1.Count<=1 ) return;

            using( var fOut=new StreamWriter(SDK_MethodsFileName,append:false,encoding:Encoding.UTF8) ){
                string st="";
                SolverLst1.ForEach( P=>{ 
                    st=(P.IsChecked? "": "-")+P.MethodName.TrimStart( ' ');
                    fOut.WriteLine(st); 
                });

                foreach( var P in GNPX_App.GMthdOption.Where(p=>p.Key!="abortResult" ) ){
                    fOut.WriteLine("*"+P.Key +" "+P.Value );
                }
            }

            var Qabort = (string)GNPX_App.GMthdOption["abortResult"];
            if( Qabort != "" ){
                using( var fAbort=new StreamWriter("abortMessage.txt",append:false,encoding:Encoding.UTF8) ){
                    fAbort.WriteLine( $"\nabort Time:{DateTime.Now}\n  {Qabort}" );
                }
                GNPX_App.GMthdOption["abortResult"] = "";
            }

            bool B = (bool)GNPX_App.GMthdOption["GeneralLogic_on"];
            var Q1=SolverLst1.Find(x=>x.MethodName.Contains("GeneralLogic"));
            if( Q1 != null ) Q1.IsChecked=B;
            var Q2=SolverLst2.Find(x=>x.Name.Contains("GeneralLogic"));
            if( Q2 != null ) Q2.IsChecked=B;

            WriteLine( $"#### MethodListOutPut() ... done" +  DateTime.Now.ToString("G") );
        }
    #endregion SuDoKu Algorithm

    }

    public class UMthdChked{
        public int    ID{ get; set; }
        public string Name{ get; set; }
        public bool   IsChecked{
            get=> UAM.IsChecked;
            set=> UAM.IsChecked=value;
        }

        public UAlgMethod UAM;
        public int    __DifLevel;  //(difficulty)

        public UMthdChked( UAlgMethod P ){
            this.UAM=P; this.ID=P.ID; Name=P.MethodName; IsChecked=P.IsChecked;
        }
    }
}