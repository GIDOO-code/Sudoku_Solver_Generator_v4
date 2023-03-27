using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using static System.Math;
using static System.Diagnostics.Debug;
using GIDOO_space;

namespace GNPXcore{
    public partial class FishGen: AnalyzerBaseV2{
        //Autocannibalism
        //http://www.dailysudoku.com/sudoku/forums/viewtopic.php?p=26306&sid=13490447f6255f8d78a75b647a9096b9

        //http://forum.enjoysudoku.com/als-chains-with-overlap-cannibalism-t6580-30.html
        //http://www.dailysudoku.com/sudoku/forums/viewtopic.php?t=219&sid=dae2c2133114ee9513a6a37124374e7c
        //http://www.dailysudoku.co.uk/sudoku/forums/viewtopic.php?p=1180&highlight=#1180

        //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/
        //.6...52..4..1..65.....6..3....3...65.5........7.5.....681457.......2.517.2.9..846
        //....2.6..7.41.9....3.847.1..7.9.1.541.3...7.9.9.2.8.36.4.683.7.3.75.4.......1.4..

        //....9.6..4.61.8....8.462.5..6.2.1.373.5...2.8.4.3.6.95.3.914.7.6.18.5.......2.8..
        //12.59.6844561.8.2..8.462.51.6.251437315749268.4.386195.3.9145766.18.5.425.462.81.  for develop

        private bool break_CannibalisticFMFish=false; //True if the number of solutions reaches the specified number.

        public bool CannibalisticFMFish( ){
            CannibalisticFMFish_Ex( FinnedFlag:false, CannFlag:true );
            return  break_CannibalisticFMFish;
        }

        public bool FinnedCannibalisticFMFish( ){
            CannibalisticFMFish_Ex( FinnedFlag:true, CannFlag:true );
            return  break_CannibalisticFMFish;
        }


        private bool CannibalisticFMFish_Ex( bool FinnedFlag=false, bool CannFlag=true ){
            break_CannibalisticFMFish = false;
            for(int sz=2; sz<=7; sz++ ){

                for(int no=0; no<9; no++ ){

                    if( CannibalisticFMFish_sub( sz, no, FMSize:27, FinnedFlag, EndoFlag:false, CannFlag:true) ) return true;
                    if( break_CannibalisticFMFish ) return true;
                }
            }
            return false;
        }

        public bool CannibalisticFMFish_sub( int sz, int no, int FMSize, bool FinnedFlag, bool EndoFlag=false, bool CannFlag=false ){
            int noB=(1<<no);
            int BasesetFilter=0x7FFFFFF, CoverSetFilter=0x7FFFFFF;
            FishMan FMan=new FishMan(this,FMSize,no,sz,extFlag:(sz>=3));
            
            foreach( var Bas in FMan.IEGet_BaseSet(BasesetFilter, FinnedFlag:FinnedFlag, EndoFlag:EndoFlag) ){    

                foreach( var Cov in FMan.IEGet_CoverSet(Bas, CoverSetFilter, FinnedFlag:FinnedFlag, CannFlag:CannFlag) ){                  //CoverSet
                    Bit81 FinB81 = Bas.BaseB81 - Cov.CoverB81;

                    if( FinB81.Count==0 ){
                        foreach( var P in Cov.CannFinB81.IEGetUCell_noB(pBOARD,noB) ){ P.CancelB=noB; SolCode=2; }
                        if(SolCode>0){
                            if( SolInfoB ){
                                _FishResult(no,sz,Bas,Cov,(FMSize==27)); //FMSize 27:Franken/Mutant
                            }
                                //WriteLine(ResultLong); //___Debug_CannFish("Cannibalistic");
                            if( __SimpleAnalyzerB__ )  return true;
                            if( !pAnMan.SnapSaveGP(pPZL) ){ break_CannibalisticFMFish=true; return true; }
                        }
                    }
                    else{
                        FinB81 |= Cov.CannFinB81;
                        Bit81 ELM =null;
                        Bit81 E=(Cov.CoverB81-Bas.BaseB81) | Cov.CannFinB81;
                        ELM=new Bit81();
                        foreach( var rc in E.IEGet_rc() ){
                            if( (FinB81-ConnectedCells[rc]).Count==0 ) ELM.BPSet(rc);
                        }
                        if( ELM.Count>0 ){
                            foreach( var P in ELM.IEGetUCell_noB(pBOARD,noB) ){ P.CancelB=noB; SolCode=2; }
                            if( SolCode>0 ){
                                if( SolInfoB )_FishResult(no,sz,Bas,Cov,(FMSize==27));
                                    //WriteLine(ResultLong); //___Debug_CannFish("Finned Cannibalistic");
                                if( __SimpleAnalyzerB__ )  return true;
                                if( !pAnMan.SnapSaveGP(pPZL) ){ break_CannibalisticFMFish=true; return true; }
                            }
                        }
                    }
                }
            }
            return false;
        }

        private void ___Debug_CannFish(string MName){
            using( var fpX=new StreamWriter(" ##DebugP.txt",append:true,encoding:Encoding.UTF8) ){
                string st="";
                pBOARD.ForEach(q =>{ st += (Max(q.No,0)).ToString(); } );
                st=st.Replace("0",".");
                fpX.WriteLine(st+" "+MName);
            }
        }
    }  
}