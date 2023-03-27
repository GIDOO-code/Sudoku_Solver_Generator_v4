using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Media;
using System.Threading;
using static System.Diagnostics.Debug;
using static System.Math;
using System.Security.Cryptography;
using System.Windows.Interop;
using System.IO;
using System.Text;

namespace GNPXcore{
    using pRes=Properties.Resources;
    public delegate bool dSolver();


    // *==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*
    public class UAlgMethod{
        static private int ID0=0;
        public int        ID;               // System default order.
        public string     MethodName;
        public string     MethodKey => ID.ToString().PadLeft(7) +DifLevel.ToString().PadLeft(2) +MethodName;

        // For algorithms with negative levels, there are simpler conjugate algorithms.
        // If you just solve Sudoku, you don't need it. For example, the 5D-LockedSet is conjugate with the 4D-LockedSet.
        public int        DifLevel;         // Level of difficulty
        public dSolver    Method;
        public bool       GenLogB;          // "GeneralLogic"
        public int        UsedCC=0;         // Counter applied to solve one problem.
        public bool       IsChecked=true;   // Algorithm valid

        public UAlgMethod( ){ }
        public UAlgMethod( int pid, string MethodName, int DifLevel, dSolver Method, bool GenLogB=false ){

            this.ID         = pid*100+(ID0++); //System default order.
            this.MethodName = MethodName;
            this.DifLevel   = DifLevel;     //Level of difficulty
            this.Method     = Method;
            this.GenLogB    = GenLogB;
        }
        public override string ToString(){
            string st=MethodName.PadRight(30)+"["+DifLevel+"]"+" "+UsedCC;
            st += "GeneralLogic:"+GenLogB.ToString();
            return st;
        }
    }



    // *==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*
    // Analyzer Manager
    public class GNPX_AnalyzerMan{
        private Bit81[]     pConnectedCells{ get{ return AnalyzerBaseV2.ConnectedCells; } }
      //  static public event SDKSolutionEventHandler Send_Solved;  20230307
        static private Color _Black_=Colors.Black;


        public GNPZ_Engin    pGNPX_Eng;


        public bool          SolInfoB{ get=>GNPZ_Engin.SolInfoB; }
        public UPuzzleMan    pPZLMan => pGNPX_Eng.PZLMan;
        private int          stageNo => pPZLMan.stageNo;     


        public UPuzzle       pPZL => pPZLMan.PZL;
        public List<UCell>   pBOARD=> pPZL.BOARD;
        public bool          improper{ get=>pPZL.improper; set=>pPZL.improper=value; } 




        public int          SolCode{ set =>pPZL.SolCode=value; }
        public string       Result{ set=>pPZL.Sol_Result=value; }
        public string       ResultLong{ set=>pPZL.Sol_ResultLong=value; }


        public bool         chbConfirmMultipleCells{ get=>GNPX_App.chbConfirmMultipleCells; }



        public TimeSpan     SdkExecTime;
#if RegularVersion
		public SuperLinkMan SprLKsMan;
#endif
        public List<UAlgMethod>  SolverLst0;
        private int[,]     Sol99sta{ get=> NuPz_Win.Sol99sta; } //int[,]

        public  GNPX_AnalyzerMan( ){ }

        public  GNPX_AnalyzerMan( GNPZ_Engin pGNPX_Eng ){
            SolverLst0 = new List<UAlgMethod>();
            this.pGNPX_Eng = pGNPX_Eng;
#if RegularVersion
			SprLKsMan=new SuperLinkMan(this);
#endif
          //================================================================================================
          //  UAlgMethod( int pid, string MethodName, int DifLevel, dSolver Method, bool GenLogB=false )
          //================================================================================================

#if Research_
            var TryApp = new Research_trial(this);
            SolverLst0.Add( new UAlgMethod( 1, "Research_Trial",    1, TryApp.TrialAndErrorApp ) );
#endif



            var SSingle=new SimpleSingleGen(this);
            SolverLst0.Add( new UAlgMethod( 1, "LastDigit",    1, SSingle.LastDigit ) );
            SolverLst0.Add( new UAlgMethod( 2, "NakedSingle",  1, SSingle.NakedSingle ) );
            SolverLst0.Add( new UAlgMethod( 3, "HiddenSingle", 1, SSingle.HiddenSingle ) );

#if RegularVersion
          //var GLTech=new GeneralLogicGen(this);
          //SolverLst0.Add( new UAlgMethod( 5, " GeneralLogic",  2, GLTech.GeneralLogic, true) );
            var GLTech2=new GeneralLogicGen2(this);
            SolverLst0.Add( new UAlgMethod( 4, " GeneralLogicEx",  2, GLTech2.GeneralLogic2, true) );
#endif

            var LockedCand=new LockedCandidateGen(this);
            SolverLst0.Add( new UAlgMethod( 5, "LockedCandidate", 2, LockedCand.LockedCandidate ) );
        //  SolverLst0.Add( new UAlgMethod( 5, "LockedCandidate_old", 2, LockedCand.LockedCandidate_old ) );
            
            var LockedSet=new LockedSetGen(this);
            SolverLst0.Add( new UAlgMethod( 10, "LockedSet(2D)",        3, LockedSet.LockedSet2 ) );
            SolverLst0.Add( new UAlgMethod( 12, "LockedSet(3D)",        4, LockedSet.LockedSet3 ) );
            SolverLst0.Add( new UAlgMethod( 14, "LockedSet(4D)",        5, LockedSet.LockedSet4 ) );
            SolverLst0.Add( new UAlgMethod( 16, "LockedSet(5D)",       -6, LockedSet.LockedSet5 ) );//complementary to 4D
            SolverLst0.Add( new UAlgMethod( 18, "LockedSet(6D)",       -6, LockedSet.LockedSet6 ) );//complementary to 3D
            SolverLst0.Add( new UAlgMethod( 20, "LockedSet(7D)",       -6, LockedSet.LockedSet7 ) );//complementary to 2D           
            SolverLst0.Add( new UAlgMethod( 11, "LockedSet(2D)Hidden",  3, LockedSet.LockedSet2Hidden ) );           
            SolverLst0.Add( new UAlgMethod( 13, "LockedSet(3D)Hidden",  4, LockedSet.LockedSet3Hidden ) );          
            SolverLst0.Add( new UAlgMethod( 15, "LockedSet(4D)Hidden",  5, LockedSet.LockedSet4Hidden ) );
            SolverLst0.Add( new UAlgMethod( 17, "LockedSet(5D)Hidden", -6, LockedSet.LockedSet5Hidden ) );//complementary to 4D
            SolverLst0.Add( new UAlgMethod( 19, "LockedSet(6D)Hidden", -6, LockedSet.LockedSet6Hidden ) );//complementary to 3D        
            SolverLst0.Add( new UAlgMethod( 21, "LockedSet(7D)Hidden", -6, LockedSet.LockedSet7Hidden ) );//complementary to 2D

            var Fish=new FishGen(this);
            SolverLst0.Add( new UAlgMethod( 30, "XWing",            4, Fish.XWing ) );
            SolverLst0.Add( new UAlgMethod( 31, "SwordFish",        5, Fish.SwordFish ) );
            SolverLst0.Add( new UAlgMethod( 32, "JellyFish",        6, Fish.JellyFish ) );
            SolverLst0.Add( new UAlgMethod( 33, "Squirmbag",       -6, Fish.Squirmbag ) );//complementary to 4D 
            SolverLst0.Add( new UAlgMethod( 34, "Whale",           -6, Fish.Whale ) );    //complementary to 3D 
            SolverLst0.Add( new UAlgMethod( 35, "Leviathan",       -6, Fish.Leviathan ) );//complementary to 2D 

            SolverLst0.Add( new UAlgMethod( 40, "Finned XWing",     5, Fish.FinnedXWing ) );
            SolverLst0.Add( new UAlgMethod( 41, "Finned SwordFish", 6, Fish.FinnedSwordFish ) );
            SolverLst0.Add( new UAlgMethod( 42, "Finned JellyFish", 6, Fish.FinnedJellyFish ) );
            SolverLst0.Add( new UAlgMethod( 43, "Finned Squirmbag", 7, Fish.FinnedSquirmbag ) );//not complementary with fin
            SolverLst0.Add( new UAlgMethod( 44, "Finned Whale",     7, Fish.FinnedWhale ) );    //not complementary with fin
            SolverLst0.Add( new UAlgMethod( 45, "Finned Leviathan", 7, Fish.FinnedLeviathan ) );//not complementary with fin
#if RegularVersion
            SolverLst0.Add( new UAlgMethod( 90, "Franken/MutantFish",         8, Fish.FrankenMutantFish ) );
            SolverLst0.Add( new UAlgMethod( 91, "Finned Franken/Mutant Fish", 8, Fish.FinnedFrankenMutantFish ) );

            SolverLst0.Add( new UAlgMethod( 100, "EndoFinned F/M Fish",       11, Fish.EndoFinnedFMFish ) );
            SolverLst0.Add( new UAlgMethod( 100, "Finned EndoFinned F/M Fish",11, Fish.FinnedEndoFinnedFMFish ) );

            SolverLst0.Add( new UAlgMethod( 101, "Cannibalistic F/M Fish",      11, Fish.CannibalisticFMFish ) );
            SolverLst0.Add( new UAlgMethod( 101, "FinnedCannibalistic F/M Fish",11, Fish.FinnedCannibalisticFMFish ) );
#endif

#if RegularVersion
            var nxgCellLink=new NXGCellLinkGen(this);
            SolverLst0.Add( new UAlgMethod( 50, "Skyscraper",       5, nxgCellLink.Skyscraper ) );
            SolverLst0.Add( new UAlgMethod( 51, "EmptyRectangle",   5, nxgCellLink.EmptyRectangle ) );
            SolverLst0.Add( new UAlgMethod( 52, "XY-Wing",          6, nxgCellLink.XYwing ) );
            SolverLst0.Add( new UAlgMethod( 53, "W-Wing",           7, nxgCellLink.Wwing ) );

            SolverLst0.Add( new UAlgMethod( 55, "RemotePair",       6, nxgCellLink.RemotePair ) );    
            SolverLst0.Add( new UAlgMethod( 56, "XChain",           7, nxgCellLink.XChain ) );
            SolverLst0.Add( new UAlgMethod( 57, "XYChain",          7, nxgCellLink.XYChain ) ); 
           
            SolverLst0.Add( new UAlgMethod( 60, "Color-Trap",       6, nxgCellLink.Color_Trap ) );
            SolverLst0.Add( new UAlgMethod( 61, "Color-Wrap",       6, nxgCellLink.Color_Wrap ) );
            SolverLst0.Add( new UAlgMethod( 62, "MultiColor-Type1", 7, nxgCellLink.MultiColor_Type1 ) );
            SolverLst0.Add( new UAlgMethod( 63, "MultiColor-Type2", 7, nxgCellLink.MultiColor_Type2 ) );

            var ALSTechP=new AALSTechGen(this);
            SolverLst0.Add( new UAlgMethod( 59, "SueDeCoq",         6, ALSTechP.SueDeCoq ) );         

          //var SimpleXYZ=new SimpleUVWXYZwingGen(this);        // -----------> Replaced with ALS version
          //SolverLst0.Add( new UAlgMethod( 70, "XYZ-Wing",         6, SimpleXYZ.XYZwing ) );
          //SolverLst0.Add( new UAlgMethod( 71, "WXYZ-Wing",        6, SimpleXYZ.WXYZwing ) );
          //SolverLst0.Add( new UAlgMethod( 72, "VWXYZ-Wing",       7, SimpleXYZ.VWXYZwing ) );
          //SolverLst0.Add( new UAlgMethod( 73, "UVWXYZ-Wing",      7, SimpleXYZ.UVWXYZwing ) );

            var ALSTech=new ALSTechGen(this);                   // ALS version
            SolverLst0.Add( new UAlgMethod( 74, "XYZ-WingALS",         8, ALSTech.XYZwingALS ) );
            SolverLst0.Add( new UAlgMethod( 75, "ALS_Wing",            8, ALSTech.ALS_Wing ) );
            SolverLst0.Add( new UAlgMethod( 80, "ALS-XZ",              8, ALSTech.ALS_XZ ) );
            SolverLst0.Add( new UAlgMethod( 81, "ALS-XY-Wing",         9, ALSTech.ALS_XY_Wing ) );
            SolverLst0.Add( new UAlgMethod( 82, "ALS-Chain",           10, ALSTech.ALS_Chain ) );
            SolverLst0.Add( new UAlgMethod( 83, "ALS-DeathBlossom",    10, ALSTech.ALS_DeathBlossom ) );
            SolverLst0.Add( new UAlgMethod( 83, "ALS-DeathBlossomEx",  10, ALSTech.ALS_DeathBlossomEx ) );

            var NLTech=new NiceLoopGen(this);
            SolverLst0.Add( new UAlgMethod(  95, "NiceLoop",           11, NLTech.NiceLoop ) );

            var GNLTech=new GroupedLinkGen(this);
	      //SolverLst0.Add( new UAlgMethod(  96, "GroupedNiceLoop",    12, GNLTech.GroupedNiceLoop ) );    //Suspension of development 
            SolverLst0.Add( new UAlgMethod(  97, "GroupedNiceLoopEx",  12, GNLTech.GroupedNiceLoopEx ) );  //Updated to Radiation Search
          //SolverLst0.Add( new UAlgMethod( 103, "Kraken Fish",        12, GNLTech.KrakenFish ) );         //Suspension of development 
          //SolverLst0.Add( new UAlgMethod( 104, "Kraken FinnedFish",  12, GNLTech.KrakenFinnedFish ) );   //Suspension of development      
            SolverLst0.Add( new UAlgMethod( 105, "Kraken FishEx",      13, GNLTech.KrakenFishEx));
            SolverLst0.Add( new UAlgMethod( 106, "Kraken FinnedFishEx",13, GNLTech.KrakenFinnedFishEx));

            SolverLst0.Add( new UAlgMethod( 112, "ForceChain_CellEx",  13, GNLTech.ForceChain_CellEx ) );
	        SolverLst0.Add( new UAlgMethod( 114, "ForceChain_HouseEx", 13, GNLTech.ForceChain_HouseEx ) );
	        SolverLst0.Add( new UAlgMethod( 116, "ForceChain_ContradictionEx",13, GNLTech.ForceChain_ContradictionEx ) );
#endif


            SolverLst0.Sort((a,b)=>(a.ID-b.ID));
        }

        public void Initialize_Solvers(){
            pPZL.extResult = "";
#if RegularVersion
			SprLKsMan.Initialize();
#endif
        }
//==========================================================

        private (int,string)  stageNo_MethodName=(-1,""); 
        private int methodCC = 0;



        public bool SnapSaveGP( UPuzzle aPZL, bool OmitteDuplicateSolutionB=true ){
            //Duplicate solutions occur in Kraken F/M Fish. Omitted the duplicate Solutions in the code below.
 
            if( pPZLMan is null )  return false;
            if( aPZL.Sol_ResultLong is null )  return false;
            var pChild_PZLs = pPZLMan.child_PZLs; //?? (pGNPX_Eng.PZLMan.child_PZLs=new List<UPuzzle>());

            try{
                // changed the policy[v4.1.2] ... became clear!
                
                pPZLMan.selectedIX = 0;
                // *************** Single ***************
                if( !SDK_Ctrl.MltAnsSearch ){           
                    //In one-solution search, save the same Puzzle-object without copying
                    pChild_PZLs.Add( aPZL );
                    return false;
                }

        
                // *************** Multiple ***************
                // is unique solution?
                string __SolResultKey = aPZL.Sol_ResultLong.Replace("\r"," ");
                if( OmitteDuplicateSolutionB && pChild_PZLs.Any(P=>(P.__SolResultKey==__SolResultKey)) ){  //Excluding the same solution
                          
                    //Duplicate solutions occur in Kraken F/M Fish. Omitted the duplicate Solutions in the code below.
                    //
                    using( var fAbort=new StreamWriter( "WarningMessage.txt",append:false, encoding:Encoding.UTF8) ){
                        fAbort.WriteLine( $"*** Redundant Solution Found in SnapSaveGP...  not unique solution : {__SolResultKey}" );
                    }        
                    WriteLine( $"*** Redundant Solution Found in SnapSaveGP...  not unique solution : {__SolResultKey}" );
                    return true;

                }   
                else{   // Save the copy of the solution
                    UPuzzle aPZLcopy = aPZL.Copy( stageNo_Increments:0, IDm:pChild_PZLs.Count ); //Copy at the same stage
                    aPZLcopy.DifLevel = GNPZ_Engin.AnalyzingMethod.DifLevel;
                    aPZLcopy.__SolResultKey = __SolResultKey;    
                    pChild_PZLs.Add(aPZLcopy);     // Save a copy.
        
                    aPZL.ToPreStage( );           // Return the original to the state before analysis.
                }



                {// *************** Termination by number of solutions ***************
                    // Reached the limit of multiple solutions searches.
                    if( pChild_PZLs.Count>=(int)GNPX_App.GMthdOption["MSlvrMaxAllAlgorithm"] ){
                        GNPX_App.GMthdOption["abortResult"] = pRes.msgUpperLimitBreak; // Reached the limit of multiple solution searches.
                        return false;
                    }  

                    // Method-specific counter initialization
                    var Stage_Method = ( stageNo, GNPZ_Engin.AnalyzingMethod.MethodName ); // tuple:(stage,name)
                    if( stageNo_MethodName != Stage_Method ){
                        stageNo_MethodName = Stage_Method; 
                        methodCC = 0;       // Initialization of number of solutions per algorithm
                    }
                    bool ContinueAnalysisB = (++methodCC < (int)GNPX_App.GMthdOption["MSlvrMaxAlgorithm"] );
                    return ContinueAnalysisB;  
                }
            }
            catch(Exception e){ WriteLine( $"{e.Message}\r{e.StackTrace}"); }
            return false;
        }


        public bool Check_TimeLimit(){ // Use only time-consuming SuDoKu Algorithm
            if( !SDK_Ctrl.MltAnsSearch )  return false;
            TimeSpan ts =  DateTime.Now - GNPX_App.MultiSolve_StartTime;
            bool timeLimit = ts.TotalSeconds >= (int)GNPX_App.GMthdOption["MSlvrMaxTime"];
            if(timeLimit)  GNPX_App.GMthdOption["abortResult"] = pRes.msgUpperLimitTimeBreak;
            return timeLimit;
        }


        //==========================================================
        public (bool,int,int,int) Aggregate_CellsPZM( List<UCell> BDLarg ){
            int P=0, Z=0, M=0;
            if( BDLarg==null )  return (false,P,Z,M);
            BDLarg.ForEach( q =>{
                if(q.No>0)      P++;
                else if(q.No<0) M++;
                else            Z++;
            } );

            return  (Z==0,P,Z,M);
        }



        public (int,int[]) Execute_Fix_Eliminate( List<UCell> BDLarg ){//Confirmation process
            // return code = 0 : Complete. Go to the next stage.
            //               1 : Solved. 
            //              -1 : Error. Conditions are broken.

            if( BDLarg.All(p=> p.No!=0) )  return (1,null);     //1: Solved. 

            if( BDLarg.Any(p=>p.CancelB>0) ){                         // ..... CancelB .....
                foreach( var P in BDLarg.Where(p=>p.CancelB>0) ){
                    int CancelB_ = P.CancelB^0x1FF;
                    P.FreeB &= CancelB_; P.CancelB=0;       
                    P.CellBgCr=_Black_;
                }
            }

            if( BDLarg.Any(p=>p.FixedNo>0) ){                         // ..... FixedNo .....
                foreach( var P in BDLarg.Where(p=>p.FixedNo>0) ){
                    int No = P.FixedNo;
                    if( No<1 || No>9 ) continue;
                    P.No=-No; P.FixedNo=0; P.FreeB=0;
                    P.CellBgCr = _Black_;
                }
            }

            {   // ..... Check Error .....
                Update_CellsState( BDLarg, false );
                foreach( var P in BDLarg.Where(p=>(p.No==0 && p.FreeBC==0)) )  P.ErrorState=9; // ..... Error .....
                int[] NChk=new int[27];
                for(int h=0; h<27; h++ ) NChk[h]=0;
                foreach( var P in BDLarg ){
                    int no = (P.No<0)? -P.No: P.No;
                    int B = (no>0)? (1<<(no-1)): P.FreeB;
                    NChk[P.r]|=B; NChk[P.c+9]|=B; NChk[P.b+18]|=B;
                }
                for(int h=0; h<27; h++ ){
                    if(NChk[h]!=0x1FF){ SolCode=-9119; return (-1,NChk); }  // -1: Error. Conditions are broken.
                }
            }
            SolCode=-1;
            return (0,null);    // 0: Complete. Go to next stage.
        }

        public void SetBG_OnError(int h){
            foreach(var P in pBOARD.IEGetCellInHouse(h)) P.Set_CellBKGColor(Colors.Violet);
        }

        public void Update_CellsState( List<UCell> BDLarg, bool setAllCandidates=true ){
                                                                //Set all candidates : Clear all analysis results.
            //WriteLine( $"...Update_CellsState...");   

            improper = false;
            foreach( var P in BDLarg ){
                P.Reset_StepInfo();
                int freeB=0;  
                if( P.No==0 ){
                    foreach( var rc in pConnectedCells[P.rc].IEGetRC() ){
                        if( BDLarg[rc].No != 0 )  freeB |= 1<<(Abs(BDLarg[rc].No)); //bit representation of  fixed cells
                    }

                    freeB = (freeB>>=1)^0x1FF;      //internal expression with 1 right bit shift, and EOR.
                    if( !setAllCandidates ) freeB &= P.FreeB; 
                    if( freeB==0 ){ improper=true; P.ErrorState=1; }//No solution
                }
                P.FreeB = freeB;
            }
        }

        public bool Verify_SUDOKU_Roule( ){
            bool    ret=true;
            if( improper ){ SolCode=9; return false; }

            for(int h=0; h<27; h++){
                int usedB=0, errB=0;
                foreach(var P in pBOARD.IEGetCellInHouse(h).Where(Q=>Q.No!=0)){
                    int no = Abs(P.No);
                    if(( usedB&(1<<no)) !=0 )   errB |= (1<<no);            //have the same digit.
                    usedB |= (1<<no);
                }

                if(errB!=0){
                    foreach(var P in pBOARD.IEGetCellInHouse(h).Where(Q=>Q.No!=0)){
                        int no = Abs(P.No);
                        if( (errB&(1<<no)) !=0 ){ P.ErrorState=8; ret=false; }  //mark cells with the same digit
                    }
                }
            }
            SolCode = ret? 0: 9;
            return ret;
        }

        public void ResetAnalysisResult( bool clear0 ){
            if(clear0){   // true:Initial State
                foreach(var P in pBOARD.Where(Q=>Q.No<=0)){ P.Reset_StepInfo(); P.FreeB=0x1FF; P.No=0; }
            }
            else{
                foreach(var P in pBOARD.Where(Q=>Q.No==0)){ P.Reset_StepInfo(); P.FreeB=0x1FF; }
            }
            Update_CellsState( BDLarg:pBOARD );
			pPZL.extResult="";
        }
    }
}