using System;
using System.Collections.Generic;
using System.Linq;
using static System.Diagnostics.Debug;

using GIDOO_space;

namespace GNPXcore{
    public partial class ALSTechGen: AnalyzerBaseV2{

#if false
		private int stageNoMemo = -9;
        private Bit981 BP981;
        private Bit81  BD0;
        public ALSTechGen( GNPX_AnalyzerMan pAnMan ): base(pAnMan){
            this.pAnMan=pAnMan;
        }

		private void Prepare(){
			if( stageNo != stageNoMemo ){
				stageNoMemo = stageNo;
				ALSMan.Initialize();
				ALSMan.PrepareALSLinkMan(1);
                BP981 = new Bit981(pBOARD);
                BD0 = new Bit81(pBOARD,0x1FF);
			}      
		}

        //ALS XY-Wing is an analysis algorithm using three ALS. It is the case of the next ALS Chain 3ALS.
        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page44.html

        //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/
        //7.1..9..8.52...19..8...3.574.3.5.......2.1.......3.7.519.7...3..37...68.8..3..9.1
		//2.9..3..8.17...63..8...6.259.5.6.......3.2.......9.3.114.6...9..53...48.7..4..2.6

        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page44.html   - XYZ-WingALS
        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page51.html   - ALS XY-Wing
#endif
        private bool break_XYZwingALS=false; //True if the number of solutions reaches the specified number.
        public bool XYZwingALS( ){
            break_XYZwingALS = false;
			Prepare();
            if( ALSMan.ALSLst==null || ALSMan.ALSLst.Count<=2 ) return false;
            ALSMan.QSearch_Cell2ALS_Link();     //prepare cell-ALS link

            for(int sz=3; sz<7; sz++ ){ //number of digits in the cell
                if( _XYZwingALSSub(sz) )  return true;
                if( break_XYZwingALS ) return true;
            }
            return false;
        }

        private    bool _XYZwingALSSub( int wsz ){ //simple UVWXYZwing
            List<UCell> FBCX = pBOARD.FindAll(p=>p.FreeBC==wsz);
            if(FBCX.Count==0)  return false;

            // ----- Stem cell (P0) -----
            foreach( var P0 in FBCX ){              // Stem cell
                int b0=P0.b;                        // Stem block


                // ----- digit (no) -----
                for( int no=0; no<9; no++){ //No relation between P0.FreeB and excluded digit(no).
                    int noB=1<<no;

                    Bit81 B81_P0_conn = (new Bit81(pBOARD,noB)) & ConnectedCells[P0.rc];  //cells related to rc/no
                    Bit81 B81_P0_block = B81_P0_conn & HouseCells[18+b0];               //rc-related cells in Stem block(=b0).


                    // ----- row / column -----
                    for(int dir=0; dir<2; dir++ ){ // dir 0:row 1:col
                        int   h = (dir==0)? P0.r: (9+P0.c);
                        Bit81 B81_P0_block2  = B81_P0_block - HouseCells[h];            //ALS candidate position inside the block
                        if( B81_P0_block2.IsZero() ) continue;


                        // ---- ALSout (ALS out of Stem Block) -----
                        // ALSout : ALS out of Stem Block
                        // ALSin  : ALS in Stem Block
                        Bit81 Pout = (B81_P0_conn&HouseCells[h])-HouseCells[18+P0.b];   //ALS candidate position outside the block
                        foreach( var ALSout in ALSMan.IEGetCellInHouse(1,noB,Pout,h) ){ //ALS out of Stem Block
                            Bit81 B81_out = new Bit81( ALSout.UCellLst, noB );          //#no existence position(outer ALS)


                            // ---- ALSin (ALS in Stem Block) -----
                            foreach( var ALSin in ALSMan.IEGetCellInHouse(1,noB,B81_P0_block2,18+b0) ){ //ALS in Stem b0
                                if( pAnMan.Check_TimeLimit() )  return false;

                                int FreeBin2 = ALSin.FreeB.DifSet(noB);
                                Bit81 B81_in = new Bit81( ALSin.UCellLst, noB );        //#no existence position(inner-ALS)

                                int notHooked = P0.FreeB.DifSet(ALSout.FreeB|ALSin.FreeB);//Stem digits not hooked to ALSin and ALSout
                                if( notHooked!=0 ) continue;

                                int FreeBOut2 = ALSout.FreeB.DifSet(noB);               // ALSout.FreeB - noB
                                int FreeBin3  = P0.FreeB.DifSet(FreeBOut2|FreeBin2);    // P0.FreeB - (FreeBOut2|FreeBin2)
                                if( FreeBin3==0 )  continue;


                                // ----- Eliminated cell(rc) -----
                                Bit81 B81_in_out = B81_out | B81_in;
                                bool  SolFound = false;  
                                Bit81 B81elm = B81_P0_block - B81_in_out;
                                foreach( int rc in B81elm.IEGetRC() ){
                                    if( B81_P0_block2.IsHit(rc) )  continue; //not (forcused cell),(Included in Pout),(Included in B81_P0_block)
                                    if( (B81_in_out-ConnectedCells[rc]).IsNotZero() )              continue;
                                    pBOARD[rc].CancelB = noB;
                                    SolFound=true;
                                }

                                if(SolFound){
                                    SolCode=2;     
                                    string[] xyzWingName = { "XYZ-Wing","WXYZ-Wing","VWXYZ-Wing","UVWXYZ-Wing"};
                                    string SolMsg = xyzWingName[wsz-3]+"(ALS)";

                                    if( SolInfoB ){
                                        P0.Set_CellColorBkgColor_noBit(P0.FreeB,AttCr,SolBkCr2);

                                        ALSin.UCellLst.IE_SetNoBBgColor(  pBOARD, 0x1FF, AttCr3, SolBkCr);
                                        ALSout.UCellLst.IE_SetNoBBgColor( pBOARD, 0x1FF, AttCr3, SolBkCr);

                                        string msg0 = $" Pivot: {P0.rc.ToRCString()} #{P0.FreeB.ToBitStringN(9)}";
                                        string msg1 = $"    in: {ALSin.UCellLst.ToRCString()} #{ALSin.FreeB.ToBitStringN(9)}";
                                        string msg2 = $"   out: {ALSout.UCellLst.ToRCString()} #{ALSout.FreeB.ToBitStringN(9)}";
                                        string msg3 = $" Eliminated: {pBOARD.FindAll(p=>p.CancelB>0).ToRCString()} #{no+1}";   

                                        Result = SolMsg+msg0;  
                                        ResultLong = $"{SolMsg}\r{msg0}\r{msg1}\r{msg2}\r{msg3}";    
                                    }

                                    if( __SimpleAnalyzerB__ )  return true;
                                    if( !pAnMan.SnapSaveGP(pPZL) ){ break_XYZwingALS=true; return true; }
                                    SolFound=false;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

    }
}