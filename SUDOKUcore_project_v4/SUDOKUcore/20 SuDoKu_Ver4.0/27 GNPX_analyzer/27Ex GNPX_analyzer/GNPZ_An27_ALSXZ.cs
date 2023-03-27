using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using static System.Diagnostics.Debug;

using GIDOO_space;
using System.Security.Policy;
using System.Windows.Documents;
using System.Runtime.CompilerServices;
using System.Windows.Media.Media3D;

namespace GNPXcore{
    public partial class ALSTechGen: AnalyzerBaseV2{

        //Algorithm using ALS and RCC((Restricted Common Candidate).
        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page50.html

        //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/
        //2.548...9.9..3..2...7.923.4..8.....2541...6879.....1..4.961.2...8..7..9.7...584.6
        //.2...783..47.2...13..1....7....38.15...5.4...58.79....6....2..82...8.57..793...6.
        //87........9.81.65....79...8.....67316..5.1..97124.....3...57....57.48.1........74
        //.9..4..6.4..15...2..6..91....4....7.36.....15.8....3....82..4..9...34..1.4..8..3.

        private bool break_ALS_XZ=false; //True if the number of solutions reaches the specified number.
        public bool ALS_XZ( ){
			Prepare();
            if( ALSMan.ALSLst==null || ALSMan.ALSLst.Count<=2 ) return false;
        
		    for(int sz=4; sz<=14; sz++ ){
                if( _ALSXZsub(sz) ) return true;
                if( break_ALS_XZ )  return true;
            }
            return false;
        }

        private bool _ALSXZsub( int sz ){
            break_ALS_XZ=false;
            if( ALSMan.ALSLst.Count<2 ) return false;

            var cmb = new Combination(ALSMan.ALSLst.Count,2);
            int nxt=99;
            while( cmb.Successor(skip:nxt) ){                        //Select two ALSs
                if( pAnMan.Check_TimeLimit() )  return false;

                UALS UA = ALSMan.ALSLst[cmb.Index[0]];
                nxt=0;
                if( !UA.singly || UA.Size==1 || UA.Size>(sz-2) ) continue;

                UALS UB = ALSMan.ALSLst[cmb.Index[1]];
                nxt=1;
                if( !UB.singly || UB.Size==1 || (UA.Size+UB.Size)!=sz ) continue;

                //=======================================================
                // WriteLine($"\nUA {UA}\nUB{UB}");

                int RCC = ALSMan.Get_AlsAlsRcc(UA,UB);          //Common numbers, House contact, Without overlap

                if( RCC==0 ) continue;               

                if( RCC.BitCount()==1 ){        //===== Singly Linked =====
                    int noBother = (UA.FreeB&UB.FreeB).DifSet(RCC); //Exclude candidate digit
                    if( noBother>0 && _ALSXZ_IsSinglyLinked(UA,UB,RCC,noBother) ){
                        SolCode = 2;
                        ALSXZ_SolResult(RCC,UA,UB );

                        if( __SimpleAnalyzerB__ )  return true;
                        if( !pAnMan.SnapSaveGP(pPZL) ){ break_ALS_XZ=true; return true; }
                    }
                }
                else if( RCC.BitCount()==2 ){   //===== Doubly Linked =====
                    if( _ALSXZ_DoublyLinked(UA,UB,RCC) ){
                        SolCode=2;
                        ALSXZ_SolResult(RCC,UA,UB);

                        if( __SimpleAnalyzerB__ )  return true;
                        if( !pAnMan.SnapSaveGP(pPZL) ){ break_ALS_XZ=true; return true; }
                    }
                }
            }
            return false;
        }
        private void ALSXZ_SolResult( int RCC, UALS UA, UALS UB ){
            string st = "ALS-XZ "+((RCC.BitCount()==1)? "(Singly Linked)": "(Doubly Linked)");
            Result = st;
            
            if( SolInfoB ){            
                foreach( var P in UA.UCellLst ) P.Set_CellColorBkgColor_noBit(P.FreeB,AttCr3,SolBkCr);
                foreach( var P in UB.UCellLst ) P.Set_CellColorBkgColor_noBit(P.FreeB,AttCr3,SolBkCr2);

                st += "\r ALS1: "+UA.ToStringRCN();
                st += "\r ALS2: "+UB.ToStringRCN();
                st += "\r  RCC: #"+RCC.ToBitStringN(9);
                ResultLong = st;
            }
        }       
  
        private bool _ALSXZ_IsSinglyLinked( UALS UA, UALS UB, int RCC, int noBother ){   
        // *=*=* SinglyLinked subroutine *=*=*
        //noBother : Common digits for UA and UB other than RCC. Bit representation.

            // Suppose that two ALSs have RCC(digit x).
            // And, let z be the digit contained in both ALS different from RCC.
	        // digit z outside the ALS and associated with all z in both ALS can be excluded from the candidate.
	        // If z is true, both ALSs are changed to LockedSet, and both ALSs include RCC.
            bool solF=false;
            foreach( var no in noBother.IEGet_BtoNo() ){ 
                int noB = 1<<no; 

                //Outside ALS, find cells commonly related to ALS#no.
                Bit81 ELM = (new Bit81(pBOARD,noB)) - (UA.B81|UB.B81);
                foreach( var P in UA.UCellLst.Where(p=> (p.FreeB&noB)>0) )  ELM &= pConnectedCells[P.rc];
                foreach( var P in UB.UCellLst.Where(p=> (p.FreeB&noB)>0) )  ELM &= pConnectedCells[P.rc];

                if( ELM.IsNotZero() ){
                    foreach( var rc in ELM.IEGetRC() ){ pBOARD[rc].CancelB |= noB; solF = true; }
                }
            }

            return solF;
        }
        private bool _ALSXZ_DoublyLinked( UALS UA, UALS UB, int RCC ){              // *=*=* DoublyLinked subroutine *=*=*
            //WriteLine( $"UA:{UA}\nUB:{UB}" );
            Bit81 B81inALS = new Bit81(); //Covered cells
            bool solF=false;

            //----- RCC -----
            // A digit belonging to the same house as non-ALS RCC can be excluded.
            // If this applies to RCC-1, then the two ALS are LockedSet and
            // the RCC-2 is excluded from both ALS.
            foreach( int no in RCC.IEGet_BtoNo() ){
                int noB=1<<no;
                B81inALS.Clear();   // all #no in ALS
                foreach( var uc in UA.UCellLst.Where(p=>(p.FreeB&noB)>0) ) B81inALS.BPSet(uc.rc);
                foreach( var uc in UB.UCellLst.Where(p=>(p.FreeB&noB)>0) ) B81inALS.BPSet(uc.rc);

                Bit81 B81outside = (new Bit81(pBOARD,noB)) - (UA.B81|UB.B81);    //Cells outside ALS(UA,UB)
                foreach( var rc in B81outside.IEGet_rc() ){
                    if( (B81inALS-ConnectedCells[rc]).IsZero() ){   // no cells linked with all #no in ALS.
                        pBOARD[rc].CancelB|=noB; solF=true;
                    }
                }
            }

            //----- ALS element digit other than RCC -----
            // For a element of ALS digited z(different from RCC),
		    // z outside the ALS and related to all z in the ALS can be excluded from the candidate.
		    // If this is true, that ALS is a LockedSet and two RCCs belong to this ALS.
		    // In the other ALS, there are n-1 candidate digits in n cells, and ALS collapses.
            int nRCC = UA.FreeB.DifSet(RCC);               
            foreach( int no in nRCC.IEGet_BtoNo() ){    // a digit other than RCC
                int noB = 1<<no;
                B81inALS.Clear();
                
                foreach( var uc in UA.UCellLst.Where(p=>(p.FreeB&noB)>0) ) B81inALS.BPSet(uc.rc); //Cells#no inside UA
                Bit81 B81outside = (new Bit81(pBOARD,noB)) - (UA.B81|UB.B81);                     //Cells#no outside UA & UB

                foreach( var rc in B81outside.IEGet_rc() ){
                    if( (B81inALS-ConnectedCells[rc]).IsZero() ){   //If rc#no is true, exclude all UA#no.
                        pBOARD[rc].CancelB|=noB; solF=true;           //Therefore rc#no is excluded.
                    }
                }
            }

            nRCC = UB.FreeB.DifSet(RCC);                
            foreach( int no in nRCC.IEGet_BtoNo() ){    // a digit other than RCC
                int noB = 1<<no;
                B81inALS.Clear();

                foreach( var uc in UB.UCellLst.Where(p=>(p.FreeB&noB)>0) ) B81inALS.BPSet(uc.rc); //Cells#no inside UB
                Bit81 B81outside = (new Bit81(pBOARD,noB)) - (UA.B81|UB.B81);                     //Cells#no outside UA & UB
           
                foreach( var rc in B81outside.IEGet_rc() ){
                    if( (B81inALS-ConnectedCells[rc]).IsZero() ){   //If rc#no is true, exclude all UB#no.
                        pBOARD[rc].CancelB|=noB; solF=true;           //Therefore rc#no is excluded.
                    }
                }
            }
            return solF;
        }
    }
}
 