using System;
using System.Collections.Generic;
using System.Linq;

using GIDOO_space;

namespace GNPXcore{
    public partial class FishGen: AnalyzerBaseV2{
    //  http://forum.enjoysudoku.com/search.php?keywords=Endo&t=4993&sf=msgonly
    //  latest viewpoint
    //  Fin Cell: Any cell that's in more Base Sectors than Cover Sectors.
    //  Possible Elimination Cell: Any cell that's in more Cover Sectors than Base Sectors.
    //  Actual Elimination Cell: All possible elimination cells if no fin cells exist. 
    //  Otherwise, all possible elimination cells that are a buddy to every fin cell. 
    //  An exception to the buddy restriction exists for Kraken fish.

    //  Endo-fin
    //  http://www.dailysudoku.com/sudoku/forums/viewtopic.php?p=32379&sid=8fb87da8d9beec9c11a2909cae5adecf

    //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/
    //....2.6..7.41.9....3.847.1..7.9.1.541.3...7.9.9.2.8.36.4.683.7.3.75.4.......1.4..

        private bool break_EndoFinnedFMFish=false; //True if the number of solutions reaches the specified number.

        public bool EndoFinnedFMFish( ) => _EndoFinnedFMFish( );
        public bool FinnedEndoFinnedFMFish( ) => _EndoFinnedFMFish( FinnedFlag:true );

        private bool _EndoFinnedFMFish( bool FinnedFlag=false ){
            break_EndoFinnedFMFish=false;
            for(int sz=2; sz<=7; sz++){   //(5:Squirmbag 6:Whale 7:Leviathan)
                for(int no=0; no<9; no++){
                    if( EndoFinnedFMFish_sub( sz, no, FMSize:27, FinnedFlag, EndoFlag:true, CannFlag:false) ) return true;
                    if( break_EndoFinnedFMFish ) return true;
                }
            }
            return false;
        }

        public bool EndoFinnedFMFish_sub( int sz, int no, int FMSize, bool FinnedFlag, bool EndoFlag=false, bool CannFlag=false ){   
            int noB=(1<<no);
            int BasesetFilter=0x7FFFFFF, CoverSetFilter=0x7FFFFFF;
            FishMan FMan=new FishMan(this,FMSize,no,sz,(sz>=3));
            foreach( var Bas in FMan.IEGet_BaseSet(BasesetFilter,FinnedFlag:FinnedFlag,EndoFlag:EndoFlag) ){ //BaseSet

                foreach(var Cov in FMan.IEGet_CoverSet(Bas,CoverSetFilter,FinnedFlag:FinnedFlag, CannFlag:CannFlag)){               //CoverSet
                    if(pAnMan.Check_TimeLimit()) return false; 
                    Bit81 FinB81 = Cov.FinB81 | Bas.EndoFinB81;
                    Bit81 E      = Cov.CoverB81 - Bas.BaseB81;
                    Bit81 ELM    = new Bit81();

                    //see latest viewpoint
                    foreach( var rc in E.IEGet_rc() ){
                        if( (FinB81-ConnectedCells[rc]).Count == 0 ) ELM.BPSet(rc);
                    }
                    if( ELM.Count>0 ){
                        foreach( var P in ELM.IEGetUCell_noB(pBOARD,noB) ){ P.CancelB=noB; SolCode=2; }
                        if( SolCode>0 ){
                            if( SolInfoB ){
                                _FishResult(no,sz,Bas,Cov,(FMSize==27)); //27:Franken/Mutant
                            }
                            //WriteLine(ResultLong);
                            if( __SimpleAnalyzerB__ )  return true;
                            if( !pAnMan.SnapSaveGP(pPZL) ){ break_EndoFinnedFMFish=true;  return true; }
                        }
                    }
                }
            }
            return false;
        }
    }  
}