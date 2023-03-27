using System;
using System.Collections.Generic;
using System.Linq;
using static System.Diagnostics.Debug;

using GIDOO_space;

namespace GNPXcore{
    //  Sue de Coq
    //  http://hodoku.sourceforge.net/en/tech_misc.php#sdc
    //  A more formal definition of SDC is given in the original Two-Sector Disjoint Subsets thread:
    //  Consider the set of unfilled cells C that lies at the intersection of Box B and Row (or Column) R.
    //  Suppose |C|>=2. Let V be the set of candidate values to occur in C. Suppose |V|>=|C|+2.
    //  The pattern requires that we find |V|-|C|+n cells in B and R, with at least one cell in each, 
    //  with at least |V|-|C| candidates drawn from V and with n the number of candidates not drawn from V.
    //  Label the sets of cells CB and CR and their candidates VB and VR. Crucially,
    //  no candidate from V is allowed to appear in VB and VR. 
    //  Then C must contain V\(VB U VR) [possibly empty], |VB|-|CB| elements of VB and |VR|-|CR| elements of VR.
    //  The construction allows us to eliminate candidates VB U (V\VR) from B\(C U CB), 
    //  and candidates VR U (V\VB) from R\(C U CR).
    // (\:backslash)

    // SueDeCoq
    // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page45.html

    //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/
    //.4...1.286..5....7..7.46...7.3..98..9.......2..12..3.9...95.1..1....2..559.1...7.

    //87........9.81.65....79...8.....67316..5.1..97124.....3...57....57.48.1........74
    //87.....9..9381.657...79...8.4.286731638571..97124398653.415798..57.48.1..8....574  //for develop

    //.2...3..4.4....25.6...243.8256..8....8..9..2....2..4868.463...2.63....4.9..7...6.
    //.2...36.434..6.25.69..243.82564.8.3.48.396.25.3925.48687463.5.2.63..2.4.912745863  //for develop

    //hodoku
    //3248615976579438219185273642.143....5.32.6...4..7.....1..6.....8..1746..7..39..1. //Q1 ok
    //641..8329873291645592..4187.8..2.4.1.....92.3...41.8.6......73..6.9..51...714.968 //Q2 another solution
    //82..6.....6.8...2...32..568641...37.53......4.87...6..4563.97..37...1.......5..3. //Q3 ok
    //329...5.........29.6.....3.9342..765716..5482258764..3.73....5.1954.3.7...25..3.. //Q4 ok

    public partial class AALSTechGen: AnalyzerBaseV2{

		private int stageNoMemo = -9;
		private ALSLinkMan fALS;

		public AALSTechGen( GNPX_AnalyzerMan pAnMan ): base(pAnMan){
			fALS = new ALSLinkMan(pAnMan);
        }

        public bool SueDeCoq( ){
			if( stageNo != stageNoMemo ){
				stageNoMemo = stageNo;
				fALS.Initialize();
	         // fALS.PrepareALSLinkMan(+2, minSize:2 ); //Generate ALS(+1 & +2)
                fALS.PrepareALSLinkMan(+1, minSize:2 ); //Generate ALS(+1)    
			}
			
            if( fALS.ALSLst.Count<=3 ) return false;
                   // fALS.ALSLst.ForEach( P => WriteLine(P) );

            foreach( var ISPB in fALS.ALSLst.Where(p=> p.Size>=3 && p.houseBlk>=0) ){ //Selecte Block-type ALS
                if( ISPB.houseBlk <= 1) continue;  //Block squares have multiple rows and columns             
                    //WriteLine( $"ISPB:{ISPB}" );

                foreach( var ISPR in fALS.ALSLst.Where(p=> p.Size>=3 ) ){　//Selecte Row-type/Column-type ALS
                    if( pAnMan.Check_TimeLimit() )  return false;

                    if( ISPR.houseRow<0 && ISPR.houseCol<0 )  continue;

                    //Are the cell configurations of the intersections the same?
                    int hs = (ISPR.houseRow>=0)? ISPR.houseRow:
                             (ISPR.houseCol>=0)? ISPR.houseCol:
                             -1;
                    if( hs<0 )  continue;
                    if( (ISPB.B81&HouseCells[hs]) != (ISPR.B81&HouseCells[ISPB.houseBlk]) ) continue;
                        //WriteLine( $"ISPR:{ISPR}" );

                    // ***** the code follows HP -> https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page45.html *****
                    Bit81 IS = ISPB.B81&ISPR.B81;                               //Intersection
                    if( IS.Count<2 ) continue; 　                               //At least 2 cells at the intersection
                    if( (ISPR.B81-IS).Count==0 ) continue;                      //There is a part other than the intersecting part in ISPR                    

                    Bit81 PB = ISPB.B81-IS;                                     //ISPB's outside IS
                    Bit81 PR = ISPR.B81-IS;                                     //ISPR's outside IS
                    int IS_FreeB = IS.AggregateFreeB(pBOARD);                     //Intersection number
                    int PB_FreeB = PB.AggregateFreeB(pBOARD);                     //ISPB's number outside the IS
                    int PR_FreeB = PR.AggregateFreeB(pBOARD);                     //ISPR's number outside the IS
                    
                    if( (IS_FreeB&PB_FreeB&PR_FreeB)>0 ) continue;

                    //A.DifSet(B): A-B = A&(B^0xFFFFFFFF)
                    int PB_FreeBn = PB_FreeB.DifSet(IS_FreeB);                  //Digits not at the intersection of PB
                    int PR_FreeBn = PR_FreeB.DifSet(IS_FreeB);                  //Numbers not in the intersection of PR

                    int sdqNC = PB_FreeBn.BitCount()+PR_FreeBn.BitCount();      //Number of confirmed numbers outside the intersection
                    if( (IS_FreeB.BitCount()-IS.Count) != (PB.Count+PR.Count-sdqNC) ) continue;

                    int elmB = PB_FreeB | IS_FreeB.DifSet(PR_FreeB);            //Exclusion Number in PB 
                    int elmR = PR_FreeB | IS_FreeB.DifSet(PB_FreeB);            //Exclusion Number in PR                
                    if( elmB==0 && elmR==0 ) continue;

                    foreach( var P in _GetRestCells(ISPB,elmB) ){ P.CancelB|=P.FreeB&elmB; SolCode=2; }
                    foreach( var P in _GetRestCells(ISPR,elmR) ){ P.CancelB|=P.FreeB&elmR; SolCode=2; }

                    if(SolCode>0){      //--- SueDeCoq found -----
                        SolCode=2;
                        SuDoQueEx_SolResult( ISPB, ISPR );
                        if( ISPB.Level>=3 || ISPB.Level>=3 ) WriteLine("Level-3");

                        if( __SimpleAnalyzerB__ )  return true;
                        if( !pAnMan.SnapSaveGP(pPZL) )  return true;
                    }
                }
            }
            return false;
        }

        private (Bit81,Bit81) _BreakDown_ALS( UALS Uals, int hs ){
            Bit81 B_in  = Uals.B81 & HouseCells[hs];
            Bit81 B_out = Uals.B81 - HouseCells[hs];
            return (B_in,B_out);
        }

        public IEnumerable<UCell> _GetRestCells( UALS ISP, int selB ){
            return pBOARD.IEGetCellInHouse(ISP.h,selB).Where(P=>!ISP.B81.IsHit(P.rc));
        }
        private void SuDoQueEx_SolResult( UALS ISPB, UALS ISPR ){
            int level = Math.Max( ISPB.Level, ISPR.Level );
            string stT = "SueDeCoq";
            if( level > 1 )  stT += $" (ALS Level-{level})";
            Result = stT;

            if( SolInfoB ){
                ISPB.UCellLst.IE_SetNoBBgColor( pBOARD, 0x1FF, AttCr, SolBkCr );    // FreeB_Clr, Digit_Clr, background_Clr
                ISPR.UCellLst.IE_SetNoBBgColor( pBOARD, 0x1FF, AttCr, SolBkCr );

                string st = "\r ALS";
                if( ISPB.Level==1 ) st += "(block)  ";
                else{ st += $"-{ISPB.Level}(block)"; }
                st += $": {ISPB.ToStringRCN()}";

                st += "\r ALS" + ((ISPR.Level==1)? "": "-2");
                st += ((ISPR.h<9)? "(row)":"(col)");
                st += ((ISPR.Level==1)? "    ": "  ");
                st += ": "+ISPR.ToStringRCN();
                ResultLong = stT+st;
            }
        }
    }
}